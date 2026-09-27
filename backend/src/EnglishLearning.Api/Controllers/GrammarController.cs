using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnglishLearning.Infrastructure.Persistence;
using System.Text.Json;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/grammar")]
public sealed class GrammarController(EnglishLearningDbContext db) : ControllerBase
{
    private static readonly IReadOnlyList<GrammarLessonDto> Lessons =
    [
        new("present-simple-vs-continuous", "Present Simple ve Present Continuous", "A1", "Geniş zaman ile şu anda devam eden eylemi ayır.",
            "Türkçede bağlam çoğu zaman yeterlidir; İngilizcede yardımcı fiil ve fiil biçimi anlamı netleştirir.",
            [new("Kural", "Present Simple alışkanlıklar ve genel gerçekler için kullanılır: I work every day.", "Geniş zaman / alışkanlık"), new("Kural", "Present Continuous şu anda devam eden eylemler için kullanılır: I am working now.", "Şimdiki zaman"), new("Dikkat", "He/She/It ile Present Simple fiiline -s gelir.", "Üçüncü tekil kişi")],
            [new("She ___ coffee every morning.", ["drink", "drinks", "is drinking", "drinking"], "drinks", "Every morning bir alışkanlık belirttiği için Present Simple kullanılır."), new("Look! They ___ football.", ["play", "plays", "are playing", "playing"], "are playing", "Look! şu anda devam eden bir eylem belirtir.")]),
        new("articles-a-an-the", "A, An ve The", "A2", "İngilizce artikelleri Türkçe ile karşılaştırmalı öğren.",
            "Türkçede a/an/the için birebir bir ek yoktur. İngilizcede isimden önce belirlilik ve ses kuralı önemlidir.",
            [new("A / An", "Belirsiz, tekil ve ilk kez bahsedilen isimlerde kullanılır: a book, an apple.", "Belirsiz artikel"), new("The", "Konuşan ve dinleyen tarafından bilinen belirli isimlerde kullanılır: the book on the table.", "Belirli artikel"), new("Türkçe karşılık", "Türkçede anlam çoğu zaman bağlam veya bir kelimesiyle verilir.", "Karşılaştırma")],
            [new("I saw ___ elephant at the zoo.", ["a", "an", "the", "—"], "an", "Elephant sesli harfle başladığı için an kullanılır."), new("Please close ___ door.", ["a", "an", "the", "—"], "the", "Hangi kapıdan bahsedildiği biliniyor.")]),
        new("english-question-order", "İngilizcede Soru Cümlesi", "A2", "Do/does yardımcı fiiliyle doğru soru kur.",
            "Türkçede soru eki cümlenin sonunda gelir. İngilizcede yardımcı fiil öznenin önüne geçer: Do you work?",
            [new("Sıra", "Do/Does + özne + fiilin yalın hâli kullanılır.", "Soru yapısı"), new("Olumsuz", "Do not / does not + fiilin yalın hâli kullanılır.", "Olumsuz yapı"), new("Hata", "Does he works? yanlıştır. Does he work? doğrudur.", "-s kuralı")],
            [new("___ you speak English?", ["Do", "Does", "Are", "Is"], "Do", "You öznesiyle Do kullanılır."), new("Where ___ she live?", ["do", "does", "is", "are"], "does", "She öznesiyle Does kullanılır.")])
    ];

    [HttpGet("lessons")]
    public async Task<ActionResult<IReadOnlyList<GrammarLessonDto>>> GetLessons([FromQuery] string? level = null, CancellationToken ct = default)
    {
        var query = db.LearningContentItems.AsNoTracking().Where(x => x.Status == EnglishLearning.Domain.ContentStudioStatus.Published && x.Type == "grammar");
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(x => x.Level == level.Trim().ToUpperInvariant());
        var published = await query.OrderBy(x => x.Level).ThenBy(x => x.Title).ToListAsync(ct);
        var dynamicLessons = published.Select(MapPublished).Where(x => x is not null).Cast<GrammarLessonDto>();
        var staticLessons = string.IsNullOrWhiteSpace(level) ? Lessons : Lessons.Where(x => x.Level.Equals(level.Trim(), StringComparison.OrdinalIgnoreCase));
        var result = staticLessons.Concat(dynamicLessons).GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        return Ok(result);
    }

    [HttpGet("lessons/{key}")]
    public async Task<ActionResult<GrammarLessonDto>> GetLesson(string key, CancellationToken ct = default)
    {
        var item = await db.LearningContentItems.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key && x.Type == "grammar" && x.Status == EnglishLearning.Domain.ContentStudioStatus.Published, ct);
        if (item is not null && MapPublished(item) is { } dynamicLesson) return Ok(dynamicLesson);
        return Lessons.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) is { } lesson ? Ok(lesson) : NotFound();
    }

    private static GrammarLessonDto? MapPublished(EnglishLearning.Domain.LearningContentItem item)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<GrammarPayload>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return payload is null ? null : new(item.Key, item.Title, item.Level, payload.Summary ?? item.Title, payload.ContrastNote ?? "Bu derste Türkçe ve İngilizce yapılarını karşılaştır.", payload.Rules ?? [], payload.Exercises ?? []);
        }
        catch (JsonException) { return null; }
    }

    public sealed record GrammarLessonDto(string Key, string Title, string Level, string Summary, string ContrastNote, IReadOnlyList<GrammarRuleDto> Rules, IReadOnlyList<GrammarExerciseDto> Exercises);
    public sealed record GrammarRuleDto(string Title, string Explanation, string Focus);
    public sealed record GrammarExerciseDto(string Prompt, IReadOnlyList<string> Options, string Answer, string Explanation);
    private sealed record GrammarPayload(string? Summary, string? ContrastNote, IReadOnlyList<GrammarRuleDto>? Rules, IReadOnlyList<GrammarExerciseDto>? Exercises);
}
