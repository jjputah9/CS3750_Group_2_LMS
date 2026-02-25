using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class GradeSubmissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GradeSubmissionsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public submittedAssignment Submission { get; set; }
        public Assignment Assignment { get; set; }
        public ApplicationUser Student { get; set; }
        public string SubmissionTypeName { get; set; }
        public string TextContent { get; set; }

        [BindProperty]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Grade must be zero or greater")]
        public int Grade { get; set; }

        public async Task<IActionResult> OnGetAsync(int submissionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            Submission = await _context.submittedAssignments
                .Include(s => s.SubmissionType)
                .FirstOrDefaultAsync(s => s.submittedAssignmentId == submissionId);

            if (Submission == null)
                return NotFound();

            Assignment = await _context.Assignment
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == Submission.AssignmentId);

            if (Assignment == null)
                return NotFound();

            // Verify instructor owns this course
            if (Assignment.Course.InstructorEmail != user.Email)
                return Forbid();

            Student = await _userManager.FindByIdAsync(Submission.StudentId);

            SubmissionTypeName = Submission.SubmissionType?.TypeName ?? "";

            // If text submission, read the text file content
            if (SubmissionTypeName == "Text Entry" && !string.IsNullOrEmpty(Submission.filePath))
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, Submission.filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    TextContent = await System.IO.File.ReadAllTextAsync(fullPath);
                }
            }

            Grade = Submission.grade;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int submissionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            Submission = await _context.submittedAssignments
                .FirstOrDefaultAsync(s => s.submittedAssignmentId == submissionId);

            if (Submission == null)
                return NotFound();

            Assignment = await _context.Assignment
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == Submission.AssignmentId);

            if (Assignment == null)
                return NotFound();

            // Verify instructor owns this course
            if (Assignment.Course.InstructorEmail != user.Email)
                return Forbid();

            if (!ModelState.IsValid)
            {
                Student = await _userManager.FindByIdAsync(Submission.StudentId);
                SubmissionTypeName = (await _context.Assignment
                    .Include(a => a.SubmissionType)
                    .FirstOrDefaultAsync(a => a.AssignmentId == Submission.AssignmentId))
                    ?.SubmissionType?.TypeName ?? "";
                return Page();
            }

            // Validate grade doesn't exceed assignment points
            if (Grade > Assignment.Points)
            {
                ModelState.AddModelError("Grade", $"Grade cannot exceed {Assignment.Points} points");
                Student = await _userManager.FindByIdAsync(Submission.StudentId);
                SubmissionTypeName = (await _context.Assignment
                    .Include(a => a.SubmissionType)
                    .FirstOrDefaultAsync(a => a.AssignmentId == Submission.AssignmentId))
                    ?.SubmissionType?.TypeName ?? "";
                return Page();
            }

            Submission.grade = Grade;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Grade saved successfully!";
            return RedirectToPage("/Assignments/Submissions", new { assignmentId = Assignment.AssignmentId });
        }

        public IActionResult OnGetDownloadFile(int submissionId)
        {
            var submission = _context.submittedAssignments
                .FirstOrDefault(s => s.submittedAssignmentId == submissionId);

            if (submission == null || string.IsNullOrEmpty(submission.filePath))
                return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, submission.filePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var fileName = Path.GetFileName(fullPath);
            var fileBytes = System.IO.File.ReadAllBytes(fullPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }
    }
}
