using System.Security.Claims;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Route("api/v1/admin/content")]
public sealed class ContentWorkflowController(EnglishLearningDbContext db) : ControllerBase
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);

    [Authorize(Policy = "ContentEditor"), HttpGet("vocabulary/review-queue")]
    public async Task<IActionResult> ReviewQueue([FromQuery] VocabularyPublicationStatus? status = null, CancellationToken ct = default)
    {
        var query = db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus != VocabularyPublicationStatus.Published);
        if (status is not null) query = query.Where(x => x.PublicationStatus == status);
        return Ok(await query.OrderBy(x => x.Term).Select(x => new { x.Id, x.Term, x.Translation, x.Level, x.Category, Status = x.PublicationStatus.ToString(), x.PublishedAtUtc }).ToListAsync(ct));
    }

    [Authorize(Policy = "ContentEditor"), HttpPost("vocabulary/{id:guid}/submit-review")]
    public async Task<IActionResult> SubmitReview(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        if (word.PublicationStatus is VocabularyPublicationStatus.Published) return Conflict(new { message = "Published content cannot be submitted for review." });
        word.PublicationStatus = VocabularyPublicationStatus.InReview;
        await db.SaveChangesAsync(ct);
        await AuditAsync(userId, "submit-review", id, ct);
        return NoContent();
    }

    [Authorize(Policy = "ContentReviewer"), HttpPost("vocabulary/{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken ct) => TransitionAsync(id, VocabularyPublicationStatus.Published, "publish", ct);

    [Authorize(Policy = "ContentReviewer"), HttpPost("vocabulary/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, CancellationToken ct) => TransitionAsync(id, VocabularyPublicationStatus.Rejected, "reject", ct);

    private async Task<IActionResult> TransitionAsync(Guid id, VocabularyPublicationStatus status, string action, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        if (word.PublicationStatus != VocabularyPublicationStatus.InReview) return Conflict(new { message = "Only content in review can be approved or rejected." });
        word.PublicationStatus = status;
        word.PublishedAtUtc = status == VocabularyPublicationStatus.Published ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        await AuditAsync(userId, action, id, ct);
        return NoContent();
    }

    private async Task AuditAsync(Guid userId, string action, Guid entityId, CancellationToken ct)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = action, EntityType = "VocabularyWord", EntityId = entityId });
        await db.SaveChangesAsync(ct);
    }
}
