namespace EnglishLearning.Application.Leaderboard;

public sealed record LeaderboardEntry(int Rank, string DisplayName, int Points, bool IsCurrentUser);
public sealed record LeaderboardXpBreakdown(int ReviewXp, int QuizXp, int ReviewCount, int QuizCount, int MissionXp = 0, int MissionCount = 0);
public sealed record LeaderboardReward(string Title, string Description);
public sealed record LeaderboardSummary(string League, DateTime PeriodEndsAtUtc, IReadOnlyList<LeaderboardEntry> Entries, int CurrentUserRank, int CurrentUserPoints, LeaderboardXpBreakdown XpBreakdown, LeaderboardReward Reward, int PersonalBestPoints, bool IsClosingSoon, string SeasonKey, int PromotionCutoff, int DemotionCutoff, string RewardTier);

public interface ILeaderboardService
{
    Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct);
}
