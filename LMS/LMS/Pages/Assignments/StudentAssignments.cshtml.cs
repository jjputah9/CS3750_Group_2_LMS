using System;
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

        // Grade properties
        public double GradePercentage { get; set; }
        public string LetterGrade { get; set; } = "N/A";

        // Box plot data for course grade distribution
        public CourseGradeBoxPlotData? BoxPlotData { get; set; }
        public bool ShouldShowGradeDistribution { get; set; }

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

            // Calculate the student's grade for this course
            await CalculateGrade(user.Id, courseId);

            // Calculate grade distribution for box plot
            await CalculateGradeDistribution(courseId);

            return Page();
        }

        private async Task CalculateGrade(string studentId, int courseId)
        {
            // Get all assignments for this course
            var assignmentIds = Assignment.Select(a => a.AssignmentId).ToList();
            
            // Calculate total possible points
            int totalPossiblePoints = Assignment.Sum(a => a.Points);

            if (totalPossiblePoints == 0)
            {
                GradePercentage = 0;
                LetterGrade = "N/A";
                return;
            }

            // Get student's submissions and sum their grades
            var submissions = await _context.submittedAssignments
                .Where(s => s.StudentId == studentId && assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();

            int earnedPoints = submissions.Sum(s => s.grade);

            // Calculate percentage
            GradePercentage = (double)earnedPoints / totalPossiblePoints * 100;

            // Determine letter grade
            LetterGrade = GetLetterGrade(GradePercentage);
        }

        private async Task CalculateGradeDistribution(int courseId)
        {
            // Get all students registered in this course
            var registeredStudentIds = await _context.Registration
                .Where(r => r.CourseID == courseId)
                .Select(r => r.StudentID)
                .ToListAsync();

            if (registeredStudentIds.Count == 0)
            {
                ShouldShowGradeDistribution = false;
                return;
            }

            // Get all assignment IDs for this course
            var assignmentIds = Assignment.Select(a => a.AssignmentId).ToList();
            int totalPossiblePoints = Assignment.Sum(a => a.Points);

            if (totalPossiblePoints == 0)
            {
                ShouldShowGradeDistribution = false;
                return;
            }

            // Calculate each student's grade percentage
            var studentGrades = new List<double>();

            foreach (var studentId in registeredStudentIds)
            {
                var submissions = await _context.submittedAssignments
                    .Where(s => s.StudentId == studentId && 
                                assignmentIds.Contains(s.AssignmentId) &&
                                s.GradedAt != DateTime.UnixEpoch) // Only graded submissions
                    .ToListAsync();

                // Only include students who have at least one graded submission
                if (submissions.Any())
                {
                    int earnedPoints = submissions.Sum(s => s.grade);
                    double percentage = (double)earnedPoints / totalPossiblePoints * 100;
                    studentGrades.Add(Math.Round(percentage, 1));
                }
            }

            // Need at least 2 students with graded work to show distribution
            if (studentGrades.Count >= 2)
            {
                studentGrades.Sort();
                BoxPlotData = new CourseGradeBoxPlotData(studentGrades.ToArray());
                ShouldShowGradeDistribution = true;
            }
            else
            {
                ShouldShowGradeDistribution = false;
            }
        }

        private string GetLetterGrade(double percentage)
        {
            if (percentage >= 93) return "A";
            if (percentage >= 90) return "A-";
            if (percentage >= 87) return "B+";
            if (percentage >= 83) return "B";
            if (percentage >= 80) return "B-";
            if (percentage >= 77) return "C+";
            if (percentage >= 73) return "C";
            if (percentage >= 70) return "C-";
            if (percentage >= 67) return "D+";
            if (percentage >= 63) return "D";
            if (percentage >= 60) return "D-";
            return "F";
        }
    }

    /// <summary>
    /// Box plot data for course-level grade distribution (using percentages)
    /// </summary>
    public class CourseGradeBoxPlotData
    {
        public double[] AllData { get; set; }
        public double AveragePercentage { get; set; }
        public double Quartile1 { get; set; }
        public double Median { get; set; }
        public double Quartile3 { get; set; }
        public double HighestPercentage { get; set; }
        public double LowestPercentage { get; set; }

        /// <summary>
        /// Constructor for the course grade boxplot data, assumes the list is already sorted
        /// </summary>
        /// <param name="grades">Sorted array of grade percentages</param>
        public CourseGradeBoxPlotData(double[] grades)
        {
            AllData = grades;
            AveragePercentage = Math.Round(grades.Average(), 1);
            Quartile1 = grades[(int)Math.Floor(grades.Length / 4d)];
            Median = grades[(int)Math.Floor(grades.Length / 2d)];
            Quartile3 = grades[(int)Math.Floor(3 * grades.Length / 4d)];
            HighestPercentage = grades.Last();
            LowestPercentage = grades.First();
        }
    }
}
