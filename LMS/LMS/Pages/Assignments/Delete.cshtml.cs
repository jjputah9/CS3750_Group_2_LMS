using System.Threading.Tasks;
using LMS.Data;
using LMS.models;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Assignment Assignment { get; set; } = default!;

        public int CourseId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null) return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var assignment = await _context.Assignment.FirstOrDefaultAsync(a => a.AssignmentId == id);
            if (assignment == null) return NotFound();

            if (assignment.CourseId != activeCourseId.Value) return Forbid();

            var ownsCourse = await _context.Course.AnyAsync(c =>
                c.Id == assignment.CourseId && c.InstructorEmail == user.Email);

            if (!ownsCourse) return Forbid();

            Assignment = assignment;
            CourseId = assignment.CourseId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null) return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var assignment = await _context.Assignment.FirstOrDefaultAsync(a => a.AssignmentId == Assignment.AssignmentId);
            if (assignment == null) return NotFound();

            if (assignment.CourseId != activeCourseId.Value) return Forbid();

            var ownsCourse = await _context.Course.AnyAsync(c =>
                c.Id == assignment.CourseId && c.InstructorEmail == user.Email);

            if (!ownsCourse) return Forbid();

            _context.Assignment.Remove(assignment);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { courseId = assignment.CourseId });
        }
    }
}
