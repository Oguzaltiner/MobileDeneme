namespace EnglishLearning.Application.Practice;

public sealed record PracticeStep(string Key, string Title, string Description, string Route, int EstimatedMinutes);
public sealed record PracticePlan(string PathKey, string PathTitle, int EstimatedMinutes, IReadOnlyList<PracticeStep> Steps);

public interface IPracticeService
{
    Task<PracticePlan> GetPlanAsync(Guid userId, CancellationToken ct);
}
