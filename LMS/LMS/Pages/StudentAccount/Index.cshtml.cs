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

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<Course> RegisteredCourses { get; set; } = new();
        public int TotalCredits { get; set; }
        public decimal Tuition { get; set; }
        public decimal TotalPaid { get; set; }
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
            if (CurrentUser == null || CurrentUser.UserType != "Student")
                return;

            RegisteredCourses = await _context.Registration
                .Where(r => r.StudentID == CurrentUser.Id)
                .Join(_context.Course,
                      r => r.CourseID,
                      c => c.Id,
                      (r, c) => c)
                .ToListAsync();

            TotalCredits = RegisteredCourses.Sum(c => c.CreditHours);

            decimal totalTuition = TotalCredits * 100; // $100 per credit

            TotalPaid = await _context.Payments
                .Where(p => p.StudentId == CurrentUser.Id && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            Tuition = totalTuition - TotalPaid;

            RecentPayments = await _context.Payments
                .Where(p => p.StudentId == CurrentUser.Id && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();
        }

        // ✅ FULL TUITION PAYMENT
        public async Task<IActionResult> OnPostCreateCheckoutSessionAsync()
        {
            await LoadUserData();

            if (Tuition <= 0)
            {
                TempData["ErrorMessage"] = "No tuition balance to pay.";
                return RedirectToPage();
            }

            long amountInCents = (long)(Tuition * 100);

            StripeConfiguration.ApiKey = "sk_test_51SwPAQQe5XKdlTbWrbt1qkEWTNs0E9XmqJTf9Ou7IxvAxT0izVE6p9Ha9RYhNk5SXNqabHVLTpkyNrNocuh1Azuk00qFh3nka3";

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Tuition Payment"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = "https://localhost:5001/StudentAccount/Index",
                CancelUrl = "https://localhost:5001/StudentAccount/Index",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // ✅ SAVE PAYMENT TO DATABASE
            var payment = new Payment
            {
                StudentId = CurrentUser.Id,
                Amount = Tuition,
                PaymentType = "Full Tuition",
                StripeSessionId = session.Id,
                PaymentDate = DateTime.UtcNow,
                Status = "Completed"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        // ✅ PARTIAL / CUSTOM PAYMENT
        public async Task<IActionResult> OnPostCreateCustomCheckoutSessionAsync()
        {
            await LoadUserData();

            if (PaymentAmount <= 0 || PaymentAmount > Tuition)
            {
                TempData["ErrorMessage"] = "Invalid payment amount.";
                return RedirectToPage();
            }

            long amountInCents = (long)(PaymentAmount * 100);

            StripeConfiguration.ApiKey = "sk_test_51SwPAQQe5XKdlTbWrbt1qkEWTNs0E9XmqJTf9Ou7IxvAxT0izVE6p9Ha9RYhNk5SXNqabHVLTpkyNrNocuh1Azuk00qFh3nka3";

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInCents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Tuition Payment"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Success",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/StudentAccount/Index",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // ✅ SAVE PAYMENT TO DATABASE
            var payment = new Payment
            {
                StudentId = CurrentUser.Id,
                Amount = PaymentAmount,
                PaymentType = "Partial Tuition",
                StripeSessionId = session.Id,
                PaymentDate = DateTime.UtcNow,
                Status = "Completed"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }
    }
}