namespace EnglishLearning.Application.Dashboard;

public sealed record AchievementDto(string Key, string Title, string Description, bool Unlocked);
public sealed record DashboardSummary(string? CurrentLevel, int DailyGoal, int TodayProgress, int TotalWordsLearned, int DueReviewCount, int CurrentStreak, int LongestStreak, int WeeklyReviewGoal, int WeeklyReviewProgress, IReadOnlyList<AchievementDto> Achievements, string CoachTitle, string CoachMessage, int RecommendedSessionSize, string CoachReason, string CoachActionKey, string CoachRoute, string CoachCta, string CoachSkill);
public interface IDashboardService
{
    Task<DashboardSummary?> GetAsync(Guid userId, CancellationToken ct);
}
