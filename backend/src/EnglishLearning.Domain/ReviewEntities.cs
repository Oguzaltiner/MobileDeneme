namespace EnglishLearning.Domain;

public enum ReviewRating
{
    Again = 0,
    Hard = 1,
    Good = 2,
    Easy = 3
}

public sealed class UserWordProgress
{
    public Guid UserId { get; set; }
    public Guid VocabularyWordId { get; set; }
    public int Repetition { get; set; }
    public int IntervalDays { get; set; }
    public decimal EaseFactor { get; set; } = 2.5m;
    public decimal MasteryScore { get; set; }
    public int TotalReviews { get; set; }
    public int CorrectReviews { get; set; }
    public int Lapses { get; set; }
    public ReviewRating? LastRating { get; set; }
    public DateTime? LastReviewedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
    public VocabularyWord VocabularyWord { get; set; } = null!;
}

public sealed class ReviewEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid VocabularyWordId { get; set; }
    public ReviewRating Rating { get; set; }
    public string ClientEventId { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
    public VocabularyWord VocabularyWord { get; set; } = null!;
}
