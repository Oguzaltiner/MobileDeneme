using EnglishLearning.Domain;

namespace EnglishLearning.Application.Reviews;

public sealed record SubmitReviewRequest(Guid WordId, ReviewRating Rating, string ClientEventId);
public sealed record ReviewResult(Guid WordId, ReviewRating Rating, int Repetition, int IntervalDays, DateTime DueAtUtc, int DailyWordsUsed, int DailyWordLimit);

public interface IReviewService
{
    Task<ReviewResult?> SubmitAsync(Guid userId, SubmitReviewRequest request, CancellationToken ct);
}
