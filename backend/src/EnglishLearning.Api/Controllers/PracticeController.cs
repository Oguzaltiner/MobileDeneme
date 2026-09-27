using System.Security.Claims;
using EnglishLearning.Application.Practice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/practice")]
public sealed class PracticeController(IPracticeService practice) : ControllerBase
{
    [HttpGet("plan")]
    public async Task<ActionResult<PracticePlan>> Plan(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await practice.GetPlanAsync(userId, ct));
    }
}
