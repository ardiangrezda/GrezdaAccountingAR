using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Accounting.Pages
{
    public class HostModel : PageModel
    {
        public void OnGet()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                if (!HttpContext.Request.Path.Value?.Contains("/login", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    Response.Redirect("/login");
                    return;
                }
            }
        }
    }
}