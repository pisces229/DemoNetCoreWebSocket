using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPage.Pages
{
    public class IndexModel(ILogger<IndexModel> logger) : PageModel
    {
        private readonly ILogger<IndexModel> _logger = logger;
        public void OnGet()
        {
            _logger.LogInformation("RazorPage.Pages.IndexModel");
        }
    }
}
