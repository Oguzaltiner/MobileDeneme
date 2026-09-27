namespace EnglishLearning.Domain;

public enum VocabularyPublicationStatus
{
    Draft,
    InReview,
    Published,
    Rejected
}

public sealed class VocabularyWord
{
    public Guid Id { get; set; }
    public required string Term { get; set; }
    public required string Pronunciation { get; set; }
    public required string PartOfSpeech { get; set; }
    public required string Definition { get; set; }
    public required string Translation { get; set; }
    public required string Level { get; set; }
    public required string Category { get; set; }
    public string? ExampleSentence { get; set; }
    public VocabularyPublicationStatus PublicationStatus { get; set; } = VocabularyPublicationStatus.Published;
    public DateTime? PublishedAtUtc { get; set; }
}
