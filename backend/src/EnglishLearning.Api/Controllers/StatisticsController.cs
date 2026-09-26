using System.Security.Claims;
using EnglishLearning.Application.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/statistics")]
public sealed class StatisticsController(IStatisticsService statistics) : ControllerBase
{
    [HttpGet("weekly")]
    public async Task<ActionResult<WeeklyLearningStats>> Weekly(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return !Guid.TryParse(rawId, out var userId) ? Unauthorized() : Ok(await statistics.GetWeeklyAsync(userId, ct));
    }
}
