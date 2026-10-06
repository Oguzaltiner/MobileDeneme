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
    IReadOnlyList<string> Features,
    string PlanKey = "free",
    int MaxDailyPracticeMinutes = 10,
    bool CanUseAiConversation = false,
    bool CanUsePronunciationAnalysis = false,
    bool CanUseOfflinePacks = false,
    bool CanUseAdvancedAnalytics = false,
    bool CanUseCommunityFeedback = false,
    int MaxOfflinePacks = 0,
    int MaxSavedLists = 3);

public sealed class DailyLimitExceededException(string message) : Exception(message);

/// <summary>Maps to HTTP 403: the requested level is outside the user's plan.</summary>
public sealed class LevelLockedException(string message) : Exception(message);

public interface IEntitlementService
{
    Task<EntitlementDto> GetAsync(Guid userId, CancellationToken ct);
    Task<bool> CanAccessLevelAsync(Guid userId, string? level, CancellationToken ct);
    Task<bool> TryConsumeQuizAsync(Guid userId, CancellationToken ct);
    Task<bool> TryConsumeWordAsync(Guid userId, CancellationToken ct);
    Task<bool> HasFeatureAsync(Guid userId, string featureKey, CancellationToken ct);
}
