using Microsoft.EntityFrameworkCore;
using EnglishLearning.Domain;

namespace EnglishLearning.Infrastructure.Persistence;

public sealed class EnglishLearningDbContext(DbContextOptions<EnglishLearningDbContext> options)
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).IsRequired(); entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.HasOne(x => x.Settings).WithOne(x => x.User).HasForeignKey<UserSettings>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("user_settings"); entity.HasKey(x => x.UserId);
            entity.Property(x => x.CurrentLevel).HasMaxLength(10); entity.Property(x => x.LearningPurpose).HasMaxLength(80);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(10).IsRequired(); entity.Property(x => x.DailyGoal).HasDefaultValue(10);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens"); entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
