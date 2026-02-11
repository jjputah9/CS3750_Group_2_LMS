using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMS.Pages.StudentAccount
{
    [Authorize]
    public class SuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty(SupportsGet = true)]
        public decimal Amount { get; set; }

        public decimal RemainingBalance { get; set; }

        public SuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            // Get student's courses
            var registeredCourses = await _context.Registration
                .Where(r => r.StudentID == userId)
                .Join(_context.Course,
                      r => r.CourseID,
                      c => c.Id,
                      (r, c) => c)
                .ToListAsync();

            var totalCredits = registeredCourses.Sum(c => c.CreditHours);
            var totalTuition = totalCredits * 100;

            // Get total payments
            var totalPaid = await _context.Payments
                .Where(p => p.StudentId == userId && p.Status == "Completed")
                .SumAsync(p => p.Amount);

            RemainingBalance = totalTuition - totalPaid;
        }
    }
}