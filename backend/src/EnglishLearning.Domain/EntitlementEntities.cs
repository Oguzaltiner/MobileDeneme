namespace EnglishLearning.Domain;

public enum SubscriptionPlan
{
    Free = 1,
    Premium = 2,
    PremiumPlus = 3
}

public sealed class UserEntitlement
{
    public Guid UserId { get; set; }
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public string? Provider { get; set; }
    public string? ProductId { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
}
