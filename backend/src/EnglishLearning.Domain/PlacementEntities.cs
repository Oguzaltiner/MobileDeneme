namespace EnglishLearning.Domain;

public enum PlacementTestStatus { InProgress = 1, Completed = 2, Abandoned = 3 }
public enum PlacementQuestionType { Translation = 1, Definition = 2 }

/// <summary>
/// A persisted, resumable level assessment. It is deliberately separate from a normal
/// quiz so placement answers cannot affect daily quiz limits or review scheduling.
/// </summary>
public sealed class PlacementTestAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public PlacementTestStatus Status { get; set; } = PlacementTestStatus.InProgress;
    public int QuestionCount { get; set; }
    public int AnsweredCount { get; set; }
    public int CorrectCount { get; set; }
    public int? ScorePercent { get; set; }
    public string? EstimatedLevel { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public List<PlacementTestQuestion> Questions { get; set; } = [];
}

public sealed class PlacementTestQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid VocabularyWordId { get; set; }
    public int Order { get; set; }
    public int Difficulty { get; set; }
    public PlacementQuestionType Type { get; set; }
    public bool Answered { get; set; }
    public bool IsCorrect { get; set; }
    public PlacementTestAttempt Attempt { get; set; } = null!;
    public VocabularyWord VocabularyWord { get; set; } = null!;
    public List<PlacementTestOption> Options { get; set; } = [];
}

public sealed class PlacementTestOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public required string Key { get; set; }
    public required string Text { get; set; }
    public bool IsCorrect { get; set; }
    public PlacementTestQuestion Question { get; set; } = null!;
}
