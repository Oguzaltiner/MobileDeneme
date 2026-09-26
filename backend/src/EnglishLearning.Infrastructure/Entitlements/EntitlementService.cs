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
        var premium = entitlement?.Plan == SubscriptionPlan.Premium &&
                      (entitlement.ExpiresAtUtc is null || entitlement.ExpiresAtUtc > DateTime.UtcNow);
        return premium ? Premium() : Free();
    }

    public async Task<bool> CanAccessLevelAsync(Guid userId, string? level, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(level)) return true;
        var entitlement = await GetAsync(userId, ct);
        return entitlement.IsPremium || level.Trim().ToUpperInvariant() is "A1" or "A2";
    }

    private static EntitlementDto Free() => new(SubscriptionPlan.Free, false, "A2", 20, 1, true,
        ["A1-A2 vocabulary", "Daily 20 word limit", "Daily 1 quiz", "Ads"]);

    private static EntitlementDto Premium() => new(SubscriptionPlan.Premium, true, "C2", -1, -1, false,
        ["A1-C2 vocabulary", "Unlimited learning", "Unlimited quizzes", "Listening and pronunciation", "Personal lists", "Ad-free"]);
}
