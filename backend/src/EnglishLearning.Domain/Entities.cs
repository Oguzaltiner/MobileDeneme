namespace EnglishLearning.Domain;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserSettings Settings { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class UserSettings
{
    public Guid UserId { get; set; }
    public string? CurrentLevel { get; set; }
    public int DailyGoal { get; set; } = 10;
    public string? LearningPurpose { get; set; }
    public string PreferredLanguage { get; set; } = "tr";
    public AppUser User { get; set; } = null!;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public AppUser User { get; set; } = null!;
}
