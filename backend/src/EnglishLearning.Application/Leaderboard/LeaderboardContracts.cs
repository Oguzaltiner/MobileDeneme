namespace EnglishLearning.Application.Leaderboard;

public sealed record LeaderboardEntry(int Rank, string DisplayName, int Points, bool IsCurrentUser);
public sealed record LeaderboardXpBreakdown(int ReviewXp, int QuizXp, int ReviewCount, int QuizCount);
public sealed record LeaderboardReward(string Title, string Description);
public sealed record LeaderboardSummary(string League, DateTime PeriodEndsAtUtc, IReadOnlyList<LeaderboardEntry> Entries, int CurrentUserRank, int CurrentUserPoints, LeaderboardXpBreakdown XpBreakdown, LeaderboardReward Reward);

public interface ILeaderboardService
{
    Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct);
}
