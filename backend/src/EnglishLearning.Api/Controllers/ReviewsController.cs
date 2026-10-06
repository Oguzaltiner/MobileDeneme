using System.Security.Claims;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/reviews")]
public sealed class ReviewsController(IReviewService reviews) : ControllerBase
{
    [HttpGet("due")]
    public async Task<ActionResult<IReadOnlyList<EnglishLearning.Application.Vocabulary.VocabularyWordDto>>> Due([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await reviews.GetDueAsync(userId, limit, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ReviewResult>> Submit(SubmitReviewRequest request, CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        try
        {
            var result = await reviews.SubmitAsync(userId, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (DailyLimitExceededException ex) { return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message }); }
        catch (InvalidReviewRequestException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
