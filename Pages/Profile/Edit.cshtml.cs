using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LMS.Data;
using LMS.models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMS.Pages.Profile
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new ProfileInputModel();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get current user ID (you'll need authentication for this)
                // For now, using a demo user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo-user-123";

                // Try to load existing profile from database
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingProfile != null)
                {
                    // Populate form with existing data
                    Input.FirstName = existingProfile.FirstName;
                    Input.LastName = existingProfile.LastName;
                    Input.Description = existingProfile.Description;
                    Input.BirthDate = existingProfile.BirthDate;
                    Input.AddressLine1 = existingProfile.AddressLine1;
                    Input.AddressLine2 = existingProfile.AddressLine2;
                    Input.City = existingProfile.City;
                    Input.State = existingProfile.State;
                    Input.ZipCode = existingProfile.ZipCode;
                    Input.Phone = existingProfile.Phone;
                    Input.Link1 = existingProfile.Link1;
                    Input.Link2 = existingProfile.Link2;
                    Input.Link3 = existingProfile.Link3;
                }
                else
                {
                    // Demo data for new users
                    Input.FirstName = "John";
                    Input.LastName = "Doe";
                    Input.Description = "Passionate software developer with experience in building web applications using ASP.NET, C#, and SQL Server.";
                    Input.Phone = "(801) 555-1234";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile data");
                // Fall back to demo data
                Input.FirstName = "John";
                Input.LastName = "Doe";
                Input.Phone = "(801) 555-1234";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Get current user ID (use authentication when available)
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo-user-123";

                // Check if profile exists
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingProfile == null)
                {
                    // Create new profile
                    var newProfile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        Description = Input.Description,
                        BirthDate = Input.BirthDate,
                        AddressLine1 = Input.AddressLine1,
                        AddressLine2 = Input.AddressLine2,
                        City = Input.City,
                        State = Input.State,
                        ZipCode = Input.ZipCode,
                        Phone = Input.Phone,
                        Link1 = Input.Link1,
                        Link2 = Input.Link2,
                        Link3 = Input.Link3,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserProfiles.Add(newProfile);
                }
                else
                {
                    // Update existing profile
                    existingProfile.FirstName = Input.FirstName;
                    existingProfile.LastName = Input.LastName;
                    existingProfile.Description = Input.Description;
                    existingProfile.BirthDate = Input.BirthDate;
                    existingProfile.AddressLine1 = Input.AddressLine1;
                    existingProfile.AddressLine2 = Input.AddressLine2;
                    existingProfile.City = Input.City;
                    existingProfile.State = Input.State;
                    existingProfile.ZipCode = Input.ZipCode;
                    existingProfile.Phone = Input.Phone;
                    existingProfile.Link1 = Input.Link1;
                    existingProfile.Link2 = Input.Link2;
                    existingProfile.Link3 = Input.Link3;
                    existingProfile.UpdatedAt = DateTime.UtcNow;

                    _context.UserProfiles.Update(existingProfile);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile saved successfully to database!";
                return RedirectToPage("/Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile to database");
                ModelState.AddModelError("", "Error saving profile. Please try again.");
                return Page();
            }
        }
    }

    // Keep the same ProfileInputModel class
    public class ProfileInputModel
    {
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Birth Date")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters")]
        [Display(Name = "Address Line 1")]
        public string? AddressLine1 { get; set; }

        [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters")]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        [StringLength(10, ErrorMessage = "Zip Code cannot exceed 10 characters")]
        [Display(Name = "Zip Code")]
        public string? ZipCode { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Link cannot exceed 200 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Link 1")]
        public string? Link1 { get; set; }

        [StringLength(200, ErrorMessage = "Link cannot exceed 200 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Link 2")]
        public string? Link2 { get; set; }

        [StringLength(200, ErrorMessage = "Link cannot exceed 200 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Link 3")]
        public string? Link3 { get; set; }
    }
}