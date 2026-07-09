using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ordevo.Web.Pages;

public sealed class ErrorModel : PageModel
{
    public string RequestId { get; private set; } = "";

    public void OnGet() => RequestId = HttpContext.TraceIdentifier;
}
