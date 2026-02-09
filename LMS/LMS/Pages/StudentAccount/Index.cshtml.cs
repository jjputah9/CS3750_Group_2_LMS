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

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<Course> RegisteredCourses { get; set; } = new();
        public int TotalCredits { get; set; }
        public decimal Tuition { get; set; }
        public string StripePublishableKey { get; set; }

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

            // Tuition = $100 per credit
            Tuition = TotalCredits * 100;

            // Get Stripe publishable key
            StripePublishableKey = _configuration["Stripe:PublishableKey"];
        }

        // Full payment endpoint
        public async Task<IActionResult> OnPostCreateCheckoutSessionAsync()
        {
            await LoadUserData(); // Load data again for POST

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
                SuccessUrl = Url.Page("/StudentAccount/Success", null, new { amount = Tuition }, Request.Scheme),
                CancelUrl = Url.Page("/StudentAccount/Index", null, null, Request.Scheme),
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

            return Redirect(session.Url);
        }

        // Custom payment endpoint
        public async Task<IActionResult> OnPostCreateCustomCheckoutSessionAsync()
        {
            await LoadUserData(); // Load data again for POST

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
                SuccessUrl = Url.Page("/StudentAccount/Success", null, new { amount = PaymentAmount }, Request.Scheme),
                CancelUrl = Url.Page("/StudentAccount/Index", null, null, Request.Scheme),
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

            return Redirect(session.Url);
        }
    }
}