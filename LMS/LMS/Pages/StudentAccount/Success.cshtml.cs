using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LMS.Pages.StudentAccount
{
    [Authorize]
    public class SuccessModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public decimal Amount { get; set; }

        public void OnGet()
        {
        }
    }
}