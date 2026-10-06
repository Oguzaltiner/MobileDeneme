using System.Net;
using System.Net.Http.Json;
using EnglishLearning.Api.Controllers;
using EnglishLearning.Domain;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

[Collection(Collections.Content), Trait("Category", "Integration")]
public sealed class ContentPublishTests(ContentFixture fixture)
{
    private const string BulkUrl = "/api/v1/admin/content/vocabulary/bulk-publish";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpClient> ClientAsync(string role)
    {
        fixture.SkipIfUnavailable();
        return fixture.CreateClient(await fixture.CreateUserAsync(role));
    }

    private Task SeedAsync(params VocabularyWord[] words) => fixture.WithDbAsync(async db =>
    {
        db.VocabularyWords.AddRange(words);
        await db.SaveChangesAsync(Ct);
    });

    private Task<VocabularyWord> ReloadAsync(Guid id) => fixture.WithDbAsync(db => db.VocabularyWords.AsNoTracking().SingleAsync(x => x.Id == id, Ct));

    private static Task<HttpResponseMessage> BulkAsync(HttpClient client, params Guid[] ids) =>
        client.PostAsJsonAsync(BulkUrl, new BulkVocabularyPublishRequest(ids), Ct);

    [Fact]
    public async Task BulkPublish_MoreThan100Ids_Returns400()
    {
        using var client = await ClientAsync("Reviewer");
        using var response = await BulkAsync(client, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray());
        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
    }

    [Fact]
    public async Task BulkPublish_WordNotInReview_Returns409WithTermsAndPublishesNothing()
    {
        using var client = await ClientAsync("Reviewer");
        var s = Suffix();
        var draft = Word($"draft{s}", status: VocabularyPublicationStatus.Draft);
        var ready = Word($"ready{s}", status: VocabularyPublicationStatus.InReview);
        await SeedAsync(draft, ready);

        using var response = await BulkAsync(client, draft.Id, ready.Id);

        await AssertStatusAsync(HttpStatusCode.Conflict, response);
        var terms = (await ReadJsonAsync(response)).GetProperty("terms").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal([$"draft{s}"], terms);
        Assert.Equal(VocabularyPublicationStatus.Draft, (await ReloadAsync(draft.Id)).PublicationStatus);
        Assert.Equal(VocabularyPublicationStatus.InReview, (await ReloadAsync(ready.Id)).PublicationStatus);
    }

    [Fact]
    public async Task Publish_PlaceholderWord_Returns409ForSingleAndBulk()
    {
        using var client = await ClientAsync("Reviewer");
        var s = Suffix();
        var placeholder = Word($"ph{s}", status: VocabularyPublicationStatus.InReview, translation: VocabularyQuality.PendingTranslationPlaceholder);
        var bootstrap = Word($"bs{s}", status: VocabularyPublicationStatus.InReview);
        bootstrap.Definition = VocabularyQuality.UnverifiedBootstrapDefinition;
        await SeedAsync(placeholder, bootstrap);

        using (var single = await client.PostAsync($"/api/v1/admin/content/vocabulary/{placeholder.Id}/publish", null, Ct))
            await AssertStatusAsync(HttpStatusCode.Conflict, single);
        using var bulk = await BulkAsync(client, placeholder.Id, bootstrap.Id);
        await AssertStatusAsync(HttpStatusCode.Conflict, bulk);
        var terms = (await ReadJsonAsync(bulk)).GetProperty("terms").EnumerateArray().Select(x => x.GetString()).Order().ToList();

        Assert.Equal([$"bs{s}", $"ph{s}"], terms);
        Assert.Equal(VocabularyPublicationStatus.InReview, (await ReloadAsync(placeholder.Id)).PublicationStatus);
        Assert.Equal(VocabularyPublicationStatus.InReview, (await ReloadAsync(bootstrap.Id)).PublicationStatus);
    }

    [Fact]
    public async Task Publish_ByEditor_Returns403ForSingleAndBulk()
    {
        using var client = await ClientAsync("Editor");
        var word = Word($"ed{Suffix()}", status: VocabularyPublicationStatus.InReview);
        await SeedAsync(word);

        using (var single = await client.PostAsync($"/api/v1/admin/content/vocabulary/{word.Id}/publish", null, Ct))
            await AssertStatusAsync(HttpStatusCode.Forbidden, single);
        using (var bulk = await BulkAsync(client, word.Id))
            await AssertStatusAsync(HttpStatusCode.Forbidden, bulk);

        Assert.Equal(VocabularyPublicationStatus.InReview, (await ReloadAsync(word.Id)).PublicationStatus);
    }

    [Fact]
    public async Task Publish_ByLearner_Returns403()
    {
        using var client = await ClientAsync("Learner");
        var word = Word($"ln{Suffix()}", status: VocabularyPublicationStatus.InReview);
        await SeedAsync(word);

        using var bulk = await BulkAsync(client, word.Id);
        await AssertStatusAsync(HttpStatusCode.Forbidden, bulk);
    }

    [Fact]
    public async Task Publish_ByReviewer_PublishesSingleAndBulkWithAudit()
    {
        fixture.SkipIfUnavailable();
        var reviewer = await fixture.CreateUserAsync("Reviewer");
        using var client = fixture.CreateClient(reviewer);
        var s = Suffix();
        var single = Word($"rs{s}", status: VocabularyPublicationStatus.InReview);
        var bulkA = Word($"rba{s}", status: VocabularyPublicationStatus.InReview);
        var bulkB = Word($"rbb{s}", status: VocabularyPublicationStatus.InReview);
        await SeedAsync(single, bulkA, bulkB);

        using (var response = await client.PostAsync($"/api/v1/admin/content/vocabulary/{single.Id}/publish", null, Ct))
            await AssertStatusAsync(HttpStatusCode.NoContent, response);
        using (var response = await BulkAsync(client, bulkA.Id, bulkB.Id, bulkA.Id))
        {
            await AssertStatusAsync(HttpStatusCode.OK, response);
            Assert.Equal(2, (await ReadJsonAsync(response)).GetProperty("published").GetInt32());
        }

        foreach (var id in new[] { single.Id, bulkA.Id, bulkB.Id })
        {
            var stored = await ReloadAsync(id);
            Assert.Equal(VocabularyPublicationStatus.Published, stored.PublicationStatus);
            Assert.NotNull(stored.PublishedAtUtc);
        }
        var actions = await fixture.WithDbAsync(db => db.AdminAuditLogs.AsNoTracking()
            .Where(x => x.UserId == reviewer.Id).Select(x => x.Action).ToListAsync(Ct));
        Assert.Equal(["bulk-publish", "bulk-publish", "publish"], actions.Order());
    }
}
