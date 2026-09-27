using EnglishLearning.Domain;

namespace EnglishLearning.Application.Practice;

public sealed record PracticeStep(string Key, string Title, string Description, string Route, int EstimatedMinutes);
public sealed record PracticePlan(string PathKey, string PathTitle, int EstimatedMinutes, IReadOnlyList<PracticeStep> Steps);
public sealed record PracticeSessionStepDto(Guid Id, int Order, string Key, string Title, int EstimatedMinutes, bool Completed);
public sealed record PracticeSessionDto(Guid Id, string PathKey, PracticeSessionStatus Status, DateTime StartedAtUtc, DateTime? CompletedAtUtc, IReadOnlyList<PracticeSessionStepDto> Steps);
public sealed record StartPracticeSessionRequest(string? PathKey = null);

public interface IPracticeService
{
    Task<PracticePlan> GetPlanAsync(Guid userId, CancellationToken ct);
    Task<PracticeSessionDto> StartAsync(Guid userId, StartPracticeSessionRequest request, CancellationToken ct);
    Task<PracticeSessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct);
    Task<PracticeSessionDto?> CompleteStepAsync(Guid userId, Guid sessionId, Guid stepId, CancellationToken ct);
    Task<PracticeSessionDto?> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
