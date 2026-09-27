namespace EnglishLearning.Application.LearningPaths;

public sealed record LearningPath(string Key, string Title, string Description, string Purpose, bool Recommended);

public interface ILearningPathService
{
    Task<IReadOnlyList<LearningPath>> GetAsync(Guid userId, CancellationToken ct);
}
