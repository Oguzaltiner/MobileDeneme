namespace EnglishLearning.Application.Vocabulary;

public sealed record VocabularyWordDto(Guid Id, string Term, string Pronunciation, string PartOfSpeech, string Definition, string Translation, string Level, string Category, string? ExampleSentence);
public sealed record VocabularyFilter(string? Level, string? Category, string? Search, int Page = 1, int PageSize = 20);
public sealed record VocabularyPage(IReadOnlyList<VocabularyWordDto> Items, int Page, int PageSize, int TotalCount);
public sealed record VocabularyOption(string Value, string Label);

public interface IVocabularyService
{
    Task<VocabularyPage> SearchAsync(VocabularyFilter filter, CancellationToken ct);
    Task<VocabularyWordDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<VocabularyOption>> GetLevelsAsync(CancellationToken ct);
    Task<IReadOnlyList<VocabularyOption>> GetCategoriesAsync(CancellationToken ct);
}
