using EnglishLearning.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Tests.Integration.Infrastructure;

public static class Collections
{
    public const string Quiz = "QuizDb";
    public const string QuizScarcity = "QuizScarcityDb";
    public const string Content = "ContentDb";
    public const string PublishCap = "PublishCapDb";
    public const string Mission = "MissionDb";
}

/// <summary>
/// Controlled published pool: 6 words for each of A1, A2, B1, B2 and C1. The words seeded by
/// migrations are moved to Draft so quiz composition depends only on this pool.
/// </summary>
public sealed class QuizFixture : PostgresApiFixture
{
    public static readonly string[] PoolLevels = ["A1", "A2", "B1", "B2", "C1"];
    public const int WordsPerLevel = 6;

    public IReadOnlyList<VocabularyWord> Pool { get; private set; } = [];

    protected override async Task SeedAsync()
    {
        var suffix = TestData.Suffix();
        Pool = PoolLevels.SelectMany(level => Enumerable.Range(1, WordsPerLevel)
            .Select(i => TestData.Word($"q{level.ToLowerInvariant()}w{i}x{suffix}", level))).ToList();
        await WithDbAsync(async db =>
        {
            await db.VocabularyWords.ExecuteUpdateAsync(s => s.SetProperty(x => x.PublicationStatus, VocabularyPublicationStatus.Draft));
            db.VocabularyWords.AddRange(Pool);
            await db.SaveChangesAsync();
        });
    }
}

/// <summary>Only five published words that all share one translation, so translation distractors run out.</summary>
public sealed class QuizScarcityFixture : PostgresApiFixture
{
    protected override async Task SeedAsync()
    {
        var suffix = TestData.Suffix();
        await WithDbAsync(async db =>
        {
            await db.VocabularyWords.ExecuteUpdateAsync(s => s.SetProperty(x => x.PublicationStatus, VocabularyPublicationStatus.Draft));
            db.VocabularyWords.AddRange(Enumerable.Range(1, 5).Select(i => TestData.Word($"scarce{i}x{suffix}", "A2", translation: "aynı")));
            await db.SaveChangesAsync();
        });
    }
}

public sealed class ContentFixture : PostgresApiFixture;

/// <summary>
/// Daily mission pool: 12 published words for each of A1, A2 and B1 (migration-seeded words are
/// moved to Draft), so new words, recall distractors and quota sizing are predictable.
/// </summary>
public sealed class MissionFixture : PostgresApiFixture
{
    public static readonly string[] PoolLevels = ["A1", "A2", "B1"];
    public const int WordsPerLevel = 12;

    public IReadOnlyList<VocabularyWord> Pool { get; private set; } = [];

    protected override async Task SeedAsync()
    {
        var suffix = TestData.Suffix();
        Pool = PoolLevels.SelectMany(level => Enumerable.Range(1, WordsPerLevel)
            .Select(i => TestData.Word($"m{level.ToLowerInvariant()}w{i}x{suffix}", level))).ToList();
        await WithDbAsync(async db =>
        {
            await db.VocabularyWords.ExecuteUpdateAsync(s => s.SetProperty(x => x.PublicationStatus, VocabularyPublicationStatus.Draft));
            db.VocabularyWords.AddRange(Pool);
            await db.SaveChangesAsync();
        });
    }
}

public sealed class PublishCapFixture : PostgresApiFixture;

[CollectionDefinition(Collections.Quiz)]
public sealed class QuizCollection : ICollectionFixture<QuizFixture>;

[CollectionDefinition(Collections.QuizScarcity)]
public sealed class QuizScarcityCollection : ICollectionFixture<QuizScarcityFixture>;

[CollectionDefinition(Collections.Content)]
public sealed class ContentCollection : ICollectionFixture<ContentFixture>;

[CollectionDefinition(Collections.Mission)]
public sealed class MissionCollection : ICollectionFixture<MissionFixture>;

[CollectionDefinition(Collections.PublishCap)]
public sealed class PublishCapCollection : ICollectionFixture<PublishCapFixture>;
