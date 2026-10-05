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
    public sealed record MediaWriteRequest(
        [param: Required, StringLength(120)] string Key,
        [param: Required, StringLength(160)] string Title,
        MediaAssetKind Kind,
        [param: Required, StringLength(1000)] string Url,
        [param: StringLength(10000)] string? Transcript,
        [param: StringLength(500)] string? AltText,
        [param: StringLength(10)] string Level = "A2",
        [param: StringLength(20)] string? DurationSeconds = null);

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

    [HttpGet("vocabulary")]
    public async Task<IActionResult> Vocabulary(
        [FromQuery] string? search = null,
        [FromQuery] string? level = null,
        [FromQuery] string? category = null,
        [FromQuery] VocabularyPublicationStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Clamp(page, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var query = db.VocabularyWords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Term.Contains(term) || x.Translation.Contains(term) || x.Definition.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(x => x.Level == level.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category.Trim());
        if (status is not null) query = query.Where(x => x.PublicationStatus == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Term)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation,
                x.Level, x.Category, x.ExampleSentence,
                status = x.PublicationStatus.ToString(), x.PublishedAtUtc
            }).ToListAsync(ct);
        return Ok(new { items, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("analytics/learning")]
    public async Task<IActionResult> LearningAnalytics([FromQuery] int days = 30, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 90);
        var since = DateTime.UtcNow.Date.AddDays(-days + 1);
        var reviews = await db.ReviewEvents.AsNoTracking().Where(x => x.CreatedAtUtc >= since)
            .Select(x => new { x.CreatedAtUtc, x.Rating }).ToListAsync(ct);
        var quizQuestions = await db.QuizQuestions.AsNoTracking().Where(x => x.Answered && x.Session.CreatedAtUtc >= since)
            .Select(x => new { x.Session.CreatedAtUtc, x.IsCorrect }).ToListAsync(ct);
        var daily = Enumerable.Range(0, days).Select(offset =>
        {
            var date = since.AddDays(offset);
            var dayReviews = reviews.Where(x => x.CreatedAtUtc.Date == date).ToList();
            var dayQuiz = quizQuestions.Where(x => x.CreatedAtUtc.Date == date).ToList();
            return new
            {
                date = date.ToString("yyyy-MM-dd"),
                reviewEvents = dayReviews.Count,
                successfulReviews = dayReviews.Count(x => x.Rating != ReviewRating.Again),
                quizAnswers = dayQuiz.Count,
                correctQuizAnswers = dayQuiz.Count(x => x.IsCorrect)
            };
        });
        var content = await db.VocabularyWords.AsNoTracking().GroupBy(x => new { x.Level, x.Category })
            .Select(g => new { level = g.Key.Level, category = g.Key.Category, total = g.Count(), published = g.Count(x => x.PublicationStatus == VocabularyPublicationStatus.Published) })
            .OrderBy(x => x.level).ThenBy(x => x.category).ToListAsync(ct);
        return Ok(new { days, since, daily, contentCoverage = content });
    }

    [HttpGet("analytics/retention")]
    public async Task<IActionResult> RetentionAnalytics([FromQuery] int days = 30, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 90);
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(-days + 1);
        var users = await db.Users.AsNoTracking().Select(x => new { x.Id, x.CreatedAtUtc }).ToListAsync(ct);
        var activity = await db.ReviewEvents.AsNoTracking().Where(x => x.CreatedAtUtc >= since).Select(x => new { x.UserId, x.CreatedAtUtc }).ToListAsync(ct);
        var cohorts = Enumerable.Range(0, days).Select(offset =>
        {
            var cohortDate = since.AddDays(offset);
            var cohortUsers = users.Where(x => x.CreatedAtUtc.Date == cohortDate).Select(x => x.Id).ToHashSet();
            var activeUsers = activity.Where(x => cohortUsers.Contains(x.UserId)).Select(x => x.UserId).Distinct().Count();
            var d7 = activity.Where(x => cohortUsers.Contains(x.UserId) && x.CreatedAtUtc.Date >= cohortDate.AddDays(7)).Select(x => x.UserId).Distinct().Count();
            return new { date = cohortDate.ToString("yyyy-MM-dd"), signups = cohortUsers.Count, activated = activeUsers, d7Retained = d7, activationRate = cohortUsers.Count == 0 ? 0 : activeUsers * 100d / cohortUsers.Count, d7RetentionRate = cohortUsers.Count == 0 ? 0 : d7 * 100d / cohortUsers.Count };
        });
        return Ok(new { days, cohorts });
    }

    [HttpGet("analytics/funnel")]
    public async Task<IActionResult> FunnelAnalytics(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().CountAsync(ct);
        var onboarded = await db.UserSettings.AsNoTracking().CountAsync(x => x.CurrentLevel != null, ct);
        var quizStarted = await db.QuizSessions.AsNoTracking().CountAsync(ct);
        var quizCompleted = await db.QuizSessions.AsNoTracking().CountAsync(x => x.Status == QuizSessionStatus.Completed, ct);
        var reviewUsers = await db.ReviewEvents.AsNoTracking().Select(x => x.UserId).Distinct().CountAsync(ct);
        return Ok(new { steps = new[] {
            new { key = "registered", count = users },
            new { key = "onboarded", count = onboarded },
            new { key = "startedQuiz", count = quizStarted },
            new { key = "completedQuiz", count = quizCompleted },
            new { key = "completedReview", count = reviewUsers }
        }});
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
        if (!VocabularyQuality.IsPublishable(word)) return Conflict(new { message = "Kalite alanları eksik olan kelimeler yayınlanamaz." });
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
        // Edits are not saved, so published content cannot be degraded below the publish quality bar.
        if (word.PublicationStatus == VocabularyPublicationStatus.Published && !VocabularyQuality.IsPublishable(word))
            return Conflict(new { message = "Yayındaki kelime kalite alanları eksik olacak şekilde güncellenemez." });
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

    [HttpGet("media")]
    public async Task<IActionResult> Media([FromQuery] MediaAssetStatus? status, CancellationToken ct = default) => Ok(await db.MediaAssets.AsNoTracking().Where(x => status == null || x.Status == status).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));

    [HttpPost("media")]
    public async Task<ActionResult<object>> CreateMedia(MediaWriteRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        if (await db.MediaAssets.AnyAsync(x => x.Key == request.Key.Trim(), ct)) return Conflict(new { message = "A media asset with this key already exists." });
        var asset = new MediaAsset { Key = request.Key.Trim(), Title = request.Title.Trim(), Kind = request.Kind, Url = request.Url.Trim(), Transcript = request.Transcript?.Trim(), AltText = request.AltText?.Trim(), Level = string.IsNullOrWhiteSpace(request.Level) ? "A2" : request.Level.Trim().ToUpperInvariant(), DurationSeconds = request.DurationSeconds?.Trim() };
        db.MediaAssets.Add(asset); await db.SaveChangesAsync(ct); await AuditAsync(adminId, "create", asset.Id, ct);
        return Created($"/api/v1/admin/media/{asset.Id}", new { asset.Id });
    }

    [HttpPost("media/{id:guid}/publish")]
    public async Task<IActionResult> PublishMedia(Guid id, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct); if (asset is null) return NotFound();
        asset.Status = MediaAssetStatus.Published; asset.PublishedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); await AuditAsync(adminId, "publish", id, ct); return NoContent();
    }
    [HttpGet("moderation/queue")]
    public async Task<IActionResult> ModerationQueue([FromQuery] CommunitySubmissionStatus? status = null, CancellationToken ct = default)
    {
        var query = db.CommunitySubmissions.AsNoTracking().Include(x => x.User).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        return Ok(await query.OrderByDescending(x => x.ReportCount).ThenBy(x => x.CreatedAtUtc).Take(200)
            .Select(x => new { x.Id, x.Type, x.Content, x.Prompt, status = x.Status.ToString(), x.ReportCount, x.CreatedAtUtc, user = x.User.DisplayName ?? x.User.Email }).ToListAsync(ct));
    }

    [HttpPost("moderation/{id:guid}/{action}")]
    public async Task<IActionResult> Moderate(Guid id, string action, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();
        var item = await db.CommunitySubmissions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        item.Status = action.ToLowerInvariant() switch { "approve" => CommunitySubmissionStatus.Approved, "reject" => CommunitySubmissionStatus.Rejected, "hide" => CommunitySubmissionStatus.Hidden, _ => (CommunitySubmissionStatus)0 };
        if (item.Status == 0) return BadRequest(new { message = "Action must be approve, reject or hide." });
        await db.SaveChangesAsync(ct); await AuditAsync(adminId, $"moderation_{action}", id, ct); return Ok(new { status = item.Status.ToString() });
    }

    public sealed record ContentWriteRequest([param: Required, StringLength(100)] string Key, [param: Required, StringLength(200)] string Title, [param: Required, StringLength(40)] string Type, [param: Required, StringLength(10)] string Level, [param: Required, StringLength(80)] string Category, [param: Required] string PayloadJson);

    [HttpGet("content")]
    public async Task<IActionResult> Content([FromQuery] ContentStudioStatus? status = null, CancellationToken ct = default)
    {
        var query = db.LearningContentItems.AsNoTracking().AsQueryable(); if (status is not null) query = query.Where(x => x.Status == status.Value);
        return Ok(await query.OrderByDescending(x => x.UpdatedAtUtc).Take(500).Select(x => new { x.Id, x.Key, x.Title, x.Type, x.Level, x.Category, x.PayloadJson, status = x.Status.ToString(), x.Version, x.CreatedAtUtc, x.UpdatedAtUtc }).ToListAsync(ct));
    }

    [HttpPost("content")]
    public async Task<IActionResult> CreateContent(ContentWriteRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized(); if (!TryValidateContentPayload(request.Type, request.PayloadJson, out var error)) return BadRequest(new { message = error });
        if (await db.LearningContentItems.AnyAsync(x => x.Key == request.Key.Trim(), ct)) return Conflict(new { message = "Content key already exists." });
        var item = new LearningContentItem { Key = request.Key.Trim(), Title = request.Title.Trim(), Type = request.Type.Trim().ToLowerInvariant(), Level = request.Level.Trim().ToUpperInvariant(), Category = request.Category.Trim(), PayloadJson = request.PayloadJson };
        db.LearningContentItems.Add(item); await db.SaveChangesAsync(ct); await AuditAsync(adminId, "content_create", item.Id, ct); return Created($"/api/v1/admin/content/{item.Id}", new { item.Id });
    }

    [HttpPut("content/{id:guid}")]
    public async Task<IActionResult> UpdateContent(Guid id, ContentWriteRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized(); if (!TryValidateContentPayload(request.Type, request.PayloadJson, out var error)) return BadRequest(new { message = error });
        var item = await db.LearningContentItems.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        item.Title = request.Title.Trim(); item.Type = request.Type.Trim().ToLowerInvariant(); item.Level = request.Level.Trim().ToUpperInvariant(); item.Category = request.Category.Trim(); item.PayloadJson = request.PayloadJson; item.Version++; item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await AuditAsync(adminId, "content_update", id, ct); return NoContent();
    }

    [HttpPost("content/{id:guid}/{action}")]
    public async Task<IActionResult> ContentAction(Guid id, string action, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized(); var item = await db.LearningContentItems.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        item.Status = action.ToLowerInvariant() switch { "submit" => ContentStudioStatus.InReview, "publish" => ContentStudioStatus.Published, "archive" => ContentStudioStatus.Archived, "draft" => ContentStudioStatus.Draft, _ => (ContentStudioStatus)0 }; if (item.Status == 0) return BadRequest();
        item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); await AuditAsync(adminId, $"content_{action}", id, ct); return Ok(new { status = item.Status.ToString(), item.Version });
    }

    private static bool TryValidateContentPayload(string type, string payload, out string? error)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            if (!string.Equals(type.Trim(), "grammar", StringComparison.OrdinalIgnoreCase)) { error = null; return true; }
            var root = document.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object || !root.TryGetProperty("summary", out _) || !root.TryGetProperty("contrastNote", out _) || !root.TryGetProperty("rules", out var rules) || !root.TryGetProperty("exercises", out var exercises)) { error = "Grammar content requires summary, contrastNote, rules and exercises."; return false; }
            if (rules.ValueKind != System.Text.Json.JsonValueKind.Array || rules.GetArrayLength() == 0 || exercises.ValueKind != System.Text.Json.JsonValueKind.Array || exercises.GetArrayLength() == 0) { error = "Grammar content must include at least one rule and one exercise."; return false; }
            foreach (var exercise in exercises.EnumerateArray())
            {
                if (!exercise.TryGetProperty("prompt", out _) || !exercise.TryGetProperty("answer", out _) || !exercise.TryGetProperty("explanation", out _) || !exercise.TryGetProperty("options", out var options) || options.ValueKind != System.Text.Json.JsonValueKind.Array || options.GetArrayLength() != 4) { error = "Each grammar exercise requires prompt, answer, explanation and exactly four options."; return false; }
                var answer = exercise.GetProperty("answer").GetString();
                if (string.IsNullOrWhiteSpace(answer) || !options.EnumerateArray().Any(x => string.Equals(x.GetString(), answer, StringComparison.OrdinalIgnoreCase))) { error = "Each grammar exercise answer must match one of its options."; return false; }
            }
            error = null; return true;
        }
        catch (System.Text.Json.JsonException) { error = "PayloadJson must be valid JSON."; return false; }
    }
}
