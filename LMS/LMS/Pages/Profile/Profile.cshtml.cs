using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using LMS.Services;
using LMS.Models;

namespace LMS.Pages.Profile
{
    public class ProfileModel : PageModel
    {
        private readonly IProfileService _profileService;

        public ProfileModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [BindProperty]
        public string FirstName { get; set; }

        [BindProperty]
        public string LastName { get; set; }

        [BindProperty]
        public DateTime BirthDate { get; set; }

        [BindProperty]
        public string AddressLine1 { get; set; }

        [BindProperty]
        public string AddressLine2 { get; set; }

        [BindProperty]
        public string City { get; set; }

        [BindProperty]
        public string State { get; set; }

        [BindProperty]
        public string Zip { get; set; }

        [BindProperty]
        public string Phone { get; set; }

        [BindProperty]
        public string Link1 { get; set; }

        [BindProperty]
        public string Link2 { get; set; }

        [BindProperty]
        public string Link3 { get; set; }

        public string Message { get; set; }

        public async Task OnGetAsync()
        {
            string testUserId = "test-user-123";

            var profile = await _profileService.GetProfileAsync(testUserId);

            if (profile != null)
            {
                FirstName = profile.FirstName;
                LastName = profile.LastName;
                BirthDate = profile.BirthDate;
                AddressLine1 = profile.AddressLine1;
                AddressLine2 = profile.AddressLine2;
                City = profile.City;
                State = profile.State;
                Zip = profile.Zip;
                Phone = profile.Phone;
                Link1 = profile.Link1;
                Link2 = profile.Link2;
                Link3 = profile.Link3;
            }
            else
            {
                BirthDate = DateTime.Now.AddYears(-18);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Age validation
            var age = DateTime.Now.Year - BirthDate.Year;
            if (BirthDate > DateTime.Now.AddYears(-age)) age--;

            if (age < 16)
            {
                ModelState.AddModelError("BirthDate", "You must be at least 16 years old");
                return Page();
            }

            try
            {
                var profile = new UserProfile
                {
                    UserId = "test-user-123",
                    FirstName = FirstName,
                    LastName = LastName,
                    BirthDate = BirthDate,
                    AddressLine1 = AddressLine1,
                    AddressLine2 = AddressLine2,
                    City = City,
                    State = State,
                    Zip = Zip,
                    Phone = Phone,
                    Link1 = Link1,
                    Link2 = Link2,
                    Link3 = Link3,
                    LastUpdatedDate = DateTime.Now
                };

                await _profileService.SaveProfileAsync(profile);
                Message = "Profile saved successfully!";
            }
            catch
            {
                ModelState.AddModelError("", "Error saving profile");
            }

            return Page();
        }
    }
}