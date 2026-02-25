using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;          // IMPORTANT for Session extensions
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class StudentAssignmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentAssignmentsModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public IList<Assignment> Assignment { get; set; } = new List<Assignment>();
        public HashSet<int> SubmittedAssignmentIds { get; set; } = new HashSet<int>();

        public int CourseId { get; set; }
        public string CourseHeader { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null || activeCourseId.Value != courseId)
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Student") return Forbid();

            var course = await _context.Course.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return Forbid();

            CourseId = courseId;
            CourseHeader = $"{course.DeptName} {course.CourseNum} - {course.CourseTitle}";

            Assignment = await _context.Assignment
                .Where(a => a.CourseId == courseId)
                .Include(a => a.SubmissionType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            // Check which assignments have been submitted by querying the database
            var submittedAssignments = await _context.submittedAssignments
                .Where(s => s.StudentId == user.Id)
                .Select(s => s.AssignmentId)
                .ToListAsync();

            SubmittedAssignmentIds = new HashSet<int>(submittedAssignments);

            return Page();
        }
    }
}
