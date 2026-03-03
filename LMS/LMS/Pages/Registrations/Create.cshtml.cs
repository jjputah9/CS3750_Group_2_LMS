using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.Pages.Registrations
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
        }

        // Registration property for Add/Drop
        [BindProperty]
        public Registration Registration { get; set; } = default!;

        // List of courses displayed on page
        public IList<Course> Courses { get; set; } = default!;

        // Current logged-in user
        public ApplicationUser? CurrentUser { get; set; } = default!;

        // Search/filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedDepartment { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCredits { get; set; }

        // Dropdown lists
        public SelectList? DepartmentList { get; set; }
        public SelectList? CreditList { get; set; }

        // GET handler
        public async Task OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);

            // Start query for courses
            IQueryable<Course> courseQuery = _context.Course;

            // Apply Search filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                courseQuery = courseQuery.Where(c =>
                    c.CourseTitle.Contains(SearchTerm) ||
                    c.DeptName.Contains(SearchTerm) ||
                    c.CourseNum.ToString().Contains(SearchTerm));
            }

            // Apply Department dropdown filter
            if (!string.IsNullOrEmpty(SelectedDepartment))
            {
                courseQuery = courseQuery.Where(c => c.DeptName == SelectedDepartment);
            }

            // Apply Credits filter (Advanced)
            if (SelectedCredits.HasValue)
            {
                courseQuery = courseQuery.Where(c => c.CreditHours == SelectedCredits);
            }

            // Execute query
            Courses = await courseQuery.ToListAsync();

            // Populate dropdown lists
            DepartmentList = new SelectList(
                await _context.Course
                    .Select(c => c.DeptName)
                    .Distinct()
                    .ToListAsync());

            CreditList = new SelectList(
                await _context.Course
                    .Select(c => c.CreditHours)
                    .Distinct()
                    .ToListAsync());
        }

        // POST handler for Add/Drop courses
        public async Task<IActionResult> OnPostAsync()
        {
            if (CheckRegistration(Registration.StudentID, Registration.CourseID))
            {
                // Drop course
                var reg = _context.Registration
                    .First(e => e.CourseID == Registration.CourseID && e.StudentID == Registration.StudentID);
                _context.Registration.Remove(reg);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Add course
                Registration.RegistrationDateTime = DateTime.Now;
                _context.Registration.Add(Registration);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Registrations/Create");
        }

        // Check if student is already registered
        public bool CheckRegistration(string StudentId, int CourseId)
        {
            return _context.Registration.Any(e => e.CourseID == CourseId && e.StudentID == StudentId);
        }
    }
}
