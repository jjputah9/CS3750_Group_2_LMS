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
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

        public Assignment Assignment { get; set; } = default!;
        public submittedAssignment ExistingSubmission { get; set; } = default!;
        public GradeBoxPlotData boxPlotData { get; set; } = default!;
        public string SubmissionTypeName { get; set; } = string.Empty;
        public bool HasSubmitted { get; set; }

        [BindProperty]
        public IFormFile? SubmittedFile { get; set; }

        [BindProperty]
        public string TextSubmission { get; set; } = string.Empty;

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

            HasSubmitted = await _context.submittedAssignments
                .AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId == user.Id);

            if (HasSubmitted) // If there is a submission, load it
            {
                ExistingSubmission = await _context.submittedAssignments
                .FirstAsync(s => s.AssignmentId == assignmentId && s.StudentId == user.Id);

                if (ExistingSubmission.GradedAt != DateTime.UnixEpoch) // If assignment has been graded, calc graph
                {
                    var submissions = await _context.submittedAssignments
                    .Where(s => s.AssignmentId == assignmentId && s.GradedAt != DateTime.UnixEpoch)
                    .Select(s => s.grade)
                    .OrderBy(s => s)
                    .ToArrayAsync();

                    boxPlotData = new GradeBoxPlotData(submissions, Assignment.Points);
                }
            }

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

            var existingSubmission = await _context.submittedAssignments
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == user.Id);

            if (existingSubmission != null)
            {
                TempData["ErrorMessage"] = "You have already submitted this assignment.";
                return RedirectToPage("/Assignments/StudentAssignments", new { courseId = Assignment.CourseId });
            }

            var submissionsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "submissions", assignmentId.ToString());
            string filePath = "";

            submittedAssignment? submission = null;

            if (SubmissionTypeName == "File Upload")
            {
                if (SubmittedFile == null || SubmittedFile.Length == 0)
                {
                    ModelState.AddModelError("SubmittedFile", "Please select a file to upload.");
                    return Page();
                }

                if (!Directory.Exists(submissionsFolder))
                {
                    Directory.CreateDirectory(submissionsFolder);
                }

                var fileExtension = Path.GetExtension(SubmittedFile.FileName);
                var fileName = $"{user.Id}_{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                filePath = Path.Combine(submissionsFolder, fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await SubmittedFile.CopyToAsync(stream);
                }

                submission = new submittedAssignment
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id,
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

                if (!Directory.Exists(submissionsFolder))
                {
                    Directory.CreateDirectory(submissionsFolder);
                }

                var fileName = $"{user.Id}_{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                filePath = Path.Combine(submissionsFolder, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, TextSubmission);

                submission = new submittedAssignment
                {
                    AssignmentId = assignmentId,
                    StudentId = user.Id,
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

            if (submission != null && Assignment.Course != null && !string.IsNullOrWhiteSpace(Assignment.Course.InstructorEmail))
            {
                var instructor = await _userManager.FindByEmailAsync(Assignment.Course.InstructorEmail);

                if (instructor != null)
                {
                    var notification = new Notifications
                    {
                        UserId = instructor.Id,
                        NotificationType = "AssignmentSubmitted",
                        AssignmentId = assignmentId,
                        SubmittedAssignmentId = submission.submittedAssignmentId,
                        Message = $"{user.fName} {user.lName} submitted: {Assignment.Title}",
                        NotificationDeleted = false,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }

            return Page();
        }

        /// <summary>
        /// Returns true if the graph should be displayed (if a submission exists and has been graded)
        /// </summary>
        /// <returns></returns>
        public bool ShouldGraphBeDisplayed()
        {
            return HasSubmitted && (ExistingSubmission.GradedAt != DateTime.UnixEpoch);
        }

        /// <summary>
        /// Converts an int/int relationship to a percentage double
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double ToPercent(int? value, int? max)
        {
            return (value != null && max != null) ? (double)value * 100 / (double)max : 0;
        }
    }

    public class GradeBoxPlotData
    {
        public int[] allData { get; set; }
        public double AverageGrade { get; set; }
        public double AveragePercentage { get; set; }
        public double Quartile1 { get; set; }
        public double Median { get; set; }
        public double Quartile3 { get; set; }
        public int HighestGrade { get; set; }
        public double HighestPercentage { get; set; }
        public int LowestGrade { get; set; }
        public double LowestPercentage { get; set; }

        /// <summary>
        /// Constructor for the boxplot data, assumes the list is already sorted
        /// </summary>
        /// <param name="grades"></param>
        public GradeBoxPlotData(int[] grades, int maxGrade)
        {
            allData = grades;
            double sum = 0;
            for (int i = 0; i < grades.Length; i++)
            {
                sum += grades[i];
            }
            AverageGrade = Math.Round(sum / (double)grades.Length, 1);
            AveragePercentage = Math.Round(sum * 100 / (double)(grades.Length * maxGrade), 1);
            Quartile1 = grades[(int)Math.Floor(grades.Length / 4d)];
            Median = grades[(int)Math.Floor(grades.Length / 2d)];
            Quartile3 = grades[(int)Math.Floor(3 * grades.Length / 4d)];
            HighestGrade = grades.Last();
            HighestPercentage = Math.Round(grades.Last() * 100 / (double)maxGrade, 1);
            LowestGrade = grades.First();
            LowestPercentage = Math.Round(grades.First() * 100 / (double)maxGrade, 1);
        }
    }
}