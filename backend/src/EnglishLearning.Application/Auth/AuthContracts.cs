using EnglishLearning.Domain;
using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Application.Auth;

public sealed record RegisterRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, StringLength(128, MinimumLength = 8)] string Password,
    [param: StringLength(80)] string? DisplayName);
public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, StringLength(128, MinimumLength = 8)] string Password);
public sealed record RefreshRequest([param: Required, StringLength(512, MinimumLength = 32)] string RefreshToken);
public sealed record OnboardingRequest(
    [param: Required, StringLength(2, MinimumLength = 2)] string CurrentLevel,
    [param: Range(1, 200)] int DailyGoal,
    [param: StringLength(32)] string? LearningPurpose);
public sealed record UserProfile(Guid Id, string Email, string? DisplayName, bool OnboardingCompleted, UserSettingsDto Settings);
public sealed record UserSettingsDto(string? CurrentLevel, int DailyGoal, string? LearningPurpose, string PreferredLanguage);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, UserProfile User);

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<UserProfile?> UpdateOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken cancellationToken);
}
public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}
public interface IAuthStore
{
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken);
    Task<AppUser> CreateUserAsync(string email, string password, string? displayName, CancellationToken cancellationToken);
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    bool VerifyPassword(AppUser user, string password);
    Task SaveRefreshTokenAsync(AppUser user, string tokenHash, CancellationToken cancellationToken);
    Task<AppUser?> ConsumeRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<AppUser?> UpdateSettingsAsync(Guid userId, OnboardingRequest request, CancellationToken cancellationToken);
}
