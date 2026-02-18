using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Pages.Courses
{
    public class DeleteModel : PageModel
    {
        private readonly LMS.Data.ApplicationDbContext _context;

        public DeleteModel(LMS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            // 2️⃣ Only the instructor who owns the course can delete it
            if (course.InstructorEmail != User.Identity?.Name)
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
