using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.Data;
using LMS.models;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
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

        public StudentAssignmentsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Assignment> Assignment { get; set; } = new List<Assignment>();

        public int CourseId { get; set; }
        public string CourseHeader { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null || activeCourseId.Value != courseId)
                return Forbid(); //Figure out later why this doesn't work

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Student") return Forbid();

            var course = await _context.Course.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId); // Double check this line for errors

            if (course == null) return Forbid();

            CourseId = courseId;
            CourseHeader = $"{course.DeptName} {course.CourseNum} - {course.CourseTitle}";

            Assignment = await _context.Assignment
                .Where(a => a.CourseId == courseId)
                .Include(a => a.SubmissionType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            return Page();
        }
    }
}
