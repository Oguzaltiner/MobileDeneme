using EnglishLearning.Application.Leaderboard;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Leaderboard;

public sealed class LeaderboardService(EnglishLearningDbContext db) : ILeaderboardService
{
    public async Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        var reviewRows = await db.ReviewEvents.AsNoTracking().Where(x => x.CreatedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName, x.User.Email })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName ?? x.Key.Email, Points = x.Sum(e => e.Rating == Domain.ReviewRating.Again ? 2 : e.Rating == Domain.ReviewRating.Hard ? 5 : e.Rating == Domain.ReviewRating.Good ? 10 : 15), Count = x.Count() })
            .ToListAsync(ct);
        var quizRows = await db.QuizSessions.AsNoTracking().Where(x => x.Status == Domain.QuizSessionStatus.Completed && x.CompletedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName, x.User.Email })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName ?? x.Key.Email, Points = x.Sum(q => 20 + q.CorrectCount * 5), Count = x.Count() })
            .ToListAsync(ct);
        var rows = reviewRows.Concat(quizRows).GroupBy(x => new { x.UserId, x.Name }).Select(x => new { x.Key.UserId, x.Key.Name, Points = x.Sum(v => v.Points) }).OrderByDescending(x => x.Points).ThenBy(x => x.Name).Take(50).ToList();
        var entries = rows.Select((x, index) => new LeaderboardEntry(index + 1, x.Name, x.Points, x.UserId == userId)).ToList();
        var current = entries.FirstOrDefault(x => x.IsCurrentUser) ?? new LeaderboardEntry(entries.Count + 1, "Sen", 0, true);
        var userReviewRows = reviewRows.FirstOrDefault(x => x.UserId == userId);
        var userQuizRows = quizRows.FirstOrDefault(x => x.UserId == userId);
        var breakdown = new LeaderboardXpBreakdown(userReviewRows?.Points ?? 0, userQuizRows?.Points ?? 0, userReviewRows?.Count ?? 0, userQuizRows?.Count ?? 0);
        var reward = current.Rank switch
        {
            <= 3 => new LeaderboardReward("Podyum ödülü", "Hafta sonunda ilk 3'e girerek özel profil rozeti kazan.") ,
            <= 10 => new LeaderboardReward("Haftalık sandık", "İlk 10'da kalırsan haftalık ödül sandığını açarsın."),
            _ => new LeaderboardReward("İlk 10 hedefi", "Bir sonraki hedefin ilk 10'a girip haftalık sandığı kazanmak.")
        };
        return new("Mavi Lig", start.AddDays(7), entries, current.Rank, current.Points, breakdown, reward);
    }
}
