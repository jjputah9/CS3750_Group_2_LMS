using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LMS.Pages.Assignments
{
    [Authorize]
    public class SubmissionsModel : PageModel
    {
        public int AssignmentId { get; set; }

        // Temporary view model — NOT database-backed
        public class SubmissionViewModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public DateTime SubmittedAt { get; set; }
            public string Status { get; set; } = "";
        }

        public IList<SubmissionViewModel> Submissions { get; set; }
            = new List<SubmissionViewModel>();

        public IActionResult OnGet(int assignmentId)
        {
            AssignmentId = assignmentId;

            // Intentionally empty.
            // Real data will be added later when Submission model exists.

            return Page();
        }
    }
}