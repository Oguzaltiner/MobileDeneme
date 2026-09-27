using EnglishLearning.Application.Leaderboard;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Leaderboard;

public sealed class LeaderboardService(EnglishLearningDbContext db) : ILeaderboardService
{
    public async Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        var rows = await db.ReviewEvents.AsNoTracking().Where(x => x.CreatedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName, x.User.Email })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName ?? x.Key.Email, Points = x.Sum(e => e.Rating == Domain.ReviewRating.Again ? 2 : e.Rating == Domain.ReviewRating.Hard ? 5 : e.Rating == Domain.ReviewRating.Good ? 10 : 15) })
            .OrderByDescending(x => x.Points).ThenBy(x => x.Name).Take(50).ToListAsync(ct);
        var entries = rows.Select((x, index) => new LeaderboardEntry(index + 1, x.Name, x.Points, x.UserId == userId)).ToList();
        var current = entries.FirstOrDefault(x => x.IsCurrentUser) ?? new LeaderboardEntry(entries.Count + 1, "Sen", 0, true);
        return new("Mavi Lig", start.AddDays(7), entries, current.Rank, current.Points);
    }
}
