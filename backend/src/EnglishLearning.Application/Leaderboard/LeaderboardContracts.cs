namespace EnglishLearning.Application.Leaderboard;

public sealed record LeaderboardEntry(int Rank, string DisplayName, int Points, bool IsCurrentUser);
public sealed record LeaderboardSummary(string League, DateTime PeriodEndsAtUtc, IReadOnlyList<LeaderboardEntry> Entries, int CurrentUserRank, int CurrentUserPoints);

public interface ILeaderboardService
{
    Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct);
}
