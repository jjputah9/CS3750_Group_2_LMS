using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Identity;

namespace LMS.Pages.Courses
{
    public class DeleteModel : PageModel
    {
        private readonly LMS.Data.ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            var course = await _context.Course.FirstOrDefaultAsync(m => m.Id == id);

            if (course is not null)
            {
                Course = course;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Course.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // 1️⃣ Block anonymous users
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Forbid(); // blocks users not logged in
            }

            var user = await _userManager.GetUserAsync(User);

            // 2️⃣ Only the instructor who owns the course can delete it
            if (course.InstructorEmail != user?.Email)
            {
                return Forbid(); // blocks other instructors
            }

            // 3️⃣ Delete the course
            _context.Course.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

    }
}
