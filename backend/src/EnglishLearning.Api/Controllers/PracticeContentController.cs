using EnglishLearning.Application.Practice;
using EnglishLearning.Application.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/practice/content")]
public sealed class PracticeContentController(IPracticeContentService content, IEntitlementService entitlements) : ControllerBase
{
    [HttpGet("conversations")]
    public ActionResult<IReadOnlyList<ConversationScenarioDto>> Conversations([FromQuery] string? level) => Ok(content.GetConversationScenarios(level));

    [HttpGet("listening")]
    public ActionResult<IReadOnlyList<ListeningExerciseDto>> Listening([FromQuery] string? level) => Ok(content.GetListeningExercises(level));

    [HttpPost("pronunciation/assess")]
    public ActionResult<PronunciationAssessmentDto> Assess([FromBody] PronunciationAssessmentRequest request)
    {
        return Ok(content.AssessPronunciation(request));
    }

    [HttpPost("conversation/reply")]
    public async Task<ActionResult<ConversationReplyDto>> Reply([FromBody] ConversationReplyRequest request, CancellationToken ct)
    {
        if (!await HasFeature("ai_conversation", ct)) return StatusCode(StatusCodes.Status403Forbidden, new { message = "AI conversation requires Premium Plus." });
        return Ok(await content.GenerateReplyAsync(request, ct));
    }

    [HttpPost("speech/transcribe")]
    public async Task<ActionResult<TranscriptionDto>> Transcribe([FromBody] TranscriptionRequest request, CancellationToken ct)
    {
        if (!await HasFeature("pronunciation_analysis", ct)) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Speech transcription requires Premium." });
        return Ok(await content.TranscribeAsync(request, ct));
    }

    private async Task<bool> HasFeature(string feature, CancellationToken ct)
    {
        var rawId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(rawId, out var userId) && await entitlements.HasFeatureAsync(userId, feature, ct);
    }
}
