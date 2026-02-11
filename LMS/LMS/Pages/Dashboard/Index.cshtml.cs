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

        // Get assignments for the selected course
        public List<Assignment> Assignments { get; set; } = new();

        // Fetch the user on GET request
        public async Task OnGetAsync()
        {
            // get logged in user
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            // show courses the student is registered for or courses the instructor is responsible for
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

                // if student, also get assignments for registered courses
                Assignments = await _context.Registration
                    .Where(r => r.StudentID == CurrentUser.Id)
                    .Join(_context.Assignment,
                          r => r.CourseID,
                          a => a.CourseId,
                          (r, a) => a)
                    .Where(a => a.DueDate >= DateTime.Now) // only show upcoming assignments
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .Include(a => a.Course) // include course name for display
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

        public async Task<IActionResult> OnGetGoToAssignmentsAsync(int courseId)
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return Challenge();

            if (CurrentUser.UserType != "Instructor")
                return Forbid();

            var ownsCourse = await _context.Course
                .AnyAsync(c => c.Id == courseId && c.InstructorEmail == CurrentUser.Email);

            if (!ownsCourse)
                return Forbid();

            HttpContext.Session.SetInt32("ActiveCourseId", courseId);

            return RedirectToPage("/Assignments/Index", new { courseId });
        }

        public async Task<IActionResult> OnGetGoToStudentAssignmentsAsync(int courseId)
        { 
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return Challenge();

            if (CurrentUser.UserType != "Student")
                return Forbid();

            var isRegistered = await _context.Registration
                .AnyAsync(r => r.CourseID == courseId && r.StudentID == CurrentUser.Id);

            if (!isRegistered)
                return Forbid();

            HttpContext.Session.SetInt32("ActiveCourseId", courseId);

            return RedirectToPage("/Assignments/StudentAssignments", new { courseId });
        }
    }
}
