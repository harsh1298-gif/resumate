using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ResumeProject.Pages
{
    public class IndexModel : PageModel
    {
        // These properties are available to your Index.cshtml
        public string CurrentYear { get; } = DateTime.Now.Year.ToString();

        public void OnGet()
        {
            // This empty method is all you need since your page
            // only uses static content and CurrentYear
        }
    }
}