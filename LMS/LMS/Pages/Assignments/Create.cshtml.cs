using System;
using System.Linq;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Assignment Assignment { get; set; } = new Assignment();

        public int CourseId { get; set; }
        public string CourseHeader { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null || activeCourseId.Value != courseId)
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var course = await _context.Course.FirstOrDefaultAsync(c =>
                c.Id == courseId && c.InstructorEmail == user.Email);

            if (course == null) return Forbid();

            CourseId = courseId;
            CourseHeader = $"{course.DeptName} {course.CourseNum} - {course.CourseTitle}";
            Assignment.CourseId = courseId;

            ViewData["SubmissionTypeId"] = new SelectList(
                _context.Set<SubmissionType>(),
                "SubmissionTypeId",
                "TypeName"
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int courseId)
        {
            var activeCourseId = HttpContext.Session.GetInt32("ActiveCourseId");
            if (activeCourseId == null || activeCourseId.Value != courseId)
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var course = await _context.Course.FirstOrDefaultAsync(c =>
                c.Id == courseId && c.InstructorEmail == user.Email);

            if (course == null) return Forbid();

            Assignment.CourseId = courseId;

            CourseId = courseId;
            CourseHeader = $"{course.DeptName} {course.CourseNum} - {course.CourseTitle}";

            if (!ModelState.IsValid)
            {
                ViewData["SubmissionTypeId"] = new SelectList(
                    _context.Set<SubmissionType>(),
                    "SubmissionTypeId",
                    "TypeName",
                    Assignment.SubmissionTypeId
                );

                return Page();
            }

            _context.Assignment.Add(Assignment);
            await _context.SaveChangesAsync();

            var studentIds = await _context.Registration
                .Where(r => r.CourseID == courseId && r.StudentID != null)
                .Select(r => r.StudentID!)
                .Distinct()
                .ToListAsync();

            if (studentIds.Any())
            {
                var notifications = studentIds.Select(studentId => new Notifications
                {
                    UserId = studentId,
                    NotificationType = "AssignmentCreated",
                    AssignmentId = Assignment.AssignmentId,
                    SubmittedAssignmentId = null,
                    Message = $"New assignment posted in {course.DeptName} {course.CourseNum}: {Assignment.Title}",
                    NotificationDeleted = false,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Assignments/Index", new { courseId });
        }
    }
}