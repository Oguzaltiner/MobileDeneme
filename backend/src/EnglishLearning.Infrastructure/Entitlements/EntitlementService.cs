using EnglishLearning.Application.Entitlements;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Entitlements;

public sealed class EntitlementService(EnglishLearningDbContext db) : IEntitlementService
{
    public async Task<EntitlementDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var entitlement = await db.UserEntitlements.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var usage = await GetUsageAsync(userId, ct);
        var activePlan = entitlement?.Plan ?? SubscriptionPlan.Free;
        var premium = activePlan is SubscriptionPlan.Premium or SubscriptionPlan.PremiumPlus &&
                      (entitlement?.ExpiresAtUtc is null || entitlement.ExpiresAtUtc > DateTime.UtcNow);
        var plan = !premium ? Free() : activePlan == SubscriptionPlan.PremiumPlus ? PremiumPlus() : Premium();
        return plan with { DailyWordsUsed = usage.WordsUsed, DailyQuizzesUsed = usage.QuizzesStarted };
    }

    public async Task<bool> CanAccessLevelAsync(Guid userId, string? level, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(level)) return true;
        var entitlement = await GetAsync(userId, ct);
        return entitlement.IsPremium || level.Trim().ToUpperInvariant() is "A1" or "A2";
    }

    public async Task<bool> TryConsumeQuizAsync(Guid userId, CancellationToken ct)
    {
        var entitlement = await GetAsync(userId, ct);
        if (entitlement.IsPremium) return true;
        var usage = await GetUsageAsync(userId, ct);
        if (usage.QuizzesStarted >= entitlement.DailyQuizLimit) return false;
        usage.QuizzesStarted++;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> TryConsumeWordAsync(Guid userId, CancellationToken ct)
    {
        var entitlement = await GetAsync(userId, ct);
        if (entitlement.IsPremium) return true;
        var usage = await GetUsageAsync(userId, ct);
        if (usage.WordsUsed >= entitlement.DailyWordLimit) return false;
        usage.WordsUsed++;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> HasFeatureAsync(Guid userId, string featureKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(featureKey)) return false;
        var entitlement = await GetAsync(userId, ct);
        return entitlement.Features.Any(x => string.Equals(x, featureKey, StringComparison.OrdinalIgnoreCase))
            || featureKey.Trim().ToLowerInvariant() switch
            {
                "ai_conversation" => entitlement.CanUseAiConversation,
                "pronunciation_analysis" => entitlement.CanUsePronunciationAnalysis,
                "offline_packs" => entitlement.CanUseOfflinePacks,
                "advanced_analytics" => entitlement.CanUseAdvancedAnalytics,
                "community_feedback" => entitlement.CanUseCommunityFeedback,
                _ => false
            };
    }

    private async Task<DailyUsage> GetUsageAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usage = await db.DailyUsages.SingleOrDefaultAsync(x => x.UserId == userId && x.DateUtc == today, ct);
        if (usage is not null) return usage;
        usage = new DailyUsage { UserId = userId, DateUtc = today };
        db.DailyUsages.Add(usage);
        await db.SaveChangesAsync(ct);
        return usage;
    }

    private static EntitlementDto Free() => new(SubscriptionPlan.Free, false, "A2", 20, 1, true, 0, 0,
        ["A1-A2 vocabulary", "Daily 20 word limit", "Daily 1 quiz", "Ads"], "free", 10, false, false, false, false, false, 0, 3);

    private static EntitlementDto Premium() => new(SubscriptionPlan.Premium, true, "C2", -1, -1, false, 0, 0,
        ["A1-C2 vocabulary", "Unlimited learning", "Unlimited quizzes", "Listening and pronunciation", "Personal lists", "Ad-free"], "premium", 30, false, true, true, true, false, 3, 20);

    private static EntitlementDto PremiumPlus() => new(SubscriptionPlan.PremiumPlus, true, "C2", -1, -1, false, 0, 0,
        ["A1-C2 vocabulary", "Unlimited learning", "Unlimited quizzes", "AI conversation coach", "Pronunciation analysis", "Offline learning packs", "Advanced analytics", "Community feedback", "Personal lists", "Ad-free"], "premium_plus", 60, true, true, true, true, true, 20, 100);
}
