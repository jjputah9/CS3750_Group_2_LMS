using Microsoft.AspNetCore.Mvc.RazorPages;
using LMS.Data;
using LMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Pages.Profile
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        public UserProfile? UserProfile { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser? CurrentUser { get; set; }

        public async Task OnGetAsync()
        {
            // need to redo entire structure here
            // needs to check for existing profile, if not create one based on the user's name and email
            // leave all other fields empty so they can be edited by the user
            // currently throws an error because the user profile is not found
            // , need to handle this case and create a default profile for the user
            // also, need to see what is going on with the creation of FullName and Initials
            // they might be being 'created' every time the page is loaded.

            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            UserProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id);

            if (UserProfile != null)
            {
                FullName = UserProfile.FullName;
                Initials = UserProfile.Initials;
            }
            else
            {
                // user profile not found, create a default one based on the user's name
                var firstName = CurrentUser.fName ?? "Unknown";
                var lastName = CurrentUser.lName ?? "User";
                FullName = $"{firstName} {lastName}";
                Initials = GetInitials(firstName, lastName);

            }

        }

        private string GetInitials(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "??";

            return $"{firstName[0]}{lastName[0]}".ToUpper();
        }
    }
}
