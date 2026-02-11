using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;
using Stripe;
using Stripe.Checkout;

namespace LMS.Pages.StudentAccount
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<Course> RegisteredCourses { get; set; } = new();
        public int TotalCredits { get; set; }
        public decimal Tuition { get; set; }
        public decimal TotalPaid { get; set; }
        public string StripePublishableKey { get; set; }

      
        public List<Payment> RecentPayments { get; set; } = new();

        [BindProperty]
        public decimal PaymentAmount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadUserData();
        }

        private async Task LoadUserData()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return;

            // Only for students
            if (CurrentUser.UserType != "Student")
                return;

            // Load courses
            RegisteredCourses = await _context.Registration
                .Where(r => r.StudentID == CurrentUser.Id)
                .Join(_context.Course,
                      r => r.CourseID,
                      c => c.Id,
                      (r, c) => c)
                .ToListAsync();

            // Sum total credits
            TotalCredits = RegisteredCourses.Sum(c => c.CreditHours);

            // Total tuition = $100 per credit
            var totalTuition = TotalCredits * 100;

            // Calculate total payments made
            TotalPaid = await _context.Payments
                .Where(p => p.StudentId == CurrentUser.Id && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            // Remaining balance = total tuition - payments made
            Tuition = totalTuition - TotalPaid;

           
            RecentPayments = await _context.Payments
                .Where(p => p.StudentId == CurrentUser.Id && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            // Get Stripe publishable key
            StripePublishableKey = _configuration["pk_test_51SwPAbQeeHKH9xZ6cogbadiQKgppICfOhZGEg5Cw1aMSDsmjcZhgzcYNZU6qJi0UyeIsvZjBc9BPGVrMsUFhuhWn009dlx00Vg\r\n"];
        }

        private async Task SavePaymentRecord(string studentId, decimal amount, string paymentType, string stripeSessionId)
        {
            var payment = new Payment
            {
                StudentId = studentId,
                Amount = amount,
                PaymentType = paymentType,
                StripeSessionId = stripeSessionId,
                PaymentDate = DateTime.UtcNow,
                Status = "Completed",
                Notes = $"Payment processed via Stripe. Session: {stripeSessionId}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Payment saved: ${amount} for student {studentId}");
        }

        // Full payment endpoint
        public async Task<IActionResult> OnPostCreateCheckoutSessionAsync()
        {
            await LoadUserData();

            if (CurrentUser == null || CurrentUser.UserType != "Student")
                return RedirectToPage("/Error");

            if (Tuition <= 0)
            {
                TempData["ErrorMessage"] = "No tuition balance to pay.";
                return RedirectToPage();
            }

            // Convert to cents (Stripe uses smallest currency unit)
            var amountInCents = (long)(Tuition * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Tuition Payment",
                                Description = $"Tuition for {TotalCredits} credits"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
               
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Success?amount={Tuition}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Index",
                CustomerEmail = CurrentUser.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "student_id", CurrentUser.Id },
                    { "payment_type", "full_tuition" },
                    { "amount", Tuition.ToString("F2") }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            // Save payment record
            await SavePaymentRecord(CurrentUser.Id, Tuition, "full_tuition", session.Id);

            return Redirect(session.Url);
        }

        // Custom payment
        public async Task<IActionResult> OnPostCreateCustomCheckoutSessionAsync()
        {
            await LoadUserData();

            if (CurrentUser == null || CurrentUser.UserType != "Student")
                return RedirectToPage("/Error");

            if (PaymentAmount <= 0 || PaymentAmount > Tuition)
            {
                TempData["ErrorMessage"] = "Invalid payment amount.";
                return RedirectToPage();
            }

            // Convert to cents
            var amountInCents = (long)(PaymentAmount * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Partial Tuition Payment",
                                Description = $"Payment of ${PaymentAmount:F2} towards tuition"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
             
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Success?amount={PaymentAmount}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Index",
                CustomerEmail = CurrentUser.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "student_id", CurrentUser.Id },
                    { "payment_type", "partial_tuition" },
                    { "amount", PaymentAmount.ToString("F2") }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            // Save payment record
            await SavePaymentRecord(CurrentUser.Id, PaymentAmount, "partial_tuition", session.Id);

            return Redirect(session.Url);
        }
    }
}