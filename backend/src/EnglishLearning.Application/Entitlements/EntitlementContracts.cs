using EnglishLearning.Domain;

namespace EnglishLearning.Application.Entitlements;

public sealed record EntitlementDto(
    SubscriptionPlan Plan,
    bool IsPremium,
    string MaxLevel,
    int DailyWordLimit,
    int DailyQuizLimit,
    bool AdsEnabled,
    int DailyWordsUsed,
    int DailyQuizzesUsed,
    IReadOnlyList<string> Features);

public sealed class DailyLimitExceededException(string message) : Exception(message);

public interface IEntitlementService
{
    Task<EntitlementDto> GetAsync(Guid userId, CancellationToken ct);
    Task<bool> CanAccessLevelAsync(Guid userId, string? level, CancellationToken ct);
    Task<bool> TryConsumeQuizAsync(Guid userId, CancellationToken ct);
}
