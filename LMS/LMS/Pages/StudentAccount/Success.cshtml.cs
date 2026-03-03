using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LMS.Pages.StudentAccount
{
    [Authorize]
    public class SuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal Amount { get; set; }

        public async Task OnGetAsync()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (studentId == null)
                return;

            // Get the latest completed payment
            var latestPayment = await _context.Payments
                .Where(p => p.StudentId == studentId && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefaultAsync();

            if (latestPayment != null)
            {
                Amount = latestPayment.Amount;
            }
        }
    }
}