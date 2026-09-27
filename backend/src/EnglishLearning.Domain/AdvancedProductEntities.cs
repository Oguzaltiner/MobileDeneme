namespace EnglishLearning.Domain;

public enum LeagueRewardStatus { Pending = 1, Claimed = 2 }

public sealed class LeagueRewardRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string SeasonKey { get; set; } = null!;
    public int Rank { get; set; }
    public int Points { get; set; }
    public string RewardTier { get; set; } = null!;
    public LeagueRewardStatus Status { get; set; } = LeagueRewardStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClaimedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
}

public enum ContentStudioStatus { Draft = 1, InReview = 2, Published = 3, Archived = 4 }

public sealed class LearningContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Type { get; set; } = "lesson";
    public string Level { get; set; } = "A1";
    public string Category { get; set; } = "general";
    public string PayloadJson { get; set; } = "{}";
    public ContentStudioStatus Status { get; set; } = ContentStudioStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
