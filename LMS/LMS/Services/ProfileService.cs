using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ProfileDbContext _context;

        public ProfileService(ProfileDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile> GetProfileAsync(string userId)
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task SaveProfileAsync(UserProfile profile)
        {
            var existing = await GetProfileAsync(profile.UserId);

            if (existing == null)
            {
                _context.UserProfiles.Add(profile);
            }
            else
            {
                existing.FirstName = profile.FirstName;
                existing.LastName = profile.LastName;
                existing.BirthDate = profile.BirthDate;
                existing.AddressLine1 = profile.AddressLine1;
                existing.AddressLine2 = profile.AddressLine2;
                existing.City = profile.City;
                existing.State = profile.State;
                existing.Zip = profile.Zip;
                existing.Phone = profile.Phone;
                existing.Link1 = profile.Link1;
                existing.Link2 = profile.Link2;
                existing.Link3 = profile.Link3;
                existing.LastUpdatedDate = DateTime.Now;

                _context.UserProfiles.Update(existing);
            }

            await _context.SaveChangesAsync();
        }
    }
}