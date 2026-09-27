using EnglishLearning.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public const string TestEmail = "test@example.com";
    public const string TestPassword = "Test1234!";

    public static async Task SeedAsync(EnglishLearningDbContext db, IPasswordHasher<AppUser> hasher, CancellationToken ct = default)
    {
        var existing = await db.Users.SingleOrDefaultAsync(x => x.Email == TestEmail, ct);
        if (existing is not null)
        {
            if (existing.Role != "Admin") { existing.Role = "Admin"; await db.SaveChangesAsync(ct); }
            return;
        }

        var user = new AppUser
        {
            Email = TestEmail,
            DisplayName = "Test User",
            Role = "Admin",
            PasswordHash = string.Empty,
            Settings = new UserSettings { PreferredLanguage = "tr", DailyGoal = 20 }
        };
        user.PasswordHash = hasher.HashPassword(user, TestPassword);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
