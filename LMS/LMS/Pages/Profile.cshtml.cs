using Microsoft.AspNetCore.Mvc.RazorPages;
using LMS.Data;
using LMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LMS.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        // Profile Properties
        public UserProfile? UserProfile { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public bool HasProfile { get; set; } = false;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileModel(
            UserManager<ApplicationUser> userManager
            , ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // property that holds the current user
        public ApplicationUser? CurrentUser { get; set; }

        public async Task OnGetAsync()
        {
            // get logged in user
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            // Load profile from database
            UserProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == @CurrentUser.Id);

            if (UserProfile == null)
            {
                // Profile does not exist (new user), create one
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == @CurrentUser.Id);

                UserProfile = new UserProfile
                {
                    UserId = user.Id,
                    FirstName = user.fName,
                    LastName = user.lName,
                    BirthDate = user.DOB
                };
                _context.UserProfiles.Add(UserProfile);
                await _context.SaveChangesAsync();
            }

            if (UserProfile != null)
            {
                FullName = UserProfile.FullName;
                Initials = UserProfile.Initials;
                HasProfile = true;
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