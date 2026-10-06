using System.Security.Claims;
using EnglishLearning.Application.Missions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

/// <summary>Daily mission endpoints; contract in docs/DAILY_MISSION.md.</summary>
[ApiController, Authorize, Route("api/v1/missions")]
public sealed class MissionsController(IDailyMissionService missions) : ControllerBase
{
    [HttpGet("today")]
    public async Task<IActionResult> Today([FromQuery] string? timeZone, [FromQuery] int? utcOffsetMinutes, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        try { return Ok(await missions.GetTodayAsync(userId, timeZone, utcOffsetMinutes, ct)); }
        catch (InvalidMissionRequestException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("today")]
    public async Task<IActionResult> Start([FromBody] StartMissionRequest? request, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        try { return Ok(await missions.StartTodayAsync(userId, request ?? new StartMissionRequest(), ct)); }
        catch (InvalidMissionRequestException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/answers")]
    public async Task<IActionResult> Answer(Guid id, MissionAnswerRequest request, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        try
        {
            var result = await missions.AnswerAsync(userId, id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidMissionRequestException ex) { return BadRequest(new { message = ex.Message }); }
        catch (MissionConflictException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        try
        {
            var result = await missions.CompleteAsync(userId, id, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (MissionIncompleteException ex) { return Conflict(new { message = ex.Message, incompleteSteps = ex.IncompleteSteps }); }
    }

    private bool TryUser(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
