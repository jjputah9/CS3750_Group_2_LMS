using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Calendar
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Course> AllCourses { get; set; } = new List<Course>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                AllCourses = await _context.Course.ToListAsync();
                return;
            }

            if (string.Equals(user.UserType, "Instructor", StringComparison.OrdinalIgnoreCase))
            {
                AllCourses = await _context.Course
                    .Where(c => c.InstructorEmail == user.Email)
                    .ToListAsync();
            }
            else if (string.Equals(user.UserType, "Student", StringComparison.OrdinalIgnoreCase))
            {
                // The database uses a `Registration` table that links StudentID and CourseId.
                // Use the registration set to join back to Course and return the student's courses.
                // This uses _context.Set<Registration>() so it works even if DbSet<Registration> is not declared.
                AllCourses = await _context.Set<Registration>()
                    .Where(r => r.StudentID == user.Id)                // property name matches your model (StudentID)
                    .Join(
                        _context.Course,
                        r => r.CourseID,                                // registration -> course FK
                        c => c.Id,                                     // course PK
                        (r, c) => c
                    )
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                AllCourses = await _context.Course.ToListAsync();
            }
        }
    }
}
