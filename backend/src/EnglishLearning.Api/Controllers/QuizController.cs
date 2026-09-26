using System.Security.Claims;
using EnglishLearning.Application.Quiz;
using EnglishLearning.Application.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/quizzes")]
public sealed class QuizController(IQuizService quiz) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<ActionResult<QuizSessionDto>> Create(CreateQuizRequest request, CancellationToken ct)
    {
        if (!UserId(out var userId)) return Unauthorized();
        QuizSessionDto? result;
        try { result = await quiz.CreateAsync(userId, request, ct); }
        catch (DailyLimitExceededException ex) { return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message }); }
        return result is null ? BadRequest(new { message = "Not enough vocabulary for this quiz." }) : Ok(result);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<QuizSessionDto>> Get(Guid sessionId, CancellationToken ct)
    {
        if (!UserId(out var userId)) return Unauthorized();
        var result = await quiz.GetAsync(userId, sessionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/questions/{questionId:guid}/answers")]
    public async Task<ActionResult<QuizAnswerResult>> Answer(Guid sessionId, Guid questionId, SubmitAnswerRequest request, CancellationToken ct)
    {
        if (!UserId(out var userId)) return Unauthorized();
        var result = await quiz.AnswerAsync(userId, sessionId, questionId, request, ct);
        return result is null ? BadRequest(new { message = "Question is invalid, already answered, or session is completed." }) : Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<ActionResult<QuizResultDto>> Complete(Guid sessionId, CancellationToken ct)
    {
        if (!UserId(out var userId)) return Unauthorized();
        var result = await quiz.CompleteAsync(userId, sessionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    private bool UserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
