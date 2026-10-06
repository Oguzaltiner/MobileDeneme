namespace EnglishLearning.Domain;

public enum DailyMissionStatus { InProgress = 1, Completed = 2 }

/// <summary>
/// One personal mission per user and mission day. Steps live on the linked
/// <see cref="PracticeSession"/>; answers are <see cref="PracticeEvent"/> rows.
/// </summary>
public sealed class DailyMission
{
    /// <summary>Practice path of mission sessions; not selectable through the practice API.</summary>
    public const string PracticePathKey = "daily-mission";

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PracticeSessionId { get; set; }
    public DateOnly MissionDate { get; set; }
    /// <summary>IANA id, a fixed offset such as "UTC+03:00", or "UTC".</summary>
    public string TimeZone { get; set; } = "UTC";
    public DailyMissionStatus Status { get; set; } = DailyMissionStatus.InProgress;
    /// <summary>Server-only mission content including correct option keys; written once at creation.</summary>
    public string PayloadJson { get; set; } = "{}";
    public int XpAwarded { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
    public int ReviewedWords { get; set; }
    public int NewWordsLearned { get; set; }
    /// <summary>Set once a Free user's word quota ran out during the mission; word steps then stay completed.</summary>
    public bool WordStepsCapped { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
    public PracticeSession PracticeSession { get; set; } = null!;
}
