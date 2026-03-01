using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCareHub.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetMe()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();

        return Ok(new
        {
            sub,
            preferred_username = User.FindFirstValue("preferred_username"),
            email = User.FindFirstValue("email"),
            roles
        });
    }
}
