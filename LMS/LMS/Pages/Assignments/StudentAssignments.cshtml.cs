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
}
