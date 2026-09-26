using EnglishLearning.Domain;

namespace EnglishLearning.Application.Auth;

public sealed class AuthService(IAuthStore store, ITokenService tokens) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email) || request.Password.Length < 8 || await store.ExistsAsync(email, ct)) return null;
        var user = await store.CreateUserAsync(email, request.Password, request.DisplayName?.Trim(), ct);
        return await CreateResponseAsync(user, ct);
    }
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await store.FindByEmailAsync(NormalizeEmail(request.Email), ct);
        if (user is null || !store.VerifyPassword(user, request.Password)) return null;
        return await CreateResponseAsync(user, ct);
    }
    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var user = await store.ConsumeRefreshTokenAsync(tokens.HashRefreshToken(request.RefreshToken), ct);
        return user is null ? null : await CreateResponseAsync(user, ct);
    }
    public async Task<UserProfile?> UpdateOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken ct)
    {
        if (request.DailyGoal is < 1 or > 200 || string.IsNullOrWhiteSpace(request.CurrentLevel)) return null;
        var user = await store.UpdateSettingsAsync(userId, request with { CurrentLevel = request.CurrentLevel.Trim().ToUpperInvariant() }, ct);
        return user is null ? null : ToProfile(user);
    }
    private async Task<AuthResponse> CreateResponseAsync(AppUser user, CancellationToken ct)
    {
        var access = tokens.CreateAccessToken(user); var refresh = tokens.CreateRefreshToken();
        await store.SaveRefreshTokenAsync(user, tokens.HashRefreshToken(refresh), ct);
        return new AuthResponse(access.Token, refresh, access.ExpiresAtUtc, ToProfile(user));
    }
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    public static UserProfile ToProfile(AppUser u) => new(u.Id, u.Email, u.DisplayName, !string.IsNullOrWhiteSpace(u.Settings.CurrentLevel),
        new UserSettingsDto(u.Settings.CurrentLevel, u.Settings.DailyGoal, u.Settings.LearningPurpose, u.Settings.PreferredLanguage));
}
