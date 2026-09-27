namespace EnglishLearning.Domain;

public enum CommunitySubmissionStatus { Pending = 1, Approved = 2, Rejected = 3, Hidden = 4 }

public sealed class CommunitySubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = "writing";
    public string Content { get; set; } = null!;
    public string? Prompt { get; set; }
    public CommunitySubmissionStatus Status { get; set; } = CommunitySubmissionStatus.Pending;
    public int ReportCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
    public List<CommunityFeedback> Feedback { get; set; } = [];
}

public sealed class CommunityFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = null!;
    public int? Rating { get; set; }
    public bool IsReported { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public CommunitySubmission Submission { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
