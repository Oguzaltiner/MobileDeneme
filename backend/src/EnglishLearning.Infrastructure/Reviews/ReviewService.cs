using EnglishLearning.Application.Entitlements;
using EnglishLearning.Application.Reviews;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Reviews;

public sealed class ReviewService(EnglishLearningDbContext db, IEntitlementService entitlements) : IReviewService
{
    public async Task<ReviewResult?> SubmitAsync(Guid userId, SubmitReviewRequest request, CancellationToken ct)
    {
        var wordExists = await db.VocabularyWords.AnyAsync(x => x.Id == request.WordId, ct);
        if (!wordExists) return null;
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
        db.ReviewEvents.Add(new ReviewEvent { UserId = userId, VocabularyWordId = request.WordId, Rating = request.Rating });
        await db.SaveChangesAsync(ct);
        var entitlement = await entitlements.GetAsync(userId, ct);
        return new(request.WordId, request.Rating, progress.Repetition, progress.IntervalDays, progress.DueAtUtc, entitlement.DailyWordsUsed, entitlement.DailyWordLimit);
    }
}
