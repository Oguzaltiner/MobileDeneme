using EnglishLearning.Domain;

namespace EnglishLearning.Application.Entitlements;

public sealed record EntitlementDto(
    SubscriptionPlan Plan,
    bool IsPremium,
    string MaxLevel,
    int DailyWordLimit,
    int DailyQuizLimit,
    bool AdsEnabled,
    IReadOnlyList<string> Features);

public interface IEntitlementService
{
    Task<EntitlementDto> GetAsync(Guid userId, CancellationToken ct);
    Task<bool> CanAccessLevelAsync(Guid userId, string? level, CancellationToken ct);
}
