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
        var recommended = Math.Clamp(dueReviewCount > 0 ? dueReviewCount : 5, 1, Math.Max(1, settings.DailyGoal));
        var coachTitle = dueReviewCount > 0 ? "Tekrar zamanı" : todayProgress >= settings.DailyGoal ? "Hedef tamamlandı" : "Bugünün mini görevi";
        var coachMessage = dueReviewCount > 0
            ? $"{dueReviewCount} kelimenin tekrar zamanı geldi. Önce zorlandıklarını pekiştirelim."
            : todayProgress >= settings.DailyGoal
                ? "Bugünkü hedefini tamamladın. İstersen mini quiz ile bilgini test et."
                : $"Bugün {recommended} kelimelik kısa bir seansla ritmini koruyalım.";
        return new DashboardSummary(settings.CurrentLevel, settings.DailyGoal, todayProgress, totalWordsLearned, dueReviewCount, coachTitle, coachMessage, recommended);
    }
}
