using EnglishLearning.Domain;

namespace EnglishLearning.Tests.Unit;

/// <summary>Pure rules from <see cref="VocabularyQuality"/>; no database or host required.</summary>
public sealed class VocabularyQualityTests
{
    private static VocabularyWord ValidWord() => new()
    {
        Term = "achieve", Pronunciation = "/əˈtʃiːv/", PartOfSpeech = "verb",
        Definition = "to succeed in doing something", Translation = "başarmak",
        Level = "A2", Category = "Daily Life", ExampleSentence = "You can achieve your goals."
    };

    [Fact]
    public void IsPublishable_ValidWord_ReturnsTrue() => Assert.True(VocabularyQuality.IsPublishable(ValidWord()));

    [Theory]
    [InlineData(VocabularyQuality.PendingTranslationPlaceholder)]
    [InlineData("  Çeviri inceleme bekliyor  ")]
    [InlineData("çeviri inceleme BEKLIYOR\t")]
    public void IsPublishable_PendingTranslationPlaceholder_ReturnsFalse(string translation)
    {
        var word = ValidWord();
        word.Translation = translation;
        Assert.False(VocabularyQuality.IsPublishable(word));
    }

    [Theory]
    [InlineData(VocabularyQuality.PendingDefinitionPlaceholder)]
    [InlineData(VocabularyQuality.UnverifiedBootstrapDefinition)]
    [InlineData("  Common English word used in everyday context.  ")]
    [InlineData("COMMON ENGLISH WORD USED IN EVERYDAY CONTEXT.")]
    [InlineData("common english word used in everyday context.\n")]
    public void IsPublishable_PlaceholderDefinition_ReturnsFalse(string definition)
    {
        var word = ValidWord();
        word.Definition = definition;
        Assert.False(VocabularyQuality.IsPublishable(word));
    }

    [Theory]
    [InlineData("achieve")]
    [InlineData("ACHIEVE")]
    public void IsPublishable_TranslationEqualsTerm_ReturnsFalse(string translation)
    {
        var word = ValidWord();
        word.Translation = translation;
        Assert.False(VocabularyQuality.IsPublishable(word));
    }

    [Fact]
    public void FindImportProblems_ValidWord_ReturnsEmpty() => Assert.Empty(VocabularyQuality.FindImportProblems(ValidWord()));

    [Fact]
    public void FindImportProblems_ReportsEveryProblemInOnePass()
    {
        var word = new VocabularyWord
        {
            Term = "cat", Pronunciation = new string('p', 121), PartOfSpeech = "",
            Definition = new string('d', 501), Translation = "kedi", Level = "D1", Category = "   ",
            ExampleSentence = "Never concatenate strings in a loop."
        };

        var problems = VocabularyQuality.FindImportProblems(word);

        Assert.Contains(problems, p => p.StartsWith("partOfSpeech", StringComparison.Ordinal) && p.Contains("zorunludur"));
        Assert.Contains(problems, p => p.StartsWith("category", StringComparison.Ordinal) && p.Contains("zorunludur"));
        Assert.Contains(problems, p => p.StartsWith("definition", StringComparison.Ordinal) && p.Contains("500"));
        Assert.Contains(problems, p => p.StartsWith("pronunciation", StringComparison.Ordinal) && p.Contains("120"));
        Assert.Contains(problems, p => p.StartsWith("level", StringComparison.Ordinal) && p.Contains("A1"));
        Assert.Contains(problems, p => p.StartsWith("exampleSentence", StringComparison.Ordinal));
        Assert.Equal(6, problems.Count);
    }

    [Fact]
    public void FindImportProblems_MissingAndOverlongRequiredFields_AreAllReported()
    {
        var word = new VocabularyWord
        {
            Term = new string('t', 121), Pronunciation = "", PartOfSpeech = new string('p', 41), Definition = "",
            Translation = new string('c', 161), Level = "", Category = new string('k', 81), ExampleSentence = null
        };

        var problems = VocabularyQuality.FindImportProblems(word);

        Assert.Contains(problems, p => p.StartsWith("term", StringComparison.Ordinal) && p.Contains("120"));
        Assert.Contains(problems, p => p.StartsWith("partOfSpeech", StringComparison.Ordinal) && p.Contains("40"));
        Assert.Contains(problems, p => p.StartsWith("definition", StringComparison.Ordinal) && p.Contains("zorunludur"));
        Assert.Contains(problems, p => p.StartsWith("translation", StringComparison.Ordinal) && p.Contains("160"));
        Assert.Contains(problems, p => p.StartsWith("level", StringComparison.Ordinal) && p.Contains("zorunludur"));
        Assert.Contains(problems, p => p.StartsWith("category", StringComparison.Ordinal) && p.Contains("80"));
        Assert.Contains(problems, p => p.StartsWith("exampleSentence", StringComparison.Ordinal) && p.Contains("zorunludur"));
    }

    [Fact]
    public void FindImportProblems_PlaceholdersAndTermEqualTranslation_AreReported()
    {
        var placeholders = ValidWord();
        placeholders.Translation = VocabularyQuality.PendingTranslationPlaceholder;
        placeholders.Definition = VocabularyQuality.UnverifiedBootstrapDefinition;
        var placeholderProblems = VocabularyQuality.FindImportProblems(placeholders);
        Assert.Contains(placeholderProblems, p => p.StartsWith("translation", StringComparison.Ordinal));
        Assert.Contains(placeholderProblems, p => p.StartsWith("definition", StringComparison.Ordinal));

        var same = ValidWord();
        same.Translation = "Achieve";
        Assert.Contains(VocabularyQuality.FindImportProblems(same), p => p.StartsWith("translation", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Do not concatenate them.", "cat", false)]
    [InlineData("Cat.", "cat", true)]
    [InlineData("The cat sleeps.", "Cat", true)]
    [InlineData("Can you look after my dog?", "look after", true)]
    [InlineData("Can you look at my dog?", "look after", false)]
    [InlineData("Exercise has many benefits.", "benefit", false)]
    [InlineData("Exercise is a benefit.", "benefit", true)]
    [InlineData("", "cat", false)]
    [InlineData(null, "cat", false)]
    [InlineData("A cat.", " ", false)]
    public void ContainsWholeWord_MatchesOnlyWholeWords(string? text, string term, bool expected) =>
        Assert.Equal(expected, VocabularyQuality.ContainsWholeWord(text, term));

    /// <summary>
    /// Characterization of a documented limitation: \b anchors need a word character at the
    /// term's edges, so terms ending in symbols such as "C++" are never matched.
    /// </summary>
    [Fact]
    public void ContainsWholeWord_TermEndingInSymbols_IsNotMatched_KnownLimitation() =>
        Assert.False(VocabularyQuality.ContainsWholeWord("I write C++ every day.", "C++"));

    [Fact]
    public void BlankWholeWord_ReplacesEveryWholeOccurrenceCaseInsensitively()
    {
        var result = VocabularyQuality.BlankWholeWord("Cat and cat and CAT, never concatenate cats.", "cat");
        Assert.Equal("_____ and _____ and _____, never concatenate cats.", result);
    }

    [Fact]
    public void BlankWholeWord_MultiWordTerm_IsBlanked()
    {
        Assert.Equal("Please _____ the baby.", VocabularyQuality.BlankWholeWord("Please look after the baby.", "look after"));
    }

    [Fact]
    public void BlankWholeWord_WhitespaceTerm_ReturnsTextUnchanged() =>
        Assert.Equal("A cat.", VocabularyQuality.BlankWholeWord("A cat.", "  "));
}
