namespace EnglishLearning.Application.Dashboard;

public sealed record DashboardSummary(string? CurrentLevel, int DailyGoal, int TodayProgress, int TotalWordsLearned, int DueReviewCount);
public interface IDashboardService
{
    Task<DashboardSummary?> GetAsync(Guid userId, CancellationToken ct);
}
