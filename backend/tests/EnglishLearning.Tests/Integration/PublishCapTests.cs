using System.Net;
using System.Net.Http.Json;
using EnglishLearning.Api.Controllers;
using EnglishLearning.Domain;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

/// <summary>Own database: these tests move the global published count to the cap.</summary>
[Collection(Collections.PublishCap), Trait("Category", "Integration")]
public sealed class PublishCapTests(PublishCapFixture fixture)
{
    private const string FillerCategory = "CapFiller";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Sets the published total to exactly <paramref name="target"/> by inserting or un-publishing filler rows.</summary>
    private Task SetPublishedCountAsync(int target) => fixture.WithDbAsync(async db =>
    {
        var published = await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, Ct);
        if (published < target)
        {
            var s = Suffix();
            db.VocabularyWords.AddRange(Enumerable.Range(0, target - published).Select(i =>
            {
                var word = Word($"fill{i}x{s}");
                word.Category = FillerCategory;
                return word;
            }));
            await db.SaveChangesAsync(Ct);
        }
        else if (published > target)
        {
            var ids = await db.VocabularyWords.Where(x => x.Category == FillerCategory && x.PublicationStatus == VocabularyPublicationStatus.Published)
                .OrderBy(x => x.Id).Select(x => x.Id).Take(published - target).ToListAsync(Ct);
            Assert.Equal(published - target, ids.Count);
            await db.VocabularyWords.Where(x => ids.Contains(x.Id))
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.PublicationStatus, VocabularyPublicationStatus.Draft).SetProperty(x => x.PublishedAtUtc, (DateTime?)null), Ct);
        }
        Assert.Equal(target, await db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, Ct));
    });

    private async Task<(HttpClient Client, VocabularyWord[] Words)> ReviewerWithInReviewWordsAsync(int count)
    {
        fixture.SkipIfUnavailable();
        var client = fixture.CreateClient(await fixture.CreateUserAsync("Reviewer"));
        var s = Suffix();
        var words = Enumerable.Range(0, count).Select(i => Word($"cap{i}x{s}", status: VocabularyPublicationStatus.InReview)).ToArray();
        await fixture.WithDbAsync(async db => { db.VocabularyWords.AddRange(words); await db.SaveChangesAsync(Ct); });
        return (client, words);
    }

    private Task<int> PublishedCountAsync() => fixture.WithDbAsync(db =>
        db.VocabularyWords.CountAsync(x => x.PublicationStatus == VocabularyPublicationStatus.Published, Ct));

    [Fact]
    public async Task BulkPublish_ExceedingCap_Returns409WithRemainingAndPublishesNothing()
    {
        var (client, words) = await ReviewerWithInReviewWordsAsync(2);
        await SetPublishedCountAsync(VocabularyLimits.MaxPublishedWords - 1);

        using var response = await client.PostAsJsonAsync("/api/v1/admin/content/vocabulary/bulk-publish",
            new BulkVocabularyPublishRequest(words.Select(w => w.Id).ToArray()), Ct);

        await AssertStatusAsync(HttpStatusCode.Conflict, response);
        Assert.Equal(1, (await ReadJsonAsync(response)).GetProperty("remaining").GetInt32());
        Assert.Equal(VocabularyLimits.MaxPublishedWords - 1, await PublishedCountAsync());
    }

    [Fact]
    public async Task BulkPublish_ExactlyFillingCap_Succeeds()
    {
        var (client, words) = await ReviewerWithInReviewWordsAsync(2);
        await SetPublishedCountAsync(VocabularyLimits.MaxPublishedWords - 2);

        using var response = await client.PostAsJsonAsync("/api/v1/admin/content/vocabulary/bulk-publish",
            new BulkVocabularyPublishRequest(words.Select(w => w.Id).ToArray()), Ct);

        await AssertStatusAsync(HttpStatusCode.OK, response);
        Assert.Equal(VocabularyLimits.MaxPublishedWords, await PublishedCountAsync());
    }

    [Fact]
    public async Task SinglePublish_AtCap_Returns409()
    {
        var (client, words) = await ReviewerWithInReviewWordsAsync(1);
        await SetPublishedCountAsync(VocabularyLimits.MaxPublishedWords);

        using var response = await client.PostAsync($"/api/v1/admin/content/vocabulary/{words[0].Id}/publish", null, Ct);

        await AssertStatusAsync(HttpStatusCode.Conflict, response);
        Assert.Equal(VocabularyLimits.MaxPublishedWords, await PublishedCountAsync());
        Assert.Equal(VocabularyPublicationStatus.InReview,
            await fixture.WithDbAsync(db => db.VocabularyWords.Where(x => x.Id == words[0].Id).Select(x => x.PublicationStatus).SingleAsync(Ct)));
    }
}
