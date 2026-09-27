using System.Security.Claims;
using EnglishLearning.Application.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/leaderboard")]
public sealed class LeaderboardController(ILeaderboardService leaderboard) : ControllerBase
{
    [HttpGet("weekly")]
    public async Task<ActionResult<LeaderboardSummary>> Weekly(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await leaderboard.GetWeeklyAsync(userId, ct));
    }
}
