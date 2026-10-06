using EnglishLearning.Application.Leaderboard;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Leaderboard;

public sealed class LeaderboardService(EnglishLearningDbContext db) : ILeaderboardService
{
    public async Task<LeaderboardSummary> GetWeeklyAsync(Guid userId, CancellationToken ct)
    {
        // ISO week: Monday 00:00 UTC (Sunday belongs to the week that started six days earlier).
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var reviewRows = await db.ReviewEvents.AsNoTracking().Where(x => x.CreatedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName, Points = x.Sum(e => e.Rating == Domain.ReviewRating.Again ? 2 : e.Rating == Domain.ReviewRating.Hard ? 5 : e.Rating == Domain.ReviewRating.Good ? 10 : 15), Count = x.Count() })
            .ToListAsync(ct);
        var quizRows = await db.QuizSessions.AsNoTracking().Where(x => x.Status == Domain.QuizSessionStatus.Completed && x.CompletedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName, Points = x.Sum(q => 20 + q.CorrectCount * 5), Count = x.Count() })
            .ToListAsync(ct);
        var missionRows = await db.DailyMissions.AsNoTracking().Where(x => x.Status == Domain.DailyMissionStatus.Completed && x.CompletedAtUtc >= start)
            .GroupBy(x => new { x.UserId, x.User.DisplayName })
            .Select(x => new { x.Key.UserId, Name = x.Key.DisplayName, Points = x.Sum(m => m.XpAwarded), Count = x.Count() })
            .ToListAsync(ct);
        var rows = reviewRows.Concat(quizRows).Concat(missionRows).GroupBy(x => x.UserId).Select(x => new { UserId = x.Key, Name = PublicName(x.Key, x.Select(v => v.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))), Points = x.Sum(v => v.Points) }).OrderByDescending(x => x.Points).ThenBy(x => x.Name).Take(50).ToList();
        var entries = rows.Select((x, index) => new LeaderboardEntry(index + 1, x.Name, x.Points, x.UserId == userId)).ToList();
        var current = entries.FirstOrDefault(x => x.IsCurrentUser) ?? new LeaderboardEntry(entries.Count + 1, "Sen", 0, true);
        var userReviewRows = reviewRows.FirstOrDefault(x => x.UserId == userId);
        var userQuizRows = quizRows.FirstOrDefault(x => x.UserId == userId);
        var userMissionRows = missionRows.FirstOrDefault(x => x.UserId == userId);
        var breakdown = new LeaderboardXpBreakdown(userReviewRows?.Points ?? 0, userQuizRows?.Points ?? 0, userReviewRows?.Count ?? 0, userQuizRows?.Count ?? 0, userMissionRows?.Points ?? 0, userMissionRows?.Count ?? 0);
        var reward = current.Rank switch
        {
            <= 3 => new LeaderboardReward("Podyum ödülü", "Hafta sonunda ilk 3'e girerek özel profil rozeti kazan.") ,
            <= 10 => new LeaderboardReward("Haftalık sandık", "İlk 10'da kalırsan haftalık ödül sandığını açarsın."),
            _ => new LeaderboardReward("İlk 10 hedefi", "Bir sonraki hedefin ilk 10'a girip haftalık sandığı kazanmak.")
        };
        var periodEnds = start.AddDays(7);
        var personalBest = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId).GroupBy(x => x.CreatedAtUtc.Date).Select(x => x.Count()).OrderByDescending(x => x).FirstOrDefaultAsync(ct);
        var seasonKey = $"{start:yyyy-MM-dd}";
        var promotionCutoff = 10;
        var demotionCutoff = Math.Max(10, entries.Count - 3);
        var rewardTier = current.Rank <= 3 ? "podium" : current.Rank <= promotionCutoff ? "chest" : "progress";
        return new("Mavi Lig", periodEnds, entries, current.Rank, current.Points, breakdown, reward, personalBest, periodEnds - DateTime.UtcNow <= TimeSpan.FromHours(24), seasonKey, promotionCutoff, demotionCutoff, rewardTier);
    }

    /// <summary>Display name, or a masked id-based label; e-mail addresses are never shown on the board.</summary>
    public static string PublicName(Guid userId, string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? $"Öğrenci #{userId.ToString("N")[..4]}" : displayName.Trim();
}
