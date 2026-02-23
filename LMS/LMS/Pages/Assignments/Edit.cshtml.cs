using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            ViewData["SubmissionTypeId"] = new SelectList(
                _context.Set<SubmissionType>(), "SubmissionTypeId", "TypeName"
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null) return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only instructors can edit
            if (user.UserType != "Instructor") return Forbid();

            // Fetch assignment from DB
            var existingAssignment = await _context.Assignment
                .FirstOrDefaultAsync(a => a.AssignmentId == Assignment.AssignmentId);

            if (existingAssignment == null) return NotFound();

            // Check ownership of course
            var ownsCourse = await _context.Course.AnyAsync(c =>
                c.Id == existingAssignment.CourseId && c.InstructorEmail == user.Email);

            if (!ownsCourse) return Forbid();

            if (!ModelState.IsValid) return Page();

            // Only now update the assignment fields
            existingAssignment.Title = Assignment.Title;
            existingAssignment.Description = Assignment.Description;
            existingAssignment.Points = Assignment.Points;
            existingAssignment.DueDate = Assignment.DueDate;
            existingAssignment.SubmissionTypeId = Assignment.SubmissionTypeId;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { courseId = existingAssignment.CourseId });
        }
    }
}
