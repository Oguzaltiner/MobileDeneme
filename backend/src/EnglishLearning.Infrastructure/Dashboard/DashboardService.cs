using EnglishLearning.Application.Dashboard;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Infrastructure.Missions;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Dashboard;

public sealed class DashboardService(EnglishLearningDbContext db, IEntitlementService entitlements) : IDashboardService
{
    public async Task<DashboardSummary?> GetAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (settings is null) return null;
        var today = DateTime.UtcNow.Date;
        var todayProgress = await db.ReviewEvents.AsNoTracking().CountAsync(x => x.UserId == userId && x.CreatedAtUtc >= today, ct);
        var totalWordsLearned = await db.UserWordProgress.AsNoTracking().CountAsync(x => x.UserId == userId && x.Repetition > 0, ct);
        // Same filters as the daily mission: published words within the plan's level range.
        var entitlement = await entitlements.GetAsync(userId, ct);
        var maxRank = Array.IndexOf(Levels, entitlement.MaxLevel);
        var allowedLevels = Levels.Take(Math.Max(0, maxRank) + 1).ToList();
        var now = DateTime.UtcNow;
        var dueReviewCount = await db.UserWordProgress.AsNoTracking().CountAsync(x => x.UserId == userId && x.DueAtUtc <= now
            && x.VocabularyWord.PublicationStatus == Domain.VocabularyPublicationStatus.Published && allowedLevels.Contains(x.VocabularyWord.Level), ct);
        var missionDone = await IsTodaysMissionCompletedAsync(userId, settings.TimeZone, ct);
        var weekStart = today.AddDays(-6);
        var weeklyReviewProgress = await db.ReviewEvents.AsNoTracking().CountAsync(x => x.UserId == userId && x.CreatedAtUtc >= weekStart, ct);
        var recentReviews = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId && x.CreatedAtUtc >= today.AddDays(-7)).Select(x => x.Rating).ToListAsync(ct);
        var recentSuccessRate = recentReviews.Count == 0 ? 1d : recentReviews.Count(x => x != Domain.ReviewRating.Again) / (double)recentReviews.Count;
        var reviewDays = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.CreatedAtUtc.Date).Distinct().OrderByDescending(x => x).ToListAsync(ct);
        var currentStreak = CalculateCurrentStreak(reviewDays, today);
        var longestStreak = CalculateLongestStreak(reviewDays);
        var recommended = Math.Clamp(dueReviewCount > 0 ? dueReviewCount : 5, 1, Math.Max(1, settings.DailyGoal));
        var coachTitle = dueReviewCount > 0 ? "Tekrar zamanı" : recentReviews.Count >= 5 && recentSuccessRate < .65 ? "Zayıf alanını güçlendir" : todayProgress >= settings.DailyGoal ? "Hedef tamamlandı" : "Bugünün mini görevi";
        var coachMessage = dueReviewCount > 0
            ? $"{dueReviewCount} kelimenin tekrar zamanı geldi. Önce zorlandıklarını pekiştirelim."
            : recentReviews.Count >= 5 && recentSuccessRate < .65
                ? "Son haftada bazı kelimelerde zorlandın. Cümle ve yazma pratiğiyle bu alanı güçlendirelim."
                : todayProgress >= settings.DailyGoal
                ? "Bugünkü hedefini tamamladın. İstersen mini quiz ile bilgini test et."
                : $"Bugün {recommended} kelimelik kısa bir seansla ritmini koruyalım.";
        var coachReason = dueReviewCount > 0 ? $"{dueReviewCount} zamanlanmış tekrar" : recentReviews.Count >= 5 && recentSuccessRate < .65 ? $"Son 7 gün başarı: %{Math.Round(recentSuccessRate * 100)}" : "Son 7 günlük ritmine göre";
        var coachActionKey = dueReviewCount > 0 ? "due-review" : recentReviews.Count >= 5 && recentSuccessRate < .65 ? "skill-recovery" : todayProgress >= settings.DailyGoal ? "knowledge-check" : "daily-practice";
        // Due reviews open the daily mission until it is done, then the SRS review screen ("Learn").
        var coachRoute = dueReviewCount > 0 ? (missionDone ? "Learn" : "DailyMission") : recentReviews.Count >= 5 && recentSuccessRate < .65 ? "WritingChallenge" : todayProgress >= settings.DailyGoal ? "QuizStart" : "PracticeSession";
        var coachCta = dueReviewCount > 0 ? "Tekrarı başlat" : recentReviews.Count >= 5 && recentSuccessRate < .65 ? "Zayıf alanı çalış" : todayProgress >= settings.DailyGoal ? "Mini quiz başlat" : "Pratiğe başla";
        var coachSkill = dueReviewCount > 0 ? "review" : recentReviews.Count >= 5 && recentSuccessRate < .65 ? "active-recall" : todayProgress >= settings.DailyGoal ? "assessment" : "vocabulary";
        // Today's daily mission (in the user's pinned time zone) takes priority after due reviews.
        if (dueReviewCount == 0 && !missionDone)
        {
            coachTitle = "Bugünün görevi";
            coachMessage = "Tekrar, yeni kelimeler, hatırlama ve dinleme: birkaç dakikalık kişisel görevin hazır.";
            coachReason = "Bugünkü görev henüz tamamlanmadı";
            coachActionKey = "daily-mission";
            coachRoute = "DailyMission";
            coachCta = "Göreve başla";
            coachSkill = "mixed";
        }
        var achievements = new[]
        {
            new AchievementDto("first-review", "İlk adım", "İlk kelime değerlendirmesini tamamla.", totalWordsLearned >= 1),
            new AchievementDto("streak-3", "Ritmi yakaladın", "3 gün üst üste çalış.", currentStreak >= 3 || longestStreak >= 3),
            new AchievementDto("words-25", "Kelime avcısı", "25 kelime öğren.", totalWordsLearned >= 25),
            new AchievementDto("words-100", "Ustalık yolu", "100 kelime öğren.", totalWordsLearned >= 100)
        };
        return new DashboardSummary(settings.CurrentLevel, settings.DailyGoal, todayProgress, totalWordsLearned, dueReviewCount, currentStreak, longestStreak, settings.DailyGoal * 5, weeklyReviewProgress, achievements, coachTitle, coachMessage, recommended, coachReason, coachActionKey, coachRoute, coachCta, coachSkill);
    }

    private static readonly string[] Levels = ["A1", "A2", "B1", "B2", "C1", "C2"];

    private async Task<bool> IsTodaysMissionCompletedAsync(Guid userId, string? pinnedTimeZone, CancellationToken ct)
    {
        var last = await db.DailyMissions.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.MissionDate).Select(x => new { x.MissionDate, x.Status }).FirstOrDefaultAsync(ct);
        if (last is null) return false;
        var today = MissionDayResolver.Resolve(MissionDayResolver.FromStoredKey(pinnedTimeZone), DateTime.UtcNow, last.MissionDate);
        return last.MissionDate == today && last.Status == Domain.DailyMissionStatus.Completed;
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
