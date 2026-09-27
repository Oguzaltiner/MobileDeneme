using System.Security.Claims;
using EnglishLearning.Application.LearningPaths;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/learning-paths")]
public sealed class LearningPathsController(ILearningPathService paths) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LearningPath>>> Get(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        return Ok(await paths.GetAsync(userId, ct));
    }
}
