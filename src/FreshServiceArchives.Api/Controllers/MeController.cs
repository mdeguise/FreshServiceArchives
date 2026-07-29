using Microsoft.AspNetCore.Mvc;

namespace FreshServiceArchives.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MeController : ControllerBase
{
    /// <summary>Returns the authenticated Windows identity, for display in the UI header.</summary>
    [HttpGet]
    public ActionResult<object> Get()
    {
        var name = User.Identity?.Name ?? "";
        // Strip the DOMAIN\ prefix for a friendlier display value.
        var display = name.Contains('\\') ? name[(name.IndexOf('\\') + 1)..] : name;
        return Ok(new
        {
            userName = name,
            displayName = display,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
        });
    }
}
