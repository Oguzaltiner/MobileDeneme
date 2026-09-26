namespace EnglishLearning.Application.Statistics;

public sealed record DailyLearningStat(DateOnly Date, int Reviews, int SuccessfulReviews);
public sealed record WeeklyLearningStats(int TotalReviews, int SuccessfulReviews, int SuccessPercent, int LearnedWords, IReadOnlyList<DailyLearningStat> Days);

public interface IStatisticsService
{
    Task<WeeklyLearningStats> GetWeeklyAsync(Guid userId, CancellationToken ct);
}
