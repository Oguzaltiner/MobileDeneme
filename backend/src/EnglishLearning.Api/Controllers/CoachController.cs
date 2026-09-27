using System.Security.Claims;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/coach")]
public sealed class CoachController(EnglishLearningDbContext db) : ControllerBase
{
    [HttpGet("plan")]
    public async Task<IActionResult> Plan([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId)) return Unauthorized();
        days = Math.Clamp(days, 3, 14);
        var since = DateTime.UtcNow.AddDays(-7);
        var events = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId && x.CreatedAtUtc >= since).Select(x => x.Rating).ToListAsync(ct);
        var success = events.Count == 0 ? 0.7 : events.Count(x => x != ReviewRating.Again) / (double)events.Count;
        var focus = success < .65 ? "review" : events.Count < 10 ? "vocabulary" : "quiz";
        var title = focus == "review" ? "Zayıf kelimelerini güçlendir" : focus == "quiz" ? "Bilgini sınayalım" : "Kelime hazineni büyüt";
        var items = Enumerable.Range(0, days).Select(i => new { date = DateTime.UtcNow.Date.AddDays(i).ToString("yyyy-MM-dd"), title, skill = focus, minutes = i == 0 ? 8 : 10, steps = focus == "review" ? new[] { "review", "quiz" } : new[] { "vocabulary", "practice" } });
        return Ok(new { generatedAtUtc = DateTime.UtcNow, horizonDays = days, successRate = Math.Round(success * 100, 1), focus, items });
    }
}
