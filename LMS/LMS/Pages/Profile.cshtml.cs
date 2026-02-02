using Microsoft.AspNetCore.Mvc.RazorPages;
using LMS.Data;
using LMS.models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMS.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Profile Properties
        public UserProfile? UserProfile { get; set; }
        public string FullName { get; set; } = "Guest User";
        public string Initials { get; set; } = "GU";
        public int ProfileViews { get; set; } = 1;
        public int Connections { get; set; } = 0;
        public bool HasProfile { get; set; } = false;
        public List<string> Skills { get; set; } = new List<string>();

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Get current user ID - use test user for now
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "test-user-001";

                // Load profile from database
                UserProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (UserProfile != null)
                {
                    FullName = $"{UserProfile.FirstName} {UserProfile.LastName}";
                    Initials = GetInitials(UserProfile.FirstName, UserProfile.LastName);
                    HasProfile = true;

                    // Demo stats (in real app, these would come from database)
                    ProfileViews = new Random().Next(100, 1000);
                    Connections = new Random().Next(10, 500);

                    // Demo skills
                    Skills = GetDemoSkills();
                }
                else
                {
                    FullName = "Guest User";
                    Initials = "GU";
                    TempData["InfoMessage"] = "Complete your profile to get started!";
                }
            }
            catch (Exception)
            {
                // Fallback to demo data
                FullName = "John Doe";
                Initials = "JD";
            }
        }

        private string GetInitials(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "??";

            return $"{firstName[0]}{lastName[0]}".ToUpper();
        }

        private List<string> GetDemoSkills()
        {
            return new List<string>
            {
                "ASP.NET Core", "C#", "SQL Server", "Entity Framework",
                "JavaScript", "HTML/CSS", "REST APIs", "Azure",
                "Git", "MVC", "Razor Pages", "Bootstrap"
            };
        }
    }
}