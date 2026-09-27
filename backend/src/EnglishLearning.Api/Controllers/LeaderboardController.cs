using System.Security.Claims;
using EnglishLearning.Application.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/leaderboard")]
public sealed class LeaderboardController(ILeaderboardService leaderboard, EnglishLearningDbContext db) : ControllerBase
{
    [HttpGet("weekly")]
    public async Task<ActionResult<LeaderboardSummary>> Weekly(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await leaderboard.GetWeeklyAsync(userId, ct));
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> Rewards(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)) return Unauthorized();
        return Ok(await db.LeagueRewardRecords.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Take(30).Select(x => new { x.Id, x.SeasonKey, x.Rank, x.Points, x.RewardTier, status = x.Status.ToString(), x.CreatedAtUtc, x.ClaimedAtUtc }).ToListAsync(ct));
    }

    [HttpPost("rewards/{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)) return Unauthorized();
        var reward = await db.LeagueRewardRecords.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct); if (reward is null) return NotFound();
        reward.Status = LeagueRewardStatus.Claimed; reward.ClaimedAtUtc ??= DateTime.UtcNow; await db.SaveChangesAsync(ct); return Ok(new { status = reward.Status.ToString(), reward.ClaimedAtUtc });
    }
}
