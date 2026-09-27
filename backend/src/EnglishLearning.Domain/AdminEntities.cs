namespace EnglishLearning.Domain;

public sealed class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public AppUser User { get; set; } = null!;
}
