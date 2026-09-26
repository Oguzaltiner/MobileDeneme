using System.Security.Claims;
using EnglishLearning.Application.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/me/entitlement")]
public sealed class EntitlementsController(IEntitlementService entitlements) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EntitlementDto>> Get(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return !Guid.TryParse(rawId, out var userId) ? Unauthorized() : Ok(await entitlements.GetAsync(userId, ct));
    }
}
