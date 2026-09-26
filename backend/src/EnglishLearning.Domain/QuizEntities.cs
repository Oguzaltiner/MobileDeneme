namespace EnglishLearning.Domain;

public enum QuizQuestionType { Translation = 1, Definition = 2 }
public enum QuizSessionStatus { InProgress = 1, Completed = 2 }

public sealed class QuizSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? Level { get; set; }
    public string? Category { get; set; }
    public int QuestionCount { get; set; }
    public int CorrectCount { get; set; }
    public QuizSessionStatus Status { get; set; } = QuizSessionStatus.InProgress;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public List<QuizQuestion> Questions { get; set; } = [];
}

public sealed class QuizQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid VocabularyWordId { get; set; }
    public int Order { get; set; }
    public QuizQuestionType Type { get; set; }
    public bool Answered { get; set; }
    public bool IsCorrect { get; set; }
    public QuizSession Session { get; set; } = null!;
    public VocabularyWord VocabularyWord { get; set; } = null!;
    public List<QuizOption> Options { get; set; } = [];
}

public sealed class QuizOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public required string Key { get; set; }
    public required string Text { get; set; }
    public bool IsCorrect { get; set; }
    public QuizQuestion Question { get; set; } = null!;
}
