using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Vocabulary;

public sealed class VocabularyService(EnglishLearningDbContext db) : IVocabularyService
{
    public async Task<SentenceChallenge?> GetSentenceChallengeAsync(CancellationToken ct)
    {
        var words = await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published && x.ExampleSentence != null && x.ExampleSentence != "").OrderBy(_ => EF.Functions.Random()).Take(100).ToListAsync(ct);
        var targets = words.Where(x => VocabularyQuality.ContainsWholeWord(x.ExampleSentence, x.Term)).ToList();
        if (targets.Count == 0) return null;
        var target = targets[Random.Shared.Next(targets.Count)];
        var blanked = VocabularyQuality.BlankWholeWord(target.ExampleSentence!, target.Term);
        var distractors = DistinctOptions(words.Where(x => x.Id != target.Id).Select(x => x.Term), target.Term);
        if (distractors.Count < 3) return null;
        var options = distractors.Append(target.Term).OrderBy(_ => Random.Shared.Next()).ToList();
        return new(target.Id, blanked, target.Term, options, target.Translation, $"Bu cümlede doğru kelime: {target.Term}.");
    }

    public async Task<WritingChallenge?> GetWritingChallengeAsync(CancellationToken ct)
    {
        var words = await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published && x.ExampleSentence != null && x.ExampleSentence != "").OrderBy(_ => EF.Functions.Random()).Take(100).ToListAsync(ct);
        if (words.Count == 0) return null;
        var target = words[Random.Shared.Next(words.Count)];
        return new(target.Id, target.Definition, target.Term, target.Translation, $"İlk harf: {target.Term[0]}");
    }

    public async Task<MatchingChallenge?> GetMatchingChallengeAsync(CancellationToken ct)
    {
        var words = await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published).OrderBy(_ => EF.Functions.Random()).Take(100).ToListAsync(ct);
        if (words.Count < 4) return null;
        var target = words[Random.Shared.Next(words.Count)];
        var distractors = DistinctOptions(words.Where(x => x.Id != target.Id).Select(x => x.Translation), target.Translation);
        if (distractors.Count < 3) return null;
        var options = distractors.Append(target.Translation).OrderBy(_ => Random.Shared.Next()).ToList();
        return new(target.Id, target.Term, target.Translation, options);
    }

    private static List<string> DistinctOptions(IEnumerable<string> candidates, string answer) => candidates
        .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(answer, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(_ => Random.Shared.Next()).Take(3).ToList();

    public async Task<VocabularyPage> SearchAsync(VocabularyFilter filter, CancellationToken ct)
    {
        var query = db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published);
        if (!string.IsNullOrWhiteSpace(filter.Level)) query = query.Where(x => x.Level == filter.Level);
        if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.Category == filter.Category);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(x => x.Term.Contains(filter.Search) || x.Translation.Contains(filter.Search));
        var total = await query.CountAsync(ct);
        var page = Math.Max(filter.Page, 1); var size = Math.Clamp(filter.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Term).Skip((page - 1) * size).Take(size).Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence)).ToListAsync(ct);
        return new(items, page, size, total);
    }
    public async Task<VocabularyWordDto?> GetAsync(Guid id, CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published && x.Id == id).Select(x => new VocabularyWordDto(x.Id, x.Term, x.Pronunciation, x.PartOfSpeech, x.Definition, x.Translation, x.Level, x.Category, x.ExampleSentence)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<VocabularyOption>> GetLevelsAsync(CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published && x.Level != null).Select(x => x.Level).Distinct().OrderBy(x => x).Select(x => new VocabularyOption(x, x)).ToListAsync(ct);
    public async Task<IReadOnlyList<VocabularyOption>> GetCategoriesAsync(CancellationToken ct) => await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published).Select(x => x.Category).Distinct().OrderBy(x => x).Select(x => new VocabularyOption(x, x)).ToListAsync(ct);
}
