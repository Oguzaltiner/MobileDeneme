namespace EnglishLearning.Domain;

public sealed class DailyUsage
{
    public Guid UserId { get; set; }
    public DateOnly DateUtc { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public int WordsUsed { get; set; }
    public int QuizzesStarted { get; set; }
    public AppUser User { get; set; } = null!;
}
