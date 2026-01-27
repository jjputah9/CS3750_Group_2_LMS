using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LMS.models;

namespace LMS.Pages.Dashboard
{
    [Authorize]
    public class DashboardIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // constructor to inject UserManager
        public DashboardIndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // property that holds the current user
        public ApplicationUser CurrentUser { get; set; }

        // Fetch the user on GET request
        public async Task OnGet()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
        }
    }
}
