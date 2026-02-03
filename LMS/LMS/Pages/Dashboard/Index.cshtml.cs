using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LMS.Models;
using LMS.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Dashboard
{
    [Authorize]
    public class DashboardIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardIndexModel(
            UserManager<ApplicationUser> userManager
            , ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // property that holds the current user
        public ApplicationUser? CurrentUser { get; set; }

        // Courses for the dashboard
        public List<Course> Courses { get; set; } = new();

        // Fetch the user on GET request
        public async Task OnGetAsync()
        {
            // get logged in user
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            if (CurrentUser.UserType == "Student")
            {
                // get courses this student is regestered for
                Courses = await _context.Registration
                    .Where(r => r.StudentID == CurrentUser.Id)
                    .Join(_context.Course,
                          r => r.CourseID,
                          c => c.Id,
                          (r, c) => c)
                    .ToListAsync();
            }
            else if (CurrentUser.UserType == "Instructor")
            {
                // get courses the Instructor is responsible for (created)
                Courses = await _context.Course
                    .Where(r => r.InstructorEmail == CurrentUser.Email)
                    .ToListAsync();
            }
        }
    }
}
