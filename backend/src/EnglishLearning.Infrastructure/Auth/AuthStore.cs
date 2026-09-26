using EnglishLearning.Application.Auth;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Auth;

public sealed class AuthStore(EnglishLearningDbContext db, IPasswordHasher<AppUser> hasher) : IAuthStore
{
    public Task<bool> ExistsAsync(string email, CancellationToken ct) => db.Users.AnyAsync(x => x.Email == email, ct);
    public async Task<AppUser> CreateUserAsync(string email, string password, string? displayName, CancellationToken ct)
    {
        var user = new AppUser { Email = email, DisplayName = displayName, PasswordHash = "" };
        user.PasswordHash = hasher.HashPassword(user, password); db.Users.Add(user); await db.SaveChangesAsync(ct); return user;
    }
    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct) => db.Users.Include(x => x.Settings).SingleOrDefaultAsync(x => x.Email == email, ct);
    public bool VerifyPassword(AppUser user, string password) => hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
    public async Task SaveRefreshTokenAsync(AppUser user, string tokenHash, CancellationToken ct)
    {
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokenHash, ExpiresAtUtc = DateTime.UtcNow.AddDays(30) }); await db.SaveChangesAsync(ct);
    }
    public async Task<AppUser?> ConsumeRefreshTokenAsync(string tokenHash, CancellationToken ct)
    {
        var token = await db.RefreshTokens.Include(x => x.User).ThenInclude(x => x.Settings)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        if (token is null) return null; token.RevokedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return token.User;
    }
    public async Task<AppUser?> UpdateSettingsAsync(Guid userId, OnboardingRequest request, CancellationToken ct)
    {
        var user = await db.Users.Include(x => x.Settings).SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return null; user.Settings.CurrentLevel = request.CurrentLevel; user.Settings.DailyGoal = request.DailyGoal;
        user.Settings.LearningPurpose = request.LearningPurpose?.Trim(); await db.SaveChangesAsync(ct); return user;
    }
}
