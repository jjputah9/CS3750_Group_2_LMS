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

        [BindProperty]
        public bool RemovePhoto { get; set; }

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

            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                _context.UserProfiles.Add(profile);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            // handle photo removal
            if (RemovePhoto && !string.IsNullOrEmpty(profile.ProfilePictureFileName))
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", profile.ProfilePictureFileName);

                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed delete old image");
                    }
                }

                profile.ProfilePictureUrl = null;
                profile.ProfilePictureFileName = null;
                profile.ProfilePictureData = null;
                profile.ProfilePictureContentType = null;
                profile.ProfilePictureUploadedAt = null;
            }

            // handle photo upload
            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                // delete old first
                if (!string.IsNullOrEmpty(profile.ProfilePictureFileName))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, "uploads", profile.ProfilePictureFileName);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed delete old image");
                        }
                    }
                }

                var upload = await SaveProfilePicture(ProfilePicture, userId);

                if (!upload.success)
                {
                    ModelState.AddModelError("ProfilePicture", upload.message);
                    await LoadCurrentImage();
                    return Page();
                }

                profile.ProfilePictureUrl = upload.url;
                profile.ProfilePictureFileName = upload.fileName;
                profile.ProfilePictureData = upload.data;
                profile.ProfilePictureContentType = upload.contentType;
                profile.ProfilePictureUploadedAt = DateTime.UtcNow;
            }

            // update profile fields
            profile.FirstName = Input.FirstName;
            profile.LastName = Input.LastName;
            profile.Description = Input.Description;
            profile.BirthDate = Input.BirthDate;
            profile.AddressLine1 = Input.AddressLine1 ?? "";
            profile.AddressLine2 = Input.AddressLine2 ?? "";
            profile.City = Input.City ?? "";
            profile.State = Input.State ?? "";
            profile.ZipCode = Input.ZipCode ?? "";
            profile.Phone = Input.Phone ?? "";
            profile.Link1 = Input.Link1 ?? "";
            profile.Link2 = Input.Link2 ?? "";
            profile.Link3 = Input.Link3 ?? "";

            // update user table for relevant data
            if (user != null)
            {
                user.fName = Input.FirstName;
                user.lName = Input.LastName;
                user.PhoneNumber = Input.Phone;
                user.DOB = Input.BirthDate;
            }

            // save all changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile saved successfully!";
            return RedirectToPage("Index");
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return (false, "Only JPG, PNG, GIF files allowed.", null, null, null, null);

                if (file.Length > 5 * 1024 * 1024)
                    return (false, "File must be less than 5MB.", null, null, null, null);

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

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
                        CurrentProfileImageUrl = existingProfile.ProfilePictureUrl;
                    else if (!string.IsNullOrEmpty(existingProfile.ProfilePictureFileName))
                        CurrentProfileImageUrl = $"/uploads/{existingProfile.ProfilePictureFileName}";
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    public class ProfileInputModel
    {
        [StringLength(500)]
        public string? Description { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        [Phone]
        public string? Phone { get; set; } = string.Empty;

        [Url] public string? Link1 { get; set; }
        [Url] public string? Link2 { get; set; }
        [Url] public string? Link3 { get; set; }
    }
}