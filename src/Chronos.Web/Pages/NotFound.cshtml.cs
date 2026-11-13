using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chronos.Web.Pages;

[IgnoreAntiforgeryToken]
public class NotFoundModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
    }
}
