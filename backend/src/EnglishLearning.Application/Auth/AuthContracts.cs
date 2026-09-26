using EnglishLearning.Domain;

namespace EnglishLearning.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string? DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record OnboardingRequest(string CurrentLevel, int DailyGoal, string? LearningPurpose);
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
