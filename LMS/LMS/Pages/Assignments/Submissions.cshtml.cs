using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class SubmissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public SubmissionsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }
        public Assignment Assignment { get; set; }
        public List<string> SubmissionFiles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.UserType != "Instructor") return Forbid();

            Assignment = await _context.Assignment
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (Assignment == null) return NotFound();

            if (Assignment.Course.InstructorEmail != user.Email)
                return Forbid();

            var folderPath = Path.Combine(
                _env.WebRootPath,
                "submissions",
                assignmentId.ToString()
            );

            if (Directory.Exists(folderPath))
            {
                SubmissionFiles = Directory
                    .GetFiles(folderPath)
                    .Select(Path.GetFileName)
                    .ToList();
            }

            return Page();
        }
    }
}
