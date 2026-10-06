using EnglishLearning.Application.Vocabulary;

namespace EnglishLearning.Application.Missions;

// Contract: docs/DAILY_MISSION.md. Status strings are camelCase ("notStarted", "inProgress", "completed").
public sealed record MissionPreviewDto(int EstimatedMinutes, int ReviewCount, int NewWordCount, bool HasListening);
public sealed record MissionStreakDto(int Current, int Longest, bool CompletedToday);
public sealed record MissionLimitsDto(bool IsPremium, int DailyWordsRemaining);
/// <summary><c>PendingMission</c>: an earlier-day mission that is still in progress and inside its completion window.</summary>
public sealed record MissionTodayDto(DateOnly MissionDate, string Status, DailyMissionDto? Mission, MissionPreviewDto Preview, MissionStreakDto Streak, MissionLimitsDto Limits, DailyMissionDto? PendingMission);

public sealed record MissionStepDto(string Key, int Order, string Title, int Required, int Done, bool Completed);
public sealed record MissionOptionDto(string Key, string Text);
// No word id before answering (it would reveal the answer); SpeakText is the English term only when the prompt shows it.
public sealed record MissionRecallQuestionDto(Guid Id, string Prompt, string? SpeakText, IReadOnlyList<MissionOptionDto> Options, bool Answered, bool? IsCorrect);
public sealed record MissionListeningDto(Guid Id, string Title, string Level, string Prompt, string Transcript, IReadOnlyList<MissionOptionDto> Options, bool Answered, bool? IsCorrect);
public sealed record DailyMissionDto(
    Guid Id,
    Guid PracticeSessionId,
    DateOnly MissionDate,
    string Status,
    int EstimatedMinutes,
    IReadOnlyList<MissionStepDto> Steps,
    IReadOnlyList<VocabularyWordDto> ReviewWords,
    IReadOnlyList<VocabularyWordDto> NewWords,
    IReadOnlyList<MissionRecallQuestionDto> RecallQuestions,
    MissionListeningDto? Listening,
    MissionResultDto? Result);

public sealed record StartMissionRequest(string? TimeZone = null, int? UtcOffsetMinutes = null);
public sealed record MissionAnswerRequest(string StepKey, Guid QuestionId, string OptionKey);
public sealed record MissionStepProgressDto(string Key, int Done, int Required, bool Completed);
/// <summary><c>WordId</c>/<c>Term</c>/<c>Translation</c> are null for the listening question.</summary>
public sealed record MissionAnswerResultDto(Guid QuestionId, bool IsCorrect, string CorrectOptionKey, string Explanation, Guid? WordId, string? Term, string? Translation, MissionStepProgressDto Step);

public sealed record MissionXpBreakdownDto(int Base, int Items, int Accuracy, int StreakBonus);
public sealed record MissionResultStreakDto(int Current, int Longest, bool Extended);
public sealed record MissionTomorrowDto(DateOnly Date, int DueReviewCount, int NewWordCount, int EstimatedMinutes);
public sealed record MissionResultDto(
    Guid MissionId,
    int XpAwarded,
    MissionXpBreakdownDto XpBreakdown,
    int CorrectAnswers,
    int TotalAnswers,
    int ReviewedWords,
    int NewWords,
    MissionResultStreakDto Streak,
    bool AlreadyCompleted,
    MissionTomorrowDto Tomorrow);

/// <summary>Maps to HTTP 400 (invalid time zone, unknown step/question/option).</summary>
public sealed class InvalidMissionRequestException(string message) : Exception(message);

/// <summary>Maps to HTTP 409 with the step keys that still block completion.</summary>
public sealed class MissionIncompleteException(string message, IReadOnlyList<string> incompleteSteps) : Exception(message)
{
    public IReadOnlyList<string> IncompleteSteps { get; } = incompleteSteps;
}

/// <summary>Maps to HTTP 409 with a message (e.g. answering a completed mission).</summary>
public sealed class MissionConflictException(string message) : Exception(message);

public interface IDailyMissionService
{
    Task<MissionTodayDto> GetTodayAsync(Guid userId, string? timeZone, int? utcOffsetMinutes, CancellationToken ct);
    Task<DailyMissionDto> StartTodayAsync(Guid userId, StartMissionRequest request, CancellationToken ct);
    /// <summary>Returns null when the mission does not exist or belongs to another user.</summary>
    Task<MissionAnswerResultDto?> AnswerAsync(Guid userId, Guid missionId, MissionAnswerRequest request, CancellationToken ct);
    /// <summary>Returns null when the mission does not exist or belongs to another user.</summary>
    Task<MissionResultDto?> CompleteAsync(Guid userId, Guid missionId, CancellationToken ct);
}
