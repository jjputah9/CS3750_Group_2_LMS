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

            try
            {
                var userId = GetUserId();
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // Handle photo removal
                if (RemovePhoto)
                {
                    if (existingProfile != null)
                    {
                        // Delete the physical file if it exists
                        if (!string.IsNullOrEmpty(existingProfile.ProfilePictureFileName))
                        {
                            var filePath = Path.Combine(_environment.WebRootPath, "uploads", existingProfile.ProfilePictureFileName);
                            if (System.IO.File.Exists(filePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(filePath);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not delete profile picture file: {FileName}", existingProfile.ProfilePictureFileName);
                                }
                            }
                        }

                        // Clear all photo-related fields
                        existingProfile.ProfilePictureUrl = null;
                        existingProfile.ProfilePictureFileName = null;
                        existingProfile.ProfilePictureData = null;
                        existingProfile.ProfilePictureContentType = null;
                        existingProfile.ProfilePictureUploadedAt = null;

                        _context.UserProfiles.Update(existingProfile);
                        await _context.SaveChangesAsync();
                    }

                    TempData["ImageMessage"] = "Profile photo removed!";
                }

                // Handle profile picture upload
                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    // First, delete the old file if it exists
                    if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.ProfilePictureFileName))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, "uploads", existingProfile.ProfilePictureFileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Could not delete old profile picture file: {FileName}", existingProfile.ProfilePictureFileName);
                            }
                        }
                    }

                    var uploadResult = await SaveProfilePicture(ProfilePicture, userId);
                    if (!uploadResult.success)
                    {
                        ModelState.AddModelError("ProfilePicture", uploadResult.message);
                        await LoadCurrentImage();
                        return Page();
                    }

                    if (existingProfile == null)
                    {
                        existingProfile = new UserProfile { UserId = userId };
                        _context.UserProfiles.Add(existingProfile);
                    }

                    existingProfile.ProfilePictureUrl = uploadResult.url;
                    existingProfile.ProfilePictureFileName = uploadResult.fileName;
                    existingProfile.ProfilePictureData = uploadResult.data;
                    existingProfile.ProfilePictureContentType = uploadResult.contentType;
                    existingProfile.ProfilePictureUploadedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["ImageMessage"] = "Profile picture updated!";
                }

                // Update AspNetUsers table
                if (user != null)
                {
                    user.fName = Input.FirstName;
                    user.lName = Input.LastName;
                    user.PhoneNumber = Input.Phone;
                    user.DOB = Input.BirthDate;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                // Update or create profile (excluding photo fields which are handled separately)
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
                        Link3 = Input.Link3
                    };
                    _context.UserProfiles.Add(newProfile);
                }
                else
                {
                    // Update only non-photo fields
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

                    _context.UserProfiles.Update(existingProfile);
                }

                await _context.SaveChangesAsync();

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