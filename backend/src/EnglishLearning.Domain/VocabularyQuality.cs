using System.Text.RegularExpressions;

namespace EnglishLearning.Domain;

/// <summary>Shared content-quality rules for vocabulary publishing, importing and exercise generation.</summary>
public static class VocabularyQuality
{
    public const string PendingTranslationPlaceholder = "Çeviri inceleme bekliyor";
    public const string PendingDefinitionPlaceholder = "Bu kelimenin Türkçe karşılığı içerik ekibi tarafından doğrulanıyor.";
    // Written by the retired bootstrap that paired two independent frequency lists by index.
    public const string UnverifiedBootstrapDefinition = "Common English word used in everyday context.";
    public const string Blank = "_____";
    public static readonly IReadOnlySet<string> Levels = new HashSet<string>(["A1", "A2", "B1", "B2", "C1", "C2"], StringComparer.OrdinalIgnoreCase);

    public static bool IsPublishable(VocabularyWord word) =>
        !string.IsNullOrWhiteSpace(word.Term) &&
        !string.IsNullOrWhiteSpace(word.Translation) &&
        !IsPlaceholderTranslation(word.Translation) &&
        !string.IsNullOrWhiteSpace(word.Definition) &&
        !IsPlaceholderDefinition(word.Definition) &&
        !string.IsNullOrWhiteSpace(word.Category) &&
        !string.IsNullOrWhiteSpace(word.ExampleSentence) &&
        !word.Term.Equals(word.Translation, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns every problem that blocks a trimmed word from being imported; empty when the word is valid.</summary>
    public static IReadOnlyList<string> FindImportProblems(VocabularyWord word)
    {
        var problems = new List<string>();
        Require(word.Term, "term", 120); Require(word.PartOfSpeech, "partOfSpeech", 40); Require(word.Definition, "definition", 500);
        Require(word.Translation, "translation", 160); Require(word.Level, "level", 10); Require(word.Category, "category", 80);
        Require(word.ExampleSentence, "exampleSentence", 500);
        if (word.Pronunciation.Length > 120) problems.Add("pronunciation en fazla 120 karakter olabilir.");
        if (!string.IsNullOrWhiteSpace(word.Level) && !Levels.Contains(word.Level)) problems.Add("level A1, A2, B1, B2, C1 veya C2 olmalıdır.");
        if (!string.IsNullOrWhiteSpace(word.Term) && word.Term.Equals(word.Translation, StringComparison.OrdinalIgnoreCase)) problems.Add("translation terim ile aynı olamaz.");
        if (IsPlaceholderTranslation(word.Translation)) problems.Add("translation doğrulanmış bir çeviri olmalıdır.");
        if (IsPlaceholderDefinition(word.Definition)) problems.Add("definition yer tutucu metin olamaz.");
        if (!string.IsNullOrWhiteSpace(word.Term) && !string.IsNullOrWhiteSpace(word.ExampleSentence) && !ContainsWholeWord(word.ExampleSentence, word.Term))
            problems.Add("exampleSentence terimi tam kelime olarak içermelidir.");
        return problems;

        void Require(string? value, string field, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) problems.Add($"{field} zorunludur.");
            else if (value.Length > maxLength) problems.Add($"{field} en fazla {maxLength} karakter olabilir.");
        }
    }

    public static bool IsPlaceholderTranslation(string? translation) =>
        string.Equals(translation?.Trim(), PendingTranslationPlaceholder, StringComparison.OrdinalIgnoreCase);

    public static bool IsPlaceholderDefinition(string? definition) =>
        string.Equals(definition?.Trim(), PendingDefinitionPlaceholder, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(definition?.Trim(), UnverifiedBootstrapDefinition, StringComparison.OrdinalIgnoreCase);

    public static bool ContainsWholeWord(string? text, string term) =>
        !string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(term) && WholeWord(term).IsMatch(text);

    public static string BlankWholeWord(string text, string term) =>
        string.IsNullOrWhiteSpace(term) ? text : WholeWord(term).Replace(text, Blank);

    private static Regex WholeWord(string term) =>
        new($@"\b{Regex.Escape(term.Trim())}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
}
