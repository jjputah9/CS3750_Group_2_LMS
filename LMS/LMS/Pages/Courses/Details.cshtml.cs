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
    public class DetailsModel : PageModel
    {
        private readonly LMS.Data.ApplicationDbContext _context;

        public DetailsModel(LMS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}
