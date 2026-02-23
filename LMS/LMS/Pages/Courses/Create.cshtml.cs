using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization; 

namespace LMS.Pages.Courses
{
    [Authorize] // require login
    public class CreateModel : PageModel
    {
        private readonly LMS.Data.ApplicationDbContext _context;

        public CreateModel(LMS.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public string MeetDayWarning { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            //  block anonymous POST
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Challenge();
            }

            if (!Course.MeetDays.Contains(true))
            {
                MeetDayWarning = "At least one day needs to be selected.";
                return Page();
            }
            else
            {
                MeetDayWarning = "";
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Course.InstructorEmail = User.Identity?.Name;

            _context.Course.Add(Course);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}