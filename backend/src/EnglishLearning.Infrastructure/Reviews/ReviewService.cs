using EnglishLearning.Application.Entitlements;
using EnglishLearning.Application.Reviews;
using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Reviews;

public sealed class ReviewService(EnglishLearningDbContext db, IEntitlementService entitlements) : IReviewService
{
    private const string MissionEventPrefix = "mission:";

    public async Task<IReadOnlyList<VocabularyWordDto>> GetDueAsync(Guid userId, int limit, CancellationToken ct)
    {
        var size = Math.Clamp(limit, 1, 50);
        var now = DateTime.UtcNow;
        var due = await db.UserWordProgress.AsNoTracking()
            .Where(x => x.UserId == userId && x.DueAtUtc <= now && x.VocabularyWord.PublicationStatus == VocabularyPublicationStatus.Published)
            .OrderBy(x => x.DueAtUtc)
            .Take(size)
            .Select(x => new VocabularyWordDto(x.VocabularyWord.Id, x.VocabularyWord.Term, x.VocabularyWord.Pronunciation, x.VocabularyWord.PartOfSpeech, x.VocabularyWord.Definition, x.VocabularyWord.Translation, x.VocabularyWord.Level, x.VocabularyWord.Category, x.VocabularyWord.ExampleSentence))
            .ToListAsync(ct);
        if (due.Count >= size) return due;
        var existingIds = await db.UserWordProgress.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.VocabularyWordId).ToListAsync(ct);
        var remaining = size - due.Count;
        var fresh = await db.VocabularyWords.AsNoTracking()
            .Where(x => !existingIds.Contains(x.Id) && x.PublicationStatus == VocabularyPublicationStatus.Published)
            .OrderBy(x => x.Level).ThenBy(x => x.Term)
            .Take(remaining)
            .Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence))
            .ToListAsync(ct);
        return due.Concat(fresh).ToList();
    }

    public async Task<ReviewResult?> SubmitAsync(Guid userId, SubmitReviewRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientEventId) || request.ClientEventId.Length > 100)
            return null;
        var wordExists = await db.VocabularyWords.AnyAsync(x => x.Id == request.WordId && x.PublicationStatus == Domain.VocabularyPublicationStatus.Published, ct);
        if (!wordExists) return null;

        if (await ReplayAsync(userId, request.ClientEventId, ct) is { } replay) return replay.Result;
        // "mission:" ids are reserved for server-graded mission answers (see DailyMissionService).
        if (request.ClientEventId.StartsWith(MissionEventPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidReviewRequestException("This client event id is reserved.");
        // Validate the session step before consuming quota: the key must be one of the session's
        // steps; daily mission sessions only accept their word steps.
        string? practiceStepKey = null;
        if (request.PracticeSessionId is not null)
        {
            var session = await db.PracticeSessions.AsNoTracking()
                .Where(x => x.Id == request.PracticeSessionId && x.UserId == userId)
                .Select(x => new { x.PathKey, StepKeys = x.Steps.Select(s => s.Key).ToList() })
                .SingleOrDefaultAsync(ct);
            if (session is not null)
            {
                // Step keys are compared and stored in lower case.
                practiceStepKey = string.IsNullOrWhiteSpace(request.PracticeStepKey) ? "review" : request.PracticeStepKey.Trim().ToLowerInvariant();
                var stepAllowed = practiceStepKey.Length <= 40
                    && (request.PracticeStepKey is null || session.StepKeys.Any(x => string.Equals(x, practiceStepKey, StringComparison.OrdinalIgnoreCase)))
                    && (session.PathKey != DailyMission.PracticePathKey || practiceStepKey is "review" or "new-words");
                if (!stepAllowed) throw new InvalidReviewRequestException("The practice step does not belong to this session.");
            }
        }
        // Quota and review rows commit together: a duplicate that loses the unique-index race rolls
        // back its quota increment and returns the stored result instead of a 500.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await entitlements.TryConsumeWordAsync(userId, ct))
            throw new DailyLimitExceededException("Daily word limit reached. Upgrade to Premium for unlimited learning.");

        var progress = await WordProgressStore.LockOrCreateAsync(db, userId, request.WordId, ct);
        var previousEase = progress.EaseFactor;
        var interval = request.Rating switch
        {
            ReviewRating.Again => 1,
            ReviewRating.Hard => Math.Max(1, (int)Math.Round(Math.Max(1, progress.IntervalDays) * 1.2)),
            ReviewRating.Good => Math.Max(1, progress.IntervalDays == 0 ? 1 : (int)Math.Round(progress.IntervalDays * (double)previousEase)),
            ReviewRating.Easy => Math.Max(2, progress.IntervalDays == 0 ? 2 : (int)Math.Round(progress.IntervalDays * (double)(previousEase + 0.3m))),
            _ => 1
        };
        progress.Repetition = request.Rating == ReviewRating.Again ? 0 : progress.Repetition + 1;
        progress.IntervalDays = interval;
        progress.EaseFactor = Math.Clamp(previousEase + request.Rating switch { ReviewRating.Again => -0.2m, ReviewRating.Hard => -0.05m, ReviewRating.Easy => 0.15m, _ => 0m }, 1.3m, 3.2m);
        progress.TotalReviews++;
        if (request.Rating != ReviewRating.Again) progress.CorrectReviews++;
        if (request.Rating == ReviewRating.Again) progress.Lapses++;
        progress.LastRating = request.Rating;
        var reviewSignal = request.Rating switch { ReviewRating.Again => 0m, ReviewRating.Hard => 0.45m, ReviewRating.Good => 0.75m, ReviewRating.Easy => 1m, _ => 0m };
        progress.MasteryScore = Math.Clamp(progress.MasteryScore * 0.8m + reviewSignal * 20m, 0m, 100m);
        progress.LastReviewedAtUtc = DateTime.UtcNow;
        progress.DueAtUtc = progress.LastReviewedAtUtc.Value.AddDays(interval);
        db.ReviewEvents.Add(new ReviewEvent { UserId = userId, VocabularyWordId = request.WordId, Rating = request.Rating, ClientEventId = request.ClientEventId });
        if (request.PracticeSessionId is not null && practiceStepKey is not null)
            db.PracticeEvents.Add(new PracticeEvent { UserId = userId, SessionId = request.PracticeSessionId.Value, VocabularyWordId = request.WordId, StepKey = practiceStepKey, Rating = request.Rating, IsCorrect = request.IsCorrect ?? request.Rating != ReviewRating.Again, ClientEventId = request.ClientEventId });
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return (await ReplayAsync(userId, request.ClientEventId, ct))?.Result
                   ?? throw new InvalidOperationException("Duplicate review could not be resolved.", ex);
        }
        var entitlement = await entitlements.GetAsync(userId, ct);
        return new(request.WordId, request.Rating, progress.Repetition, progress.IntervalDays, progress.DueAtUtc, entitlement.DailyWordsUsed, entitlement.DailyWordLimit, progress.MasteryScore, progress.TotalReviews, progress.Lapses);
    }

    /// <summary>Idempotent replay: the stored result for an already processed client event id.</summary>
    private async Task<Replay?> ReplayAsync(Guid userId, string clientEventId, CancellationToken ct)
    {
        var existingEvent = await db.ReviewEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ClientEventId == clientEventId, ct);
        if (existingEvent is null) return null;
        var existingProgress = await db.UserWordProgress.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.VocabularyWordId == existingEvent.VocabularyWordId, ct);
        if (existingProgress is null) return new Replay(null);
        var existingEntitlement = await entitlements.GetAsync(userId, ct);
        return new Replay(new(existingEvent.VocabularyWordId, existingEvent.Rating, existingProgress.Repetition, existingProgress.IntervalDays, existingProgress.DueAtUtc, existingEntitlement.DailyWordsUsed, existingEntitlement.DailyWordLimit, existingProgress.MasteryScore, existingProgress.TotalReviews, existingProgress.Lapses));
    }

    private sealed record Replay(ReviewResult? Result);
}
