using EnglishLearning.Application.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/vocabulary")]
public sealed class VocabularyController(IVocabularyService vocabulary) : ControllerBase
{
    [HttpGet("levels")] public async Task<ActionResult<IReadOnlyList<VocabularyOption>>> Levels(CancellationToken ct) => Ok(await vocabulary.GetLevelsAsync(ct));
    [HttpGet("categories")] public async Task<ActionResult<IReadOnlyList<VocabularyOption>>> Categories(CancellationToken ct) => Ok(await vocabulary.GetCategoriesAsync(ct));
    [HttpGet("sentence-challenge")] public async Task<ActionResult<SentenceChallenge>> SentenceChallenge(CancellationToken ct) => (await vocabulary.GetSentenceChallengeAsync(ct)) is { } challenge ? Ok(challenge) : NotFound();
    [HttpGet("words")] public async Task<ActionResult<VocabularyPage>> Words([FromQuery] string? level, [FromQuery] string? category, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => Ok(await vocabulary.SearchAsync(new(level, category, search, page, pageSize), ct));
    [HttpGet("words/{id:guid}")] public async Task<ActionResult<VocabularyWordDto>> Word(Guid id, CancellationToken ct) => (await vocabulary.GetAsync(id, ct)) is { } word ? Ok(word) : NotFound();
}
