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
        private readonly LMS.Data.ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(UserManager<ApplicationUser> userManager, LMS.Data.ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnGet()
        {
            Courses = await _context.Course.ToListAsync();
            CurrentUser = await _userManager.GetUserAsync(User);
        }

        //Needed tables: Registration, Courses, Current User

        [BindProperty]
        public Registration Registration { get; set; } = default!;

        public IList<Course> Courses { get; set; } = default!;

        public ApplicationUser? CurrentUser { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (CheckRegistration(Registration.StudentID, Registration.CourseID))
            {
                _context.Registration.Remove(_context.Registration.First(e => e.CourseID == Registration.CourseID && e.StudentID == Registration.StudentID));
                await _context.SaveChangesAsync();
            }
            else
            {
                Registration.RegistrationDateTime = DateTime.Now;
                _context.Registration.Add(Registration);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Registrations/Create");
        }

        // Returns true if the given user is registered in the given course, false otherwise
        public bool CheckRegistration(string StudentId, int CourseId)
        {
            return _context.Registration.Any(e => e.CourseID == CourseId && e.StudentID == StudentId);
        }
    }
}
