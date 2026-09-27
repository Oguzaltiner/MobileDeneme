namespace EnglishLearning.Domain;

public enum PracticeSessionStatus { InProgress = 1, Completed = 2, Abandoned = 3 }

public sealed class PracticeSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PathKey { get; set; } = "general";
    public PracticeSessionStatus Status { get; set; } = PracticeSessionStatus.InProgress;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public List<PracticeSessionStep> Steps { get; set; } = [];
}

public sealed class PracticeSessionStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public int Order { get; set; }
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int EstimatedMinutes { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public PracticeSession Session { get; set; } = null!;
}

public sealed class PracticeEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? VocabularyWordId { get; set; }
    public string StepKey { get; set; } = null!;
    public bool? IsCorrect { get; set; }
    public ReviewRating? Rating { get; set; }
    public string? ClientEventId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
    public PracticeSession Session { get; set; } = null!;
    public VocabularyWord? VocabularyWord { get; set; }
}
