using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.Data;
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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Assignment> Assignment { get; set; } = new List<Assignment>();

        public int CourseId { get; set; }
        public string CourseHeader { get; set; } = "";

        // Add these properties for course grade distribution
        public CourseGradeDistribution CourseGrades { get; set; }

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null || activeCourseId.Value != courseId)
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var course = await _context.Course.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorEmail == user.Email);

            if (course == null) return Forbid();

            CourseId = courseId;
            CourseHeader = $"{course.DeptName} {course.CourseNum} - {course.CourseTitle}";

            Assignment = await _context.Assignment
                .Where(a => a.CourseId == courseId)
                .Include(a => a.SubmissionType)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            // Calculate course grade distribution
            await CalculateCourseGrades(courseId);

            return Page();
        }

        private async Task CalculateCourseGrades(int courseId)
        {
            // Get all assignments for this course
            var assignments = await _context.Assignment
                .Where(a => a.CourseId == courseId)
                .ToListAsync();

            if (!assignments.Any())
            {
                CourseGrades = new CourseGradeDistribution
                {
                    TotalStudents = 0,
                    GradeCategories = new List<GradeCategory>()
                };
                return;
            }

            // Get all submissions for all assignments in this course
            var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();
            var submissions = await _context.submittedAssignments
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();

            // Get unique student IDs from submissions
            var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();

            // Get student information
            var students = new List<ApplicationUser>();
            if (studentIds.Any())
            {
                students = await _userManager.Users
                    .Where(u => studentIds.Contains(u.Id))
                    .ToListAsync();
            }

            // Calculate total points possible
            var totalPoints = assignments.Sum(a => a.Points);

            // Calculate each student's total grade
            var studentGrades = new List<StudentGradeData>();
            foreach (var student in students)
            {
                var studentSubmissions = submissions.Where(s => s.StudentId == student.Id).ToList();
                var totalEarned = studentSubmissions.Sum(s => s.grade);
                var percentage = totalPoints > 0 ? (totalEarned / (double)totalPoints) * 100 : 0;

                studentGrades.Add(new StudentGradeData
                {
                    StudentId = student.Id,
                    Percentage = percentage
                });
            }

            // Calculate grade distribution
            CourseGrades = new CourseGradeDistribution
            {
                TotalStudents = studentGrades.Count,
                GradeCategories = new List<GradeCategory>()
            };

            if (studentGrades.Any())
            {
                var categories = new Dictionary<string, int>
                {
                    { "A (90-100%)", 0 },
                    { "B (80-89%)", 0 },
                    { "C (70-79%)", 0 },
                    { "D (60-69%)", 0 },
                    { "F (Below 60%)", 0 }
                };

                foreach (var student in studentGrades)
                {
                    if (student.Percentage >= 90)
                        categories["A (90-100%)"]++;
                    else if (student.Percentage >= 80)
                        categories["B (80-89%)"]++;
                    else if (student.Percentage >= 70)
                        categories["C (70-79%)"]++;
                    else if (student.Percentage >= 60)
                        categories["D (60-69%)"]++;
                    else
                        categories["F (Below 60%)"]++;
                }

                CourseGrades.GradeCategories = categories
                    .Where(c => c.Value > 0)
                    .Select(c => new GradeCategory
                    {
                        Name = c.Key,
                        Count = c.Value,
                        Percentage = (c.Value / (double)studentGrades.Count) * 100
                    })
                    .ToList();

                CourseGrades.AveragePercentage = studentGrades.Average(s => s.Percentage);
                CourseGrades.HighestGrade = studentGrades.Max(s => s.Percentage);
                CourseGrades.LowestGrade = studentGrades.Min(s => s.Percentage);
            }
        }
    }

    public class StudentGradeData
    {
        public string StudentId { get; set; }
        public double Percentage { get; set; }
    }

    public class CourseGradeDistribution
    {
        public int TotalStudents { get; set; }
        public double AveragePercentage { get; set; }
        public double HighestGrade { get; set; }
        public double LowestGrade { get; set; }
        public List<GradeCategory> GradeCategories { get; set; }
    }

    // Remove this duplicate class - it already exists in your Submissions page
    // public class GradeCategory { ... }
}