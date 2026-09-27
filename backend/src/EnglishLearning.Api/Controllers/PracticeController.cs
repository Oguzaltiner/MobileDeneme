using System.Security.Claims;
using EnglishLearning.Application.Practice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/practice")]
public sealed class PracticeController(IPracticeService practice) : ControllerBase
{
    [HttpGet("plan")]
    public async Task<ActionResult<PracticePlan>> Plan(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await practice.GetPlanAsync(userId, ct));
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Start([FromBody] StartPracticeSessionRequest request, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        return Ok(await practice.StartAsync(userId, request, ct));
    }

    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var result = await practice.GetSessionAsync(userId, sessionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteStep(Guid sessionId, Guid stepId, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var result = await practice.CompleteStepAsync(userId, sessionId, stepId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid sessionId, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var result = await practice.CompleteAsync(userId, sessionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryUser(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
