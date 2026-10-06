using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLearning.Api.Controllers;
using EnglishLearning.Domain;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

[Collection(Collections.Content), Trait("Category", "Integration")]
public sealed class VocabularyImportTests(ContentFixture fixture)
{
    private const string ImportUrl = "/api/v1/admin/content/vocabulary/import";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpClient> ClientAsync(string role = "Editor")
    {
        fixture.SkipIfUnavailable();
        return fixture.CreateClient(await fixture.CreateUserAsync(role));
    }

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, params VocabularyImportItem?[] words) =>
        client.PostAsJsonAsync(ImportUrl, new VocabularyImportRequest(words), Ct);

    private static async Task<List<VocabularyImportInvalidRow>> InvalidRowsAsync(HttpResponseMessage response) =>
        (await ReadJsonAsync(response)).GetProperty("invalid").Deserialize<List<VocabularyImportInvalidRow>>(Json)!;

    private Task<List<VocabularyWord>> WordsAsync(params string[] terms) => fixture.WithDbAsync(db =>
        db.VocabularyWords.AsNoTracking().Where(x => terms.Contains(x.Term)).ToListAsync(Ct));

    private Task<VocabularyWord> SeedAsync(VocabularyWord word) => fixture.WithDbAsync(async db =>
    {
        db.VocabularyWords.Add(word);
        await db.SaveChangesAsync(Ct);
        return word;
    });

    [Fact]
    public async Task Import_OneInvalidRow_Returns400WithIndexAndWritesNothing()
    {
        using var client = await ClientAsync();
        var s = Suffix();
        var bad = ImportItem($"bad{s}", "D1");

        using var response = await ImportAsync(client, ImportItem($"good{s}"), bad, ImportItem($"other{s}"));

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        var invalid = Assert.Single(await InvalidRowsAsync(response));
        Assert.Equal(1, invalid.Index);
        Assert.Equal($"bad{s}", invalid.Term);
        Assert.Empty(await WordsAsync($"good{s}", $"bad{s}", $"other{s}"));
    }

    [Fact]
    public async Task Import_DuplicateTermsDifferingOnlyByCase_Returns400()
    {
        using var client = await ClientAsync();
        var s = Suffix();

        using var response = await ImportAsync(client, ImportItem($"dup{s}"), ImportItem($"DUP{s}"));

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        Assert.Equal(1, Assert.Single(await InvalidRowsAsync(response)).Index);
        Assert.Empty(await WordsAsync($"dup{s}", $"DUP{s}"));
    }

    [Fact]
    public async Task Import_MoreThan1000Rows_Returns400()
    {
        using var client = await ClientAsync();
        var s = Suffix();
        var items = Enumerable.Range(0, 1001).Select(i => ImportItem($"bulk{i}x{s}")).ToArray<VocabularyImportItem?>();

        using var response = await ImportAsync(client, items);

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        Assert.Equal(0, await fixture.WithDbAsync(db => db.VocabularyWords.CountAsync(x => x.Term.EndsWith(s), Ct)));
    }

    [Fact]
    public async Task Import_NullItem_IsReportedByIndex()
    {
        using var client = await ClientAsync();
        var s = Suffix();

        using var response = await ImportAsync(client, ImportItem($"fine{s}"), null);

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        Assert.Equal(1, Assert.Single(await InvalidRowsAsync(response)).Index);
        Assert.Empty(await WordsAsync($"fine{s}"));
    }

    [Fact]
    public async Task Import_ExistingPublishedWord_IsSkippedAndUnchanged()
    {
        using var client = await ClientAsync();
        var s = Suffix();
        var published = await SeedAsync(Word($"pub{s}", "A1"));

        using var response = await ImportAsync(client, ImportItem($"PUB{s}", "B2", "a completely different meaning"));

        await AssertStatusAsync(HttpStatusCode.OK, response);
        var result = (await response.Content.ReadFromJsonAsync<VocabularyImportResult>(Json, Ct))!;
        Assert.Equal(0, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal([$"pub{s}"], result.SkippedPublished);
        var stored = Assert.Single(await WordsAsync($"pub{s}", $"PUB{s}"));
        Assert.Equal(published.Id, stored.Id);
        Assert.Equal(VocabularyPublicationStatus.Published, stored.PublicationStatus);
        Assert.Equal(published.Definition, stored.Definition);
        Assert.Equal("A1", stored.Level);
        Assert.NotNull(stored.PublishedAtUtc);
    }

    [Fact]
    public async Task Import_ExistingRejectedWord_ReturnsToReviewWithPublishedAtCleared()
    {
        fixture.SkipIfUnavailable();
        var editor = await fixture.CreateUserAsync("Editor");
        using var client = fixture.CreateClient(editor);
        var s = Suffix();
        var rejected = Word($"rej{s}", "A1", VocabularyPublicationStatus.Rejected);
        rejected.PublishedAtUtc = DateTime.UtcNow.AddDays(-3);
        await SeedAsync(rejected);

        using var response = await ImportAsync(client, ImportItem($"rej{s}", "B1", "a corrected meaning"));

        await AssertStatusAsync(HttpStatusCode.OK, response);
        var result = (await response.Content.ReadFromJsonAsync<VocabularyImportResult>(Json, Ct))!;
        Assert.Equal(1, result.Updated);
        var stored = Assert.Single(await WordsAsync($"rej{s}"));
        Assert.Equal(rejected.Id, stored.Id);
        Assert.Equal(VocabularyPublicationStatus.InReview, stored.PublicationStatus);
        Assert.Null(stored.PublishedAtUtc);
        Assert.Equal("a corrected meaning", stored.Definition);
        Assert.Equal("B1", stored.Level);
        Assert.True(await fixture.WithDbAsync(db => db.AdminAuditLogs.AnyAsync(x => x.EntityId == rejected.Id && x.Action == "import-update" && x.UserId == editor.Id, Ct)));
    }

    [Fact]
    public async Task Import_NewTerm_IsCreatedInReviewWithAuditRow()
    {
        fixture.SkipIfUnavailable();
        var editor = await fixture.CreateUserAsync("Editor");
        using var client = fixture.CreateClient(editor);
        var s = Suffix();

        using var response = await ImportAsync(client, ImportItem($"  new{s}  "), ImportItem($"newer{s}"));

        await AssertStatusAsync(HttpStatusCode.OK, response);
        var result = (await response.Content.ReadFromJsonAsync<VocabularyImportResult>(Json, Ct))!;
        Assert.Equal(2, result.Created);
        Assert.Equal(2, result.Total);
        var stored = await WordsAsync($"new{s}", $"newer{s}");
        Assert.Equal(2, stored.Count);
        Assert.All(stored, w =>
        {
            Assert.Equal(VocabularyPublicationStatus.InReview, w.PublicationStatus);
            Assert.Null(w.PublishedAtUtc);
        });
        var ids = stored.Select(w => (Guid?)w.Id).ToArray();
        var audits = await fixture.WithDbAsync(db => db.AdminAuditLogs.AsNoTracking().Where(x => ids.Contains(x.EntityId)).ToListAsync(Ct));
        Assert.Equal(2, audits.Count);
        Assert.All(audits, a =>
        {
            Assert.Equal("import-create", a.Action);
            Assert.Equal("VocabularyWord", a.EntityType);
            Assert.Equal(editor.Id, a.UserId);
        });
    }

    [Fact]
    public async Task Import_TermMatchingAmbiguousCaseVariantsInDb_Returns400AndWritesNothing()
    {
        using var client = await ClientAsync();
        var s = Suffix();
        var upper = await SeedAsync(Word($"Run{s}", "A1", VocabularyPublicationStatus.Draft));
        var lower = await SeedAsync(Word($"run{s}", "A1", VocabularyPublicationStatus.Draft));

        using var response = await ImportAsync(client, ImportItem($"RUN{s}", "B1", "an ambiguous update"), ImportItem($"fresh{s}"));

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        Assert.Equal(0, Assert.Single(await InvalidRowsAsync(response)).Index);
        Assert.Empty(await WordsAsync($"fresh{s}"));
        var stored = await WordsAsync($"Run{s}", $"run{s}");
        Assert.Equal(2, stored.Count);
        Assert.All(stored, w =>
        {
            Assert.Equal(VocabularyPublicationStatus.Draft, w.PublicationStatus);
            Assert.Equal("A1", w.Level);
        });
        Assert.False(await fixture.WithDbAsync(db => db.AdminAuditLogs.AnyAsync(x => x.EntityId == upper.Id || x.EntityId == lower.Id, Ct)));
    }

    [Fact]
    public async Task Import_LowercaseLevel_IsNormalized()
    {
        using var client = await ClientAsync();
        var s = Suffix();

        using var response = await ImportAsync(client, ImportItem($"lvl{s}", " b1 "));

        await AssertStatusAsync(HttpStatusCode.OK, response);
        Assert.Equal("B1", Assert.Single(await WordsAsync($"lvl{s}")).Level);
    }

    [Fact]
    public async Task Import_ByLearner_Returns403()
    {
        using var client = await ClientAsync("Learner");
        var s = Suffix();

        using var response = await ImportAsync(client, ImportItem($"learner{s}"));

        await AssertStatusAsync(HttpStatusCode.Forbidden, response);
        Assert.Empty(await WordsAsync($"learner{s}"));
    }

    [Fact]
    public async Task Import_WithoutToken_Returns401()
    {
        fixture.SkipIfUnavailable();
        using var client = fixture.CreateClient();
        using var response = await ImportAsync(client, ImportItem($"anon{Suffix()}"));
        await AssertStatusAsync(HttpStatusCode.Unauthorized, response);
    }
}
