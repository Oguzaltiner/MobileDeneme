using EnglishLearning.Application.Entitlements;
using EnglishLearning.Application.Reviews;
using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Reviews;

public sealed class ReviewService(EnglishLearningDbContext db, IEntitlementService entitlements) : IReviewService
{
    public async Task<IReadOnlyList<VocabularyWordDto>> GetDueAsync(Guid userId, int limit, CancellationToken ct)
    {
        var size = Math.Clamp(limit, 1, 50);
        var now = DateTime.UtcNow;
        var due = await db.UserWordProgress.AsNoTracking()
            .Where(x => x.UserId == userId && x.DueAtUtc <= now)
            .OrderBy(x => x.DueAtUtc)
            .Take(size)
            .Select(x => new VocabularyWordDto(x.VocabularyWord.Id, x.VocabularyWord.Term, x.VocabularyWord.Pronunciation, x.VocabularyWord.PartOfSpeech, x.VocabularyWord.Definition, x.VocabularyWord.Translation, x.VocabularyWord.Level, x.VocabularyWord.Category, x.VocabularyWord.ExampleSentence))
            .ToListAsync(ct);
        if (due.Count >= size) return due;
        var existingIds = await db.UserWordProgress.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.VocabularyWordId).ToListAsync(ct);
        var remaining = size - due.Count;
        var fresh = await db.VocabularyWords.AsNoTracking()
            .Where(x => !existingIds.Contains(x.Id))
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
        var wordExists = await db.VocabularyWords.AnyAsync(x => x.Id == request.WordId, ct);
        if (!wordExists) return null;

        var existingEvent = await db.ReviewEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ClientEventId == request.ClientEventId, ct);
        if (existingEvent is not null)
        {
            var existingProgress = await db.UserWordProgress.AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == userId && x.VocabularyWordId == existingEvent.VocabularyWordId, ct);
            if (existingProgress is null) return null;
            var existingEntitlement = await entitlements.GetAsync(userId, ct);
            return new(existingEvent.VocabularyWordId, existingEvent.Rating, existingProgress.Repetition, existingProgress.IntervalDays, existingProgress.DueAtUtc, existingEntitlement.DailyWordsUsed, existingEntitlement.DailyWordLimit);
        }
        if (!await entitlements.TryConsumeWordAsync(userId, ct))
            throw new DailyLimitExceededException("Daily word limit reached. Upgrade to Premium for unlimited learning.");

        var progress = await db.UserWordProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.VocabularyWordId == request.WordId, ct);
        if (progress is null)
        {
            progress = new UserWordProgress { UserId = userId, VocabularyWordId = request.WordId };
            db.UserWordProgress.Add(progress);
        }
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
        progress.LastReviewedAtUtc = DateTime.UtcNow;
        progress.DueAtUtc = progress.LastReviewedAtUtc.Value.AddDays(interval);
        db.ReviewEvents.Add(new ReviewEvent { UserId = userId, VocabularyWordId = request.WordId, Rating = request.Rating, ClientEventId = request.ClientEventId });
        await db.SaveChangesAsync(ct);
        var entitlement = await entitlements.GetAsync(userId, ct);
        return new(request.WordId, request.Rating, progress.Repetition, progress.IntervalDays, progress.DueAtUtc, entitlement.DailyWordsUsed, entitlement.DailyWordLimit);
    }
}
