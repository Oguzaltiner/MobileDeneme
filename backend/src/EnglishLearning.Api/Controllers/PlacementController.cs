using System.Security.Claims;
using EnglishLearning.Application.Placement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/placement")]
public sealed class PlacementController(IPlacementTestService placement) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<ActionResult<PlacementTestDto>> Start(StartPlacementTestRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await placement.StartAsync(userId, request, ct);
        return result is null ? BadRequest(new { message = "Placement test requires at least four published vocabulary words." }) : Ok(result);
    }

    [HttpGet("sessions/{attemptId:guid}")]
    public async Task<ActionResult<PlacementTestDto>> Get(Guid attemptId, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await placement.GetAsync(userId, attemptId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("sessions/{attemptId:guid}/questions/{questionId:guid}/answers")]
    public async Task<ActionResult<PlacementAnswerResult>> Answer(Guid attemptId, Guid questionId, PlacementAnswerRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await placement.AnswerAsync(userId, attemptId, questionId, request, ct);
        return result is null ? BadRequest(new { message = "Question is invalid, already answered, or session is completed." }) : Ok(result);
    }

    [HttpPost("sessions/{attemptId:guid}/complete")]
    public async Task<ActionResult<PlacementResultDto>> Complete(Guid attemptId, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await placement.CompleteAsync(userId, attemptId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
