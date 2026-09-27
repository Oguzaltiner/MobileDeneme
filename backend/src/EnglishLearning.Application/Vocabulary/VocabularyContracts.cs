namespace EnglishLearning.Application.Vocabulary;

public sealed record VocabularyWordDto(Guid Id, string Term, string Pronunciation, string PartOfSpeech, string Definition, string Translation, string Level, string Category, string? ExampleSentence);
public sealed record VocabularyFilter(string? Level, string? Category, string? Search, int Page = 1, int PageSize = 20);
public sealed record VocabularyPage(IReadOnlyList<VocabularyWordDto> Items, int Page, int PageSize, int TotalCount);
public sealed record VocabularyOption(string Value, string Label);
public sealed record WritingChallenge(Guid WordId, string Prompt, string Answer, string Translation, string Hint);
public sealed record SentenceChallenge(Guid WordId, string Sentence, string Answer, IReadOnlyList<string> Options, string Translation, string Explanation);

public interface IVocabularyService
{
    Task<VocabularyPage> SearchAsync(VocabularyFilter filter, CancellationToken ct);
    Task<VocabularyWordDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<VocabularyOption>> GetLevelsAsync(CancellationToken ct);
    Task<IReadOnlyList<VocabularyOption>> GetCategoriesAsync(CancellationToken ct);
    Task<SentenceChallenge?> GetSentenceChallengeAsync(CancellationToken ct);
    Task<WritingChallenge?> GetWritingChallengeAsync(CancellationToken ct);
}
