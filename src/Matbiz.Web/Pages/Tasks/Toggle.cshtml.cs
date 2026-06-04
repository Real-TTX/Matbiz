using Matbiz.Web.Modules.Tasks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Tasks;

/// <summary>
/// Tiny POST-only handler used as the target of the "abhaken" checkbox on the
/// task list, dashboard panels, and customer-detail task tab. Returns either
/// an HX-Refresh header (htmx will reload the page in-place) or a normal
/// redirect to the referrer for non-htmx fallback.
/// </summary>
[Authorize]
[IgnoreAntiforgeryToken] // htmx sends the token via header; manual check below
public class ToggleModel(TaskService tasks, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) : PageModel
{
    public async Task<IActionResult> OnPostAsync(Guid id, string? returnUrl = null)
    {
        // Validate the antiforgery token that htmx sent via header.
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch
        {
            return BadRequest("antiforgery");
        }

        await tasks.ToggleDoneAsync(id);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers["HX-Refresh"] = "true";
            return new EmptyResult();
        }
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
