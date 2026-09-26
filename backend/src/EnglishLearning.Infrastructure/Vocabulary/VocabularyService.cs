using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Vocabulary;

public sealed class VocabularyService(EnglishLearningDbContext db) : IVocabularyService
{
    public async Task<VocabularyPage> SearchAsync(VocabularyFilter filter, CancellationToken ct)
    {
        var query = db.VocabularyWords.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Level)) query = query.Where(x => x.Level == filter.Level);
        if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.Category == filter.Category);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(x => x.Term.Contains(filter.Search) || x.Translation.Contains(filter.Search));
        var total = await query.CountAsync(ct);
        var page = Math.Max(filter.Page, 1); var size = Math.Clamp(filter.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Term).Skip((page - 1) * size).Take(size).Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence)).ToListAsync(ct);
        return new(items, page, size, total);
    }
    public async Task<VocabularyWordDto?> GetAsync(Guid id, CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Where(x => x.Id == id).Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<VocabularyOption>> GetLevelsAsync(CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Where(x => x.Level != null).Select(x => x.Level).Distinct().OrderBy(x => x).Select(x => new VocabularyOption(x, x)).ToListAsync(ct);
    public async Task<IReadOnlyList<VocabularyOption>> GetCategoriesAsync(CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Select(x => x.Category).Distinct().OrderBy(x => x).Select(x => new VocabularyOption(x, x)).ToListAsync(ct);
}
