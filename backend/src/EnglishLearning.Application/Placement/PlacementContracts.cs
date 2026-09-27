using EnglishLearning.Domain;
using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Application.Placement;

public sealed record StartPlacementTestRequest([param: Range(8, 20)] int QuestionCount = 12);
public sealed record PlacementTestOptionDto(string Key, string Text);
public sealed record PlacementTestQuestionDto(Guid Id, int Order, int Difficulty, PlacementQuestionType Type, string Prompt, IReadOnlyList<PlacementTestOptionDto> Options, bool Answered, bool? IsCorrect);
public sealed record PlacementTestDto(Guid Id, PlacementTestStatus Status, int QuestionCount, int AnsweredCount, int CorrectCount, string? EstimatedLevel, IReadOnlyList<PlacementTestQuestionDto> Questions);
public sealed record PlacementAnswerRequest([param: Required, StringLength(1, MinimumLength = 1)] string OptionKey);
public sealed record PlacementResultDto(Guid Id, PlacementTestStatus Status, int QuestionCount, int AnsweredCount, int CorrectCount, int ScorePercent, string EstimatedLevel);

public interface IPlacementTestService
{
    Task<PlacementTestDto?> StartAsync(Guid userId, StartPlacementTestRequest request, CancellationToken ct);
    Task<PlacementTestDto?> GetAsync(Guid userId, Guid attemptId, CancellationToken ct);
    Task<PlacementAnswerResult?> AnswerAsync(Guid userId, Guid attemptId, Guid questionId, PlacementAnswerRequest request, CancellationToken ct);
    Task<PlacementResultDto?> CompleteAsync(Guid userId, Guid attemptId, CancellationToken ct);
}

public sealed record PlacementAnswerResult(Guid QuestionId, bool IsCorrect, string CorrectOptionKey, int CorrectCount, int AnsweredCount);
