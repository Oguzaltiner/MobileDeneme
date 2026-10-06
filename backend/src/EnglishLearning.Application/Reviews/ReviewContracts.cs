using EnglishLearning.Domain;
using EnglishLearning.Application.Vocabulary;

namespace EnglishLearning.Application.Reviews;

public sealed record SubmitReviewRequest(Guid WordId, ReviewRating Rating, string ClientEventId, Guid? PracticeSessionId = null, string? PracticeStepKey = null, bool? IsCorrect = null);
public sealed record ReviewResult(Guid WordId, ReviewRating Rating, int Repetition, int IntervalDays, DateTime DueAtUtc, int DailyWordsUsed, int DailyWordLimit, decimal MasteryScore, int TotalReviews, int Lapses);

public interface IReviewService
{
    Task<ReviewResult?> SubmitAsync(Guid userId, SubmitReviewRequest request, CancellationToken ct);
    Task<IReadOnlyList<VocabularyWordDto>> GetDueAsync(Guid userId, int limit, CancellationToken ct);
}

/// <summary>Maps to HTTP 400, e.g. a practice step key that does not belong to the session.</summary>
public sealed class InvalidReviewRequestException(string message) : Exception(message);
