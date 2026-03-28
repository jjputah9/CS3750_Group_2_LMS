using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LMS.Models;
using LMS.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Dashboard
{
    [Authorize]
    public class DashboardIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        public readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardIndexModel(
            UserManager<ApplicationUser> userManager
            , ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // property that holds the current user
        public ApplicationUser? CurrentUser { get; set; }

        // Courses for the dashboard
        public List<Course> Courses { get; set; } = new();

        // Get assignments for the selected course
        public List<Assignment> Assignments { get; set; } = new();

        // Dictionary to store course grades: CourseId -> (Percentage, LetterGrade)
        public Dictionary<int, (double Percentage, string LetterGrade)> CourseGrades { get; set; } = new();

        // Fetch the user on GET request
        public async Task OnGetAsync()
        {
            // get logged in user
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            // check cache for courses
            var key = $"UserCourses_{CurrentUser.Id}";
            var cachedCourses = HttpContext.Session.GetString(key);

            // check if cache is not null/empty
            if (!string.IsNullOrEmpty(cachedCourses))
            {
                Courses = System.Text.Json.JsonSerializer.Deserialize<List<Course>>(cachedCourses)!;
                
                // Still need to calculate grades even with cached courses
                if (CurrentUser.UserType == "Student")
                {
                    await CalculateCourseGrades();
                }
                return;
            }

            // if cache is null, DB hit required
            // show courses the student is registered for or courses the instructor is responsible for
            if (CurrentUser.UserType == "Student")
            {
                // get courses this student is regestered for
                Courses = await _context.Registration
                    .Where(r => r.StudentID == CurrentUser.Id)
                    .Join(_context.Course,
                          r => r.CourseID,
                          c => c.Id,
                          (r, c) => c)
                    .ToListAsync();

                // Calculate grades for each course
                await CalculateCourseGrades();

                // if student, also get assignments for registered courses
                Assignments = await _context.Registration
                    .Where(r => r.StudentID == CurrentUser.Id)
                    .Join(_context.Assignment,
                          r => r.CourseID,
                          a => a.CourseId,
                          (r, a) => a)
                    .Where(a => a.DueDate >= DateTime.Now) // only show upcoming assignments
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .Include(a => a.Course) // include course name for display
                    .ToListAsync();
            }
            else if (CurrentUser.UserType == "Instructor")
            {
                // get courses the Instructor is responsible for (created)
                Courses = await _context.Course
                    .Where(r => r.InstructorEmail == CurrentUser.Email)
                    .ToListAsync();
            }

            // store new course data in cache
            HttpContext.Session.SetString(
                "UserCourses",
                System.Text.Json.JsonSerializer.Serialize(Courses)
            );

        }

        private async Task CalculateCourseGrades()
        {
            if (CurrentUser == null) return;

            foreach (var course in Courses)
            {
                // Get all assignments for this course
                var assignments = await _context.Assignment
                    .Where(a => a.CourseId == course.Id)
                    .ToListAsync();

                // Calculate total possible points
                int totalPossiblePoints = assignments.Sum(a => a.Points);

                if (totalPossiblePoints == 0)
                {
                    CourseGrades[course.Id] = (0, "N/A");
                    continue;
                }

                // Get student's submissions and sum their grades
                var submissions = await _context.submittedAssignments
                    .Where(s => s.StudentId == CurrentUser.Id && 
                                assignments.Select(a => a.AssignmentId).Contains(s.AssignmentId))
                    .ToListAsync();

                int earnedPoints = submissions.Sum(s => s.grade);

                // Calculate percentage
                double percentage = (double)earnedPoints / totalPossiblePoints * 100;

                // Determine letter grade
                string letterGrade = GetLetterGrade(percentage);

                CourseGrades[course.Id] = (percentage, letterGrade);
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

        public async Task<IActionResult> OnGetGoToAssignmentsAsync(int courseId)
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return Challenge();

            if (CurrentUser.UserType != "Instructor")
                return Forbid();

            var ownsCourse = await _context.Course
                .AnyAsync(c => c.Id == courseId && c.InstructorEmail == CurrentUser.Email);

            if (!ownsCourse)
                return Forbid();

            HttpContext.Session.SetInt32("ActiveCourseId", courseId);

            return RedirectToPage("/Assignments/Index", new { courseId });
        }

        public async Task<IActionResult> OnGetGoToStudentAssignmentsAsync(int courseId)
        { 
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null) return Challenge();

            if (CurrentUser.UserType != "Student")
                return Forbid();

            var isRegistered = await _context.Registration
                .AnyAsync(r => r.CourseID == courseId && r.StudentID == CurrentUser.Id);

            if (!isRegistered)
                return Forbid();

            HttpContext.Session.SetInt32("ActiveCourseId", courseId);

            return RedirectToPage("/Assignments/StudentAssignments", new { courseId });
        }
    }
}
