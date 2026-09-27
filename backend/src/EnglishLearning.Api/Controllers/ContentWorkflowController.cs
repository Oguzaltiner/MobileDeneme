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
        return Ok(await query.OrderBy(x => x.Term).Select(x => new
        {
            x.Id, x.Term, x.Translation, x.Level, x.Category, x.PartOfSpeech, x.Definition, x.ExampleSentence,
            Status = x.PublicationStatus.ToString(), x.PublishedAtUtc,
            qualityScore = (x.Pronunciation != "" ? 20 : 0) + (x.Definition != "" ? 20 : 0) + (x.ExampleSentence != null && x.ExampleSentence != "" ? 20 : 0) + (x.Translation != "" ? 20 : 0) + (x.Category != "" ? 20 : 0)
        }).ToListAsync(ct));
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

    [Authorize(Policy = "ContentReviewer"), HttpPost("vocabulary/bulk-publish")]
    public async Task<IActionResult> BulkPublish([FromBody] BulkVocabularyPublishRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ids = request.Ids.Distinct().Take(100).ToArray();
        if (ids.Length == 0) return BadRequest(new { message = "En az bir kelime seçilmelidir." });
        if (request.Ids.Count > 100) return BadRequest(new { message = "Tek seferde en fazla 100 kelime yayınlanabilir." });

        var words = await db.VocabularyWords.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (words.Count != ids.Length) return NotFound(new { message = "Seçilen kelimelerden biri veya birkaçı bulunamadı." });
        var notReady = words.Where(x => x.PublicationStatus != VocabularyPublicationStatus.InReview).Select(x => x.Term).ToArray();
        if (notReady.Length > 0) return Conflict(new { message = "Yalnızca incelemede olan kelimeler yayınlanabilir.", terms = notReady });
        var invalid = words.Where(x => !IsPublishable(x)).Select(x => x.Term).ToArray();
        if (invalid.Length > 0) return Conflict(new { message = "Kalite alanları eksik olan kelimeler yayınlanamaz.", terms = invalid });
        var published = await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, ct);
        if (published + words.Count > VocabularyLimits.MaxPublishedWords)
            return Conflict(new { message = $"Yayın limiti ({VocabularyLimits.MaxPublishedWords}) aşılacak.", remaining = Math.Max(0, VocabularyLimits.MaxPublishedWords - published) });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var word in words)
        {
            word.PublicationStatus = VocabularyPublicationStatus.Published;
            word.PublishedAtUtc = DateTime.UtcNow;
            db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = "bulk-publish", EntityType = "VocabularyWord", EntityId = word.Id });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { published = words.Count });
    }

    private async Task<IActionResult> TransitionAsync(Guid id, VocabularyPublicationStatus status, string action, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        if (word.PublicationStatus != VocabularyPublicationStatus.InReview) return Conflict(new { message = "Only content in review can be approved or rejected." });
        if (status == VocabularyPublicationStatus.Published)
        {
            if (!IsPublishable(word)) return Conflict(new { message = "Kalite alanları eksik olan kelimeler yayınlanamaz." });
            if (await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, ct) >= VocabularyLimits.MaxPublishedWords)
                return Conflict(new { message = $"Published vocabulary limit ({VocabularyLimits.MaxPublishedWords}) has been reached." });
        }
        word.PublicationStatus = status;
        word.PublishedAtUtc = status == VocabularyPublicationStatus.Published ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        await AuditAsync(userId, action, id, ct);
        return NoContent();
    }

    private static bool IsPublishable(VocabularyWord word) =>
        !string.IsNullOrWhiteSpace(word.Term) &&
        !string.IsNullOrWhiteSpace(word.Translation) &&
        !word.Translation.Equals("Çeviri inceleme bekliyor", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(word.Definition) &&
        !string.IsNullOrWhiteSpace(word.Category) &&
        !string.IsNullOrWhiteSpace(word.ExampleSentence) &&
        !word.Term.Equals(word.Translation, StringComparison.OrdinalIgnoreCase);

    private async Task AuditAsync(Guid userId, string action, Guid entityId, CancellationToken ct)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = action, EntityType = "VocabularyWord", EntityId = entityId });
        await db.SaveChangesAsync(ct);
    }
}

public sealed record BulkVocabularyPublishRequest(IReadOnlyList<Guid> Ids);
