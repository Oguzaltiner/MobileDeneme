using EnglishLearning.Domain;

namespace EnglishLearning.Application.Quiz;

public sealed record CreateQuizRequest(int QuestionCount = 5, string? Level = null, string? Category = null, int? Difficulty = null);
public sealed record QuizOptionDto(string Key, string Text);
public sealed record QuizQuestionDto(Guid Id, int Order, QuizQuestionType Type, int Difficulty, string Skill, string? Explanation, string? ErrorTag, string Prompt, IReadOnlyList<QuizOptionDto> Options, bool Answered, bool? IsCorrect);
public sealed record QuizSessionDto(Guid Id, QuizSessionStatus Status, int QuestionCount, int AnsweredCount, int CorrectCount, IReadOnlyList<QuizQuestionDto> Questions);
public sealed record SubmitAnswerRequest(string OptionKey);
public sealed record QuizAnswerResult(Guid QuestionId, bool IsCorrect, string CorrectOptionKey, int CorrectCount, int AnsweredCount);
public sealed record QuizResultDto(Guid Id, QuizSessionStatus Status, int QuestionCount, int AnsweredCount, int CorrectCount, int ScorePercent);

public interface IQuizService
{
    Task<QuizSessionDto?> CreateAsync(Guid userId, CreateQuizRequest request, CancellationToken ct);
    Task<QuizSessionDto?> GetAsync(Guid userId, Guid sessionId, CancellationToken ct);
    Task<QuizAnswerResult?> AnswerAsync(Guid userId, Guid sessionId, Guid questionId, SubmitAnswerRequest request, CancellationToken ct);
    Task<QuizResultDto?> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
