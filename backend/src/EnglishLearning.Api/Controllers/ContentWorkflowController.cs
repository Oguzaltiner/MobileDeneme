using System.Data;
using System.Security.Claims;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishLearning.Api.Controllers;

[ApiController, Route("api/v1/admin/content")]
public sealed class ContentWorkflowController(EnglishLearningDbContext db, ILogger<ContentWorkflowController> logger) : ControllerBase
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

    [Authorize(Policy = "ContentEditor"), HttpPost("vocabulary/import"), RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Import([FromBody] VocabularyImportRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = request.Words ?? [];
        if (items.Count == 0) return BadRequest(new { message = "En az bir kelime gönderilmelidir." });
        if (items.Count > 1000) return BadRequest(new { message = "Tek seferde en fazla 1000 kelime içe aktarılabilir." });

        var rows = items.Select(x => new VocabularyWord
        {
            Term = x?.Term?.Trim() ?? "", Pronunciation = x?.Pronunciation?.Trim() ?? "", PartOfSpeech = x?.PartOfSpeech?.Trim() ?? "",
            Definition = x?.Definition?.Trim() ?? "", Translation = x?.Translation?.Trim() ?? "", Level = x?.Level?.Trim().ToUpperInvariant() ?? "",
            Category = x?.Category?.Trim() ?? "", ExampleSentence = x?.ExampleSentence?.Trim() ?? ""
        }).ToList();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalid = new List<VocabularyImportInvalidRow>();
        for (var i = 0; i < rows.Count; i++)
        {
            var problems = VocabularyQuality.FindImportProblems(rows[i]).ToList();
            if (rows[i].Term != "" && !seen.Add(rows[i].Term)) problems.Add("Aynı terim istekte birden fazla kez yer alıyor.");
            if (problems.Count > 0) invalid.Add(new(i, rows[i].Term, string.Join(" ", problems)));
        }
        if (invalid.Count > 0) return BadRequest(new { message = "Geçersiz satırlar olduğu için hiçbir kelime içe aktarılmadı.", invalid });

        try
        {
            // Serializable: a publish/edit racing with this import fails one side with 40001
            // instead of being silently overwritten; the matching rows are read inside it.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var lowered = rows.Select(x => x.Term.ToLowerInvariant()).ToArray();
            var existing = await db.VocabularyWords.Where(x => lowered.Contains(x.Term.ToLower())).ToListAsync(ct);
            var matches = rows.Select(row => existing.Where(x => x.Term.Equals(row.Term, StringComparison.OrdinalIgnoreCase)).ToList()).ToList();
            invalid = matches.Select((found, i) => (found, i)).Where(x => x.found.Count > 1)
                .Select(x => new VocabularyImportInvalidRow(x.i, rows[x.i].Term, $"Terim birden fazla kayıtla eşleşiyor: {string.Join(", ", x.found.Select(w => w.Term))}.")).ToList();
            if (invalid.Count > 0) return BadRequest(new { message = "Geçersiz satırlar olduğu için hiçbir kelime içe aktarılmadı.", invalid });

            var created = 0; var updated = 0; var skipped = new List<string>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var word = matches[i].SingleOrDefault();
                if (word is null)
                {
                    row.Id = Guid.NewGuid();
                    row.PublicationStatus = VocabularyPublicationStatus.InReview;
                    db.VocabularyWords.Add(row); created++;
                    db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = "import-create", EntityType = "VocabularyWord", EntityId = row.Id });
                    continue;
                }
                if (word.PublicationStatus == VocabularyPublicationStatus.Published) { skipped.Add(word.Term); continue; }
                word.Term = row.Term; word.Pronunciation = row.Pronunciation; word.PartOfSpeech = row.PartOfSpeech; word.Definition = row.Definition;
                word.Translation = row.Translation; word.Level = row.Level; word.Category = row.Category; word.ExampleSentence = row.ExampleSentence;
                word.PublicationStatus = VocabularyPublicationStatus.InReview; word.PublishedAtUtc = null; updated++;
                db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = "import-update", EntityType = "VocabularyWord", EntityId = word.Id });
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new VocabularyImportResult(created, updated, skipped, rows.Count));
        }
        catch (Exception ex) when (IsConcurrentWriteConflict(ex))
        {
            logger.LogWarning(ex, "Vocabulary import by {UserId} rolled back after a concurrent write conflict.", userId);
            return Conflict(new { message = "İçerik başka bir işlemle aynı anda değişti, hiçbir şey kaydedilmedi; tekrar deneyin." });
        }
    }

    private static bool IsConcurrentWriteConflict(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;

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

    private static bool IsPublishable(VocabularyWord word) => VocabularyQuality.IsPublishable(word);

    private async Task AuditAsync(Guid userId, string action, Guid entityId, CancellationToken ct)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = action, EntityType = "VocabularyWord", EntityId = entityId });
        await db.SaveChangesAsync(ct);
    }
}

public sealed record BulkVocabularyPublishRequest(IReadOnlyList<Guid> Ids);
public sealed record VocabularyImportItem(string? Term, string? Pronunciation, string? PartOfSpeech, string? Definition, string? Translation, string? Level, string? Category, string? ExampleSentence);
public sealed record VocabularyImportRequest(IReadOnlyList<VocabularyImportItem?>? Words);
public sealed record VocabularyImportInvalidRow(int Index, string Term, string Reason);
public sealed record VocabularyImportResult(int Created, int Updated, IReadOnlyList<string> SkippedPublished, int Total);
