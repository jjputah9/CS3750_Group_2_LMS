using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class SubmissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubmissionsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }

        // View model for displaying submission data
        public class SubmissionViewModel
        {
            public int SubmissionId { get; set; }
            public string StudentId { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public DateTime SubmittedAt { get; set; }
            public string Status { get; set; } = "";
            public int Grade { get; set; }
            public string FilePath { get; set; } = "";
            public bool HasTextSubmission { get; set; }
        }

        public IList<SubmissionViewModel> Submissions { get; set; }
            = new List<SubmissionViewModel>();

        public async Task<IActionResult> OnGetAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            AssignmentId = assignmentId;

            // Get the assignment details
            Assignment = await _context.Assignment
                .Include(a => a.Course)
                .Include(a => a.SubmissionType)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (Assignment == null)
                return NotFound();

            // Verify the instructor owns this course
            if (Assignment.Course.InstructorEmail != user.Email)
                return Forbid();

            // Get all submissions for this assignment
            var submissions = await _context.submittedAssignments
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();

            // Get all student IDs from submissions
            var studentIds = submissions.Select(s => s.StudentId).ToList();

            // Get student information from AspNetUsers
            var students = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // Map to view model
            Submissions = submissions.Select(s =>
            {
                var student = students.ContainsKey(s.StudentId) ? students[s.StudentId] : null;
                return new SubmissionViewModel
                {
                    SubmissionId = s.submittedAssignmentId,
                    StudentId = s.StudentId,
                    FirstName = student?.fName ?? "Unknown",
                    LastName = student?.lName ?? "Unknown",
                    SubmittedAt = s.submissionDate,
                    Status = s.grade > 0 ? "Graded" : "Submitted",
                    Grade = s.grade,
                    FilePath = s.filePath,
                    HasTextSubmission = !string.IsNullOrEmpty(s.textSubmission)
                };
            })
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToList();

            return Page();
        }
    }
}