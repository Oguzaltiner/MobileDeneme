using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController]
[Route("api/v1/info")]
public sealed class ApiInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        service = "english-learning-api",
        version = "v1",
        status = "running"
    });
}
