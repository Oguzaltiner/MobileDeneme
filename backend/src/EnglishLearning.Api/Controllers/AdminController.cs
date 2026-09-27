using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Domain;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/v1/admin")]
public sealed class AdminController(EnglishLearningDbContext db) : ControllerBase
{
    private bool TryGetAdminId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
    private async Task AuditAsync(Guid userId, string action, Guid? entityId, CancellationToken ct)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog { UserId = userId, Action = action, EntityType = "VocabularyWord", EntityId = entityId });
        await db.SaveChangesAsync(ct);
    }
    public sealed record VocabularyWriteRequest(
        [param: Required, StringLength(120)] string Term,
        [param: StringLength(120)] string? Pronunciation,
        [param: Required, StringLength(40)] string PartOfSpeech,
        [param: Required, StringLength(500)] string Definition,
        [param: Required, StringLength(160)] string Translation,
        [param: Required, StringLength(10)] string Level,
        [param: Required, StringLength(80)] string Category,
        [param: StringLength(500)] string? ExampleSentence);

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var answeredQuizCount = await db.QuizQuestions.CountAsync(x => x.Answered, ct);
        var correctQuizCount = await db.QuizQuestions.CountAsync(x => x.Answered && x.IsCorrect, ct);
        var reviewCount = await db.ReviewEvents.CountAsync(ct);
        var successfulReviewCount = await db.ReviewEvents.CountAsync(x => x.Rating != EnglishLearning.Domain.ReviewRating.Again, ct);
        return Ok(new
        {
        users = await db.Users.CountAsync(ct),
        vocabularyWords = await db.VocabularyWords.CountAsync(ct),
        publishedVocabularyWords = await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, ct),
        vocabularyCapacity = VocabularyLimits.MaxPublishedWords,
        reviewEvents = await db.ReviewEvents.CountAsync(ct),
        completedQuizzes = await db.QuizSessions.CountAsync(x => x.Status == EnglishLearning.Domain.QuizSessionStatus.Completed, ct),
        premiumUsers = await db.UserEntitlements.CountAsync(x => x.Plan != EnglishLearning.Domain.SubscriptionPlan.Free, ct),
        quizAccuracyPercent = answeredQuizCount == 0 ? 0 : correctQuizCount * 100d / answeredQuizCount,
        reviewSuccessPercent = reviewCount == 0 ? 0 : successfulReviewCount * 100d / reviewCount
        });
    }

    [HttpPost("vocabulary")]
    public async Task<ActionResult<object>> CreateVocabulary(VocabularyWriteRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var term = request.Term.Trim();
        if (await db.VocabularyWords.AnyAsync(x => x.Term == term, ct)) return Conflict(new { message = "A vocabulary word with this term already exists." });
        var word = new VocabularyWord { Term = term, Pronunciation = request.Pronunciation?.Trim() ?? string.Empty, PartOfSpeech = request.PartOfSpeech.Trim(), Definition = request.Definition.Trim(), Translation = request.Translation.Trim(), Level = request.Level.Trim().ToUpperInvariant(), Category = request.Category.Trim(), ExampleSentence = request.ExampleSentence?.Trim(), PublicationStatus = VocabularyPublicationStatus.Draft };
        db.VocabularyWords.Add(word); await db.SaveChangesAsync(ct); await AuditAsync(adminId, "create", word.Id, ct);
        return Created($"/api/v1/vocabulary/words/{word.Id}", new { word.Id });
    }

    [HttpPost("vocabulary/{id:guid}/publish")]
    public async Task<IActionResult> PublishVocabulary(Guid id, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        if (word.PublicationStatus != VocabularyPublicationStatus.Published && await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, ct) >= VocabularyLimits.MaxPublishedWords)
            return Conflict(new { message = $"Published vocabulary limit ({VocabularyLimits.MaxPublishedWords}) has been reached." });
        word.PublicationStatus = VocabularyPublicationStatus.Published;
        word.PublishedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AuditAsync(adminId, "publish", id, ct);
        return NoContent();
    }

    [HttpPut("vocabulary/{id:guid}")]
    public async Task<IActionResult> UpdateVocabulary(Guid id, VocabularyWriteRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        var term = request.Term.Trim();
        if (await db.VocabularyWords.AnyAsync(x => x.Id != id && x.Term == term, ct)) return Conflict(new { message = "A vocabulary word with this term already exists." });
        word.Term = term; word.Pronunciation = request.Pronunciation?.Trim() ?? string.Empty; word.PartOfSpeech = request.PartOfSpeech.Trim(); word.Definition = request.Definition.Trim(); word.Translation = request.Translation.Trim(); word.Level = request.Level.Trim().ToUpperInvariant(); word.Category = request.Category.Trim(); word.ExampleSentence = request.ExampleSentence?.Trim();
        await db.SaveChangesAsync(ct); await AuditAsync(adminId, "update", id, ct); return NoContent();
    }

    [HttpDelete("vocabulary/{id:guid}")]
    public async Task<IActionResult> DeleteVocabulary(Guid id, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var word = await db.VocabularyWords.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (word is null) return NotFound();
        if (await db.QuizQuestions.AnyAsync(x => x.VocabularyWordId == id, ct)) return Conflict(new { message = "This word is referenced by quiz history and cannot be deleted." });
        db.VocabularyWords.Remove(word); await db.SaveChangesAsync(ct); await AuditAsync(adminId, "delete", id, ct); return NoContent();
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int limit = 50, CancellationToken ct = default) => Ok(await db.AdminAuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(limit, 1, 200)).Select(x => new { x.Id, x.UserId, x.Action, x.EntityType, x.EntityId, x.CreatedAtUtc }).ToListAsync(ct));
}
