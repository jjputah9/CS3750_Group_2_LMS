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
using Microsoft.AspNetCore.Identity;

namespace LMS.Pages.Courses
{
    [Authorize] // require login
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            return Page();
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<string> GetCurrentInstructorName()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return "";

            return user.lName + ", " + user.fName;
        }

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
            Course.InstructorName = GetCurrentInstructorName().Result;

            _context.Course.Add(Course);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}