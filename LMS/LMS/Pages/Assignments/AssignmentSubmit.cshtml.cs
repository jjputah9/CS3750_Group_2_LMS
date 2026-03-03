using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class AssignmentSubmitModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AssignmentSubmitModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public Assignment Assignment { get; set; }
        public string SubmissionTypeName { get; set; }
        public bool HasSubmitted { get; set; }

        [BindProperty]
        public IFormFile SubmittedFile { get; set; }

        [BindProperty]
        public string TextSubmission { get; set; }

        public async Task<IActionResult> OnGetAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Student") return Forbid();

            Assignment = await _context.Assignment
                .Include(a => a.SubmissionType)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (Assignment == null)
                return NotFound();

            SubmissionTypeName = Assignment.SubmissionType?.TypeName ?? "";

            // Check if student has already submitted (check database)
            HasSubmitted = await _context.submittedAssignments
                .AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId.ToString() == user.Id);

            HttpContext.Session.SetInt32("ActiveCourseId", Assignment.CourseId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Student") return Forbid();

            Assignment = await _context.Assignment
                .Include(a => a.SubmissionType)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (Assignment == null)
                return NotFound();

            SubmissionTypeName = Assignment.SubmissionType?.TypeName ?? "";

            // Check if already submitted (check database)
            var existingSubmission = await _context.submittedAssignments
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId.ToString() == user.Id);

            if (existingSubmission != null)
            {
                TempData["ErrorMessage"] = "You have already submitted this assignment.";
                return RedirectToPage("/Assignments/StudentAssignments", new { courseId = Assignment.CourseId });
            }

            var submissionsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "submissions", assignmentId.ToString());
            string filePath = "";

            if (SubmissionTypeName == "File Upload")
            {
                if (SubmittedFile == null || SubmittedFile.Length == 0)
                {
                    ModelState.AddModelError("SubmittedFile", "Please select a file to upload.");
                    return Page();
                }

                // Create submissions folder structure: wwwroot/submissions/assignmentId/
                if (!Directory.Exists(submissionsFolder))
                {
                    Directory.CreateDirectory(submissionsFolder);
                }

                // Create filename: studentId_assignmentId_timestamp_originalFileName
                var fileExtension = Path.GetExtension(SubmittedFile.FileName);
                var fileName = $"{user.Id}_{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                filePath = Path.Combine(submissionsFolder, fileName);

                // Save the file
                using (var stream = System.IO.File.Create(filePath))
                {
                    await SubmittedFile.CopyToAsync(stream);
                }

                // Save to database
                var submission = new submittedAssignment
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id, // NOTE: This assumes user.Id can be parsed to int
                    submissionTypeId = Assignment.SubmissionTypeId,
                    filePath = $"/submissions/{assignmentId}/{fileName}",
                    submissionDate = DateTime.Now,
                    textSubmission = "",
                    grade = 0
                };

                _context.submittedAssignments.Add(submission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "File submitted successfully!";
            }
            else if (SubmissionTypeName == "Text Entry")
            {
                if (string.IsNullOrWhiteSpace(TextSubmission))
                {
                    ModelState.AddModelError("TextSubmission", "Please enter your submission.");
                    return Page();
                }

                // Create submissions folder structure: wwwroot/submissions/assignmentId/
                if (!Directory.Exists(submissionsFolder))
                {
                    Directory.CreateDirectory(submissionsFolder);
                }

                // Create filename: studentId_assignmentId_timestamp.txt
                var fileName = $"{user.Id}_{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                filePath = Path.Combine(submissionsFolder, fileName);

                // Save text submission as a text file
                await System.IO.File.WriteAllTextAsync(filePath, TextSubmission);

                // Save to database
                var submission = new submittedAssignment
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id, // NOTE: This assumes user.Id can be parsed to int
                    submissionTypeId = Assignment.SubmissionTypeId,
                    filePath = $"/submissions/{assignmentId}/{fileName}",
                    submissionDate = DateTime.Now,
                    textSubmission = TextSubmission,
                    grade = 0
                };

                _context.submittedAssignments.Add(submission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Submission saved successfully!";
            }

            return RedirectToPage("/Assignments/StudentAssignments", new { courseId = Assignment.CourseId });
        }
    }
}
