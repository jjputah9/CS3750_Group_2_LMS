using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Pages.StudentAccount
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser CurrentUser { get; set; }

        public List<Course> RegisteredCourses { get; set; } = new();

        public int TotalCredits { get; set; }
        public decimal Tuition { get; set; }

        public async Task OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return;

            // Only for students
            if (CurrentUser.UserType != "Student")
                return;

            // Load courses (reuse dashboard logic)
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
        }
    }
}
