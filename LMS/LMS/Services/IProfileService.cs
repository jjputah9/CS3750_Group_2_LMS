using System.Threading.Tasks;
using LMS.Models;

namespace LMS.Services
{
    public interface IProfileService
    {
        Task<UserProfile> GetProfileAsync(string userId);
        Task SaveProfileAsync(UserProfile profile);
    }
}