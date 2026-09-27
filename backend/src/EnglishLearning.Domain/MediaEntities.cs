namespace EnglishLearning.Domain;

public enum MediaAssetKind { Audio = 1, Video = 2, Image = 3 }
public enum MediaAssetStatus { Draft = 1, Published = 2, Archived = 3 }

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Key { get; set; }
    public required string Title { get; set; }
    public required MediaAssetKind Kind { get; set; }
    public required string Url { get; set; }
    public string? Transcript { get; set; }
    public string? AltText { get; set; }
    public string Level { get; set; } = "A2";
    public string? DurationSeconds { get; set; }
    public MediaAssetStatus Status { get; set; } = MediaAssetStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
}
