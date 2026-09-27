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
}
