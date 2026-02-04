using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace computervision.aspcore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }
        [BindProperty]
        public FileUpload fileUpload { get; set; }
        public void OnGet()
        {
            ViewData["SuccessMessage"] = "";
        }
        public async Task<IActionResult> OnPostUpload(FileUpload fileUpload)
        {
            if (fileUpload.FormFile == null || fileUpload.FormFile.Length == 0)
            {
                ModelState.AddModelError("fileUpload.FormFile", "Please select a file to upload.");
                return Page();
            }
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "temp");

            //Creating upload folder
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            var formFile = fileUpload.FormFile;
            var filePath = Path.Combine(uploadsFolder, formFile.FileName);

            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.
            ViewData["SuccessMessage"] = formFile.FileName.ToString() + " files uploaded!!";
            return Page();
        }
    }
    public class FileUpload
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        [Display(Name = "File")]
        public IFormFile FormFile { get; set; }
        public string SuccessMessage { get; set; }
    }

}