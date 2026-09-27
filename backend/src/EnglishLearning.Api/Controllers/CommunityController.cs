using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/community")]
public sealed class CommunityController(EnglishLearningDbContext db) : ControllerBase
{
    public sealed record SubmitRequest([param: Required, StringLength(30)] string Type, [param: Required, StringLength(2000)] string Content, [param: StringLength(500)] string? Prompt);
    public sealed record FeedbackRequest([param: Required, StringLength(1000)] string Body, [param: Range(1, 5)] int? Rating);

    [HttpPost("submissions")]
    public async Task<IActionResult> Submit(SubmitRequest request, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var item = new CommunitySubmission { UserId = userId, Type = request.Type.Trim().ToLowerInvariant(), Content = request.Content.Trim(), Prompt = request.Prompt?.Trim() };
        db.CommunitySubmissions.Add(item); await db.SaveChangesAsync(ct);
        return Ok(new { id = item.Id, status = item.Status.ToString(), createdAtUtc = item.CreatedAtUtc });
    }

    [HttpGet("submissions/mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Ok(await db.CommunitySubmissions.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.Type, x.Content, x.Prompt, status = x.Status.ToString(), x.ReportCount, x.CreatedAtUtc }).ToListAsync(ct));
    }

    [HttpPost("submissions/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, FeedbackRequest request, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var exists = await db.CommunitySubmissions.AnyAsync(x => x.Id == id && x.Status == CommunitySubmissionStatus.Approved, ct);
        if (!exists) return NotFound();
        db.CommunityFeedback.Add(new CommunityFeedback { SubmissionId = id, UserId = userId, Body = request.Body.Trim(), Rating = request.Rating }); await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("submissions/{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id, CancellationToken ct)
    {
        if (!TryUser(out _)) return Unauthorized();
        var item = await db.CommunitySubmissions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.ReportCount++; if (item.ReportCount >= 3) item.Status = CommunitySubmissionStatus.Hidden; await db.SaveChangesAsync(ct); return Ok(new { status = item.Status.ToString() });
    }

    private bool TryUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
