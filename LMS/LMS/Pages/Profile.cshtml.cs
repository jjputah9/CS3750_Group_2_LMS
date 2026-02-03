using Microsoft.AspNetCore.Mvc.RazorPages;
using LMS.Data;
using LMS.Models;
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
                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Load profile from database
                UserProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (UserProfile == null)
                {
                    // Profile does not exist (new user), create one
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user != null)
                    {
                        UserProfile = new UserProfile
                        {
                            UserId = user.Id,
                            FirstName = user.fName,
                            LastName = user.lName,
                            BirthDate = user.DOB,
                            CreatedAt = DateTime.UtcNow,
                        };
                        _context.UserProfiles.Add(UserProfile);
                        await _context.SaveChangesAsync();
                    }
                }

                if (UserProfile != null)
                {
                    FullName = UserProfile.FullName;
                    Initials = UserProfile.Initials;
                    HasProfile = true;
                } else
                {
                    FullName = "John Doe";
                    Initials = "GU";
                    TempData["InfoMessage"] = "Complete your profile to get started!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile: {ex.Message}");
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