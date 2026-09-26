using System.Security.Claims;
using EnglishLearning.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);
        return result is null ? Conflict(new { message = "Email is already registered or the password is invalid." }) : Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await auth.RefreshAsync(request, ct);
        return result is null ? Unauthorized(new { message = "Refresh token is invalid or expired." }) : Ok(result);
    }

    [Authorize]
    [HttpPut("onboarding")]
    public async Task<ActionResult<UserProfile>> Onboarding(OnboardingRequest request, CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        var profile = await auth.UpdateOnboardingAsync(userId, request, ct);
        return profile is null ? BadRequest(new { message = "Invalid onboarding data." }) : Ok(profile);
    }
}
