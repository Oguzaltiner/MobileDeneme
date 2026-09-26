using EnglishLearning.Application.Dashboard;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Dashboard;

public sealed class DashboardService(EnglishLearningDbContext db) : IDashboardService
{
    public async Task<DashboardSummary?> GetAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (settings is null) return null;
        var today = DateTime.UtcNow.Date;
        var todayProgress = await db.ReviewEvents.AsNoTracking().CountAsync(x => x.UserId == userId && x.CreatedAtUtc >= today, ct);
        var totalWordsLearned = await db.UserWordProgress.AsNoTracking().CountAsync(x => x.UserId == userId && x.Repetition > 0, ct);
        var dueReviewCount = await db.UserWordProgress.AsNoTracking().CountAsync(x => x.UserId == userId && x.DueAtUtc <= DateTime.UtcNow, ct);
        var weekStart = today.AddDays(-6);
        var weeklyReviewProgress = await db.ReviewEvents.AsNoTracking().CountAsync(x => x.UserId == userId && x.CreatedAtUtc >= weekStart, ct);
        var reviewDays = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.CreatedAtUtc.Date).Distinct().OrderByDescending(x => x).ToListAsync(ct);
        var currentStreak = CalculateCurrentStreak(reviewDays, today);
        var longestStreak = CalculateLongestStreak(reviewDays);
        var recommended = Math.Clamp(dueReviewCount > 0 ? dueReviewCount : 5, 1, Math.Max(1, settings.DailyGoal));
        var coachTitle = dueReviewCount > 0 ? "Tekrar zamanı" : todayProgress >= settings.DailyGoal ? "Hedef tamamlandı" : "Bugünün mini görevi";
        var coachMessage = dueReviewCount > 0
            ? $"{dueReviewCount} kelimenin tekrar zamanı geldi. Önce zorlandıklarını pekiştirelim."
            : todayProgress >= settings.DailyGoal
                ? "Bugünkü hedefini tamamladın. İstersen mini quiz ile bilgini test et."
                : $"Bugün {recommended} kelimelik kısa bir seansla ritmini koruyalım.";
        var achievements = new[]
        {
            new AchievementDto("first-review", "İlk adım", "İlk kelime değerlendirmesini tamamla.", totalWordsLearned >= 1),
            new AchievementDto("streak-3", "Ritmi yakaladın", "3 gün üst üste çalış.", currentStreak >= 3 || longestStreak >= 3),
            new AchievementDto("words-25", "Kelime avcısı", "25 kelime öğren.", totalWordsLearned >= 25),
            new AchievementDto("words-100", "Ustalık yolu", "100 kelime öğren.", totalWordsLearned >= 100)
        };
        return new DashboardSummary(settings.CurrentLevel, settings.DailyGoal, todayProgress, totalWordsLearned, dueReviewCount, currentStreak, longestStreak, settings.DailyGoal * 5, weeklyReviewProgress, achievements, coachTitle, coachMessage, recommended);
    }

    private static int CalculateCurrentStreak(IReadOnlyList<DateTime> days, DateTime today)
    {
        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        foreach (var day in days)
        {
            if (day != cursor) break;
            streak++; cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyList<DateTime> days)
    {
        var longest = 0; var current = 0; DateTime? previous = null;
        foreach (var day in days.OrderBy(x => x))
        {
            current = previous is not null && day == previous.Value.AddDays(1) ? current + 1 : 1;
            longest = Math.Max(longest, current); previous = day;
        }
        return longest;
    }
}
