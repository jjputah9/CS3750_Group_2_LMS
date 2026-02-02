using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LMS.Data;
using LMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LMS.Pages.Profile
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new ProfileInputModel();

        [BindProperty]
        public IFormFile? ProfilePicture { get; set; }

        public string? CurrentProfileImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadProfileData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCurrentImage();
                return Page();
            }

            try
            {
                var userId = GetUserId();
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                // Handle profile picture upload
                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    var uploadResult = await SaveProfilePicture(ProfilePicture, userId);
                    if (!uploadResult.success)
                    {
                        ModelState.AddModelError("ProfilePicture", uploadResult.message);
                        await LoadCurrentImage();
                        return Page();
                    }

                    // Update profile with picture data
                    await UpdateProfileWithPicture(existingProfile, userId, uploadResult);
                    TempData["ImageMessage"] = "Profile picture updated!";
                }

                // Update or create profile
                await SaveProfile(existingProfile, userId);

                TempData["SuccessMessage"] = "Profile saved successfully!";
                return RedirectToPage("/Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                ModelState.AddModelError("", "Error saving profile. Please try again.");
                await LoadCurrentImage();
                return Page();
            }
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo-user-123";
        }

        private async Task LoadProfileData()
        {
            var userId = GetUserId();
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile != null)
            {
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

                if (!string.IsNullOrEmpty(existingProfile.ProfilePictureUrl))
                {
                    CurrentProfileImageUrl = existingProfile.ProfilePictureUrl;
                }
                else if (!string.IsNullOrEmpty(existingProfile.ProfilePictureFileName))
                {
                    CurrentProfileImageUrl = $"/uploads/{existingProfile.ProfilePictureFileName}";
                }
            }
            else
            {
                Input.FirstName = "John";
                Input.LastName = "Doe";
                Input.Phone = "(801) 555-1234";
            }
        }

        private async Task<(bool success, string message, string url, string fileName, byte[] data, string contentType)>
            SaveProfilePicture(IFormFile file, string userId)
        {
            try
            {
                // Validate
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return (false, "Only JPG, PNG, GIF files allowed.", null, null, null, null);

                if (file.Length > 5 * 1024 * 1024)
                    return (false, "File must be less than 5MB.", null, null, null, null);

                // Create folder
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Generate filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Read data
                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                return (true, "Success", $"/uploads/{fileName}", fileName, imageData, file.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload error");
                return (false, $"Error: {ex.Message}", null, null, null, null);
            }
        }

        private async Task UpdateProfileWithPicture(UserProfile? profile, string userId,
            (bool success, string message, string url, string fileName, byte[] data, string contentType) uploadResult)
        {
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserProfiles.Add(profile);
            }

            profile.ProfilePictureUrl = uploadResult.url;
            profile.ProfilePictureFileName = uploadResult.fileName;
            profile.ProfilePictureData = uploadResult.data;
            profile.ProfilePictureContentType = uploadResult.contentType;
            profile.ProfilePictureUploadedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task SaveProfile(UserProfile? existingProfile, string userId)
        {
            if (existingProfile == null)
            {
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
        }

        private async Task LoadCurrentImage()
        {
            try
            {
                var userId = GetUserId();
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingProfile != null)
                {
                    if (!string.IsNullOrEmpty(existingProfile.ProfilePictureUrl))
                    {
                        CurrentProfileImageUrl = existingProfile.ProfilePictureUrl;
                    }
                    else if (!string.IsNullOrEmpty(existingProfile.ProfilePictureFileName))
                    {
                        CurrentProfileImageUrl = $"/uploads/{existingProfile.ProfilePictureFileName}";
                    }
                }
            }
            catch
            {
                // Ignore error
            }
        }
    }

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