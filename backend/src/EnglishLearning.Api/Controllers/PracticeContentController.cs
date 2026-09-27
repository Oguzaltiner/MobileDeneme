using EnglishLearning.Application.Practice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/practice/content")]
public sealed class PracticeContentController(IPracticeContentService content) : ControllerBase
{
    [HttpGet("conversations")]
    public ActionResult<IReadOnlyList<ConversationScenarioDto>> Conversations([FromQuery] string? level) => Ok(content.GetConversationScenarios(level));

    [HttpGet("listening")]
    public ActionResult<IReadOnlyList<ListeningExerciseDto>> Listening([FromQuery] string? level) => Ok(content.GetListeningExercises(level));

    [HttpPost("pronunciation/assess")]
    public ActionResult<PronunciationAssessmentDto> Assess([FromBody] PronunciationAssessmentRequest request) => Ok(content.AssessPronunciation(request));

    [HttpPost("conversation/reply")]
    public async Task<ActionResult<ConversationReplyDto>> Reply([FromBody] ConversationReplyRequest request, CancellationToken ct) => Ok(await content.GenerateReplyAsync(request, ct));

    [HttpPost("speech/transcribe")]
    public async Task<ActionResult<TranscriptionDto>> Transcribe([FromBody] TranscriptionRequest request, CancellationToken ct) => Ok(await content.TranscribeAsync(request, ct));
}
