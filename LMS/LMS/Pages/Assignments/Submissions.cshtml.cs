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

        // Grade distribution for pie chart
        public GradeDistributionData GradeDistribution { get; set; }

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

            // Calculate grade distribution for pie chart
            GradeDistribution = CalculateGradeDistribution(submissions, Assignment.Points);

            return Page();
        }

        private GradeDistributionData CalculateGradeDistribution(List<submittedAssignment> submissions, int totalPoints)
        {
            var distribution = new GradeDistributionData
            {
                TotalStudents = submissions.Count,
                GradedStudents = submissions.Count(s => s.grade > 0),
                UngradedStudents = submissions.Count(s => s.grade == 0),
                GradeCategories = new List<GradeCategory>()
            };

            if (totalPoints > 0)
            {
                var categories = new Dictionary<string, int>
                {
                    { "A (90-100%)", 0 },
                    { "B (80-89%)", 0 },
                    { "C (70-79%)", 0 },
                    { "D (60-69%)", 0 },
                    { "F (Below 60%)", 0 },
                    { "Not Graded", 0 }
                };

                foreach (var submission in submissions)
                {
                    if (submission.grade == 0)
                    {
                        categories["Not Graded"]++;
                    }
                    else
                    {
                        var percentage = (submission.grade / (double)totalPoints) * 100;

                        if (percentage >= 90)
                            categories["A (90-100%)"]++;
                        else if (percentage >= 80)
                            categories["B (80-89%)"]++;
                        else if (percentage >= 70)
                            categories["C (70-79%)"]++;
                        else if (percentage >= 60)
                            categories["D (60-69%)"]++;
                        else
                            categories["F (Below 60%)"]++;
                    }
                }

                distribution.GradeCategories = categories
                    .Where(c => c.Value > 0)
                    .Select(c => new GradeCategory
                    {
                        Name = c.Key,
                        Count = c.Value,
                        Percentage = (c.Value / (double)submissions.Count) * 100
                    })
                    .ToList();

                // Calculate statistics for graded students only
                var gradedSubmissions = submissions.Where(s => s.grade > 0).ToList();
                if (gradedSubmissions.Any())
                {
                    var grades = gradedSubmissions.Select(s => s.grade);
                    distribution.AverageGrade = Math.Round(grades.Average(), 1);
                    distribution.AveragePercentage = Math.Round((distribution.AverageGrade / totalPoints) * 100, 1);
                    distribution.HighestGrade = grades.Max();
                    distribution.LowestGrade = grades.Min();
                }
            }

            return distribution;
        }
    }

    public class GradeDistributionData
    {
        public int TotalStudents { get; set; }
        public int GradedStudents { get; set; }
        public int UngradedStudents { get; set; }
        public double AverageGrade { get; set; }
        public double AveragePercentage { get; set; }
        public int HighestGrade { get; set; }
        public int LowestGrade { get; set; }
        public List<GradeCategory> GradeCategories { get; set; }
    }

    public class GradeCategory
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}