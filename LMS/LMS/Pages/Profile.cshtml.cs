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
        public UserProfile? UserProfile { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public bool HasProfile { get; set; } = false;

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
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
                return;

            UserProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == CurrentUser.Id);

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
