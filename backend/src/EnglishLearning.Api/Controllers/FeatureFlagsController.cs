using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/features")]
public sealed class FeatureFlagsController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var defaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["adaptiveCoach"] = true,
            ["conversationPractice"] = true,
            ["listeningLab"] = true,
            ["offlineSync"] = true,
            ["communityFeedback"] = configuration.GetValue("Features:CommunityFeedback", true),
            ["newQuizTypes"] = configuration.GetValue("Features:NewQuizTypes", true)
        };
        foreach (var key in defaults.Keys.ToArray())
            if (configuration.GetValue<bool?>("Features:" + key) is { } value) defaults[key] = value;
        return Ok(new { version = configuration["Features:Version"] ?? "1", flags = defaults });
    }
}
