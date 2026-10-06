using System.Net;
using EnglishLearning.Application.Quiz;
using EnglishLearning.Domain;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

[Collection(Collections.Quiz), Trait("Category", "Integration")]
public sealed class EntitlementTests(QuizFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<int> QuizzesStartedTodayAsync(Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return fixture.WithDbAsync(db => db.DailyUsages.AsNoTracking()
            .Where(x => x.UserId == userId && x.DateUtc == today).Select(x => x.QuizzesStarted).SingleOrDefaultAsync(Ct));
    }

    private Task<List<string>> SessionLevelsAsync(Guid sessionId) => fixture.WithDbAsync(db => db.QuizQuestions.AsNoTracking()
        .Where(x => x.SessionId == sessionId).Select(x => x.VocabularyWord.Level).ToListAsync(Ct));

    [Fact]
    public async Task FreeUser_SecondQuizSameDay_Returns429()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync();
        using var client = fixture.CreateClient(user);

        await CreateQuizOkAsync(client, 5);
        using var second = await CreateQuizAsync(client, 5);

        await AssertStatusAsync(HttpStatusCode.TooManyRequests, second);
        Assert.Equal(1, await QuizzesStartedTodayAsync(user.Id));
        Assert.Equal(1, await fixture.WithDbAsync(db => db.QuizSessions.CountAsync(x => x.UserId == user.Id, Ct)));
    }

    /// <summary>A Free user asking for a Premium level gets 403 with an upgrade message (level case-insensitive).</summary>
    [Theory]
    [InlineData("B1")]
    [InlineData(" b1 ")]
    public async Task FreeUser_RequestingB1_Returns403WithoutConsumingQuota(string level)
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync();
        using var client = fixture.CreateClient(user);

        using var response = await CreateQuizAsync(client, 5, level);
        await AssertStatusAsync(HttpStatusCode.Forbidden, response);
        Assert.Equal("Bu seviye Premium üyelikte açılır.", (await ReadJsonAsync(response)).GetProperty("message").GetString());
        Assert.Equal(0, await QuizzesStartedTodayAsync(user.Id));

        // The daily quiz is still available afterwards.
        await CreateQuizOkAsync(client, 5, "A1");
    }

    [Fact]
    public async Task FreeUser_WithoutLevel_GetsOnlyA1AndA2Words()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync();
        using var client = fixture.CreateClient(user);

        // 10 questions > 6 A2 words, so the service falls back from the adaptive level to the whole allowed pool.
        var session = await CreateQuizOkAsync(client, 10);

        var levels = await SessionLevelsAsync(session.Id);
        Assert.Equal(10, levels.Count);
        Assert.All(levels, level => Assert.Contains(level, new[] { "A1", "A2" }));
    }

    [Fact]
    public async Task ExpiredPremium_IsTreatedAsFree()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync(plan: SubscriptionPlan.Premium, planExpiresAtUtc: DateTime.UtcNow.AddDays(-1));
        using var client = fixture.CreateClient(user);

        using (var b1 = await CreateQuizAsync(client, 5, "B1"))
            await AssertStatusAsync(HttpStatusCode.Forbidden, b1);
        var session = await CreateQuizOkAsync(client, 10);
        Assert.All(await SessionLevelsAsync(session.Id), level => Assert.Contains(level, new[] { "A1", "A2" }));
        using var second = await CreateQuizAsync(client, 5);
        await AssertStatusAsync(HttpStatusCode.TooManyRequests, second);
    }

    [Fact]
    public async Task ActivePremium_HasUnlimitedQuizzesAndHigherLevels()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync(plan: SubscriptionPlan.Premium, planExpiresAtUtc: DateTime.UtcNow.AddDays(30));
        using var client = fixture.CreateClient(user);

        for (var i = 0; i < 3; i++) await CreateQuizOkAsync(client, 5);
        var b1 = await CreateQuizOkAsync(client, 5, "b1");

        Assert.All(await SessionLevelsAsync(b1.Id), level => Assert.Equal("B1", level));
        Assert.Equal(4, await fixture.WithDbAsync(db => db.QuizSessions.CountAsync(x => x.UserId == user.Id, Ct)));
        Assert.Equal(0, await QuizzesStartedTodayAsync(user.Id));
    }
}

[Collection(Collections.QuizScarcity), Trait("Category", "Integration")]
public sealed class QuizScarcityTests(QuizScarcityFixture fixture)
{
    [Fact]
    public async Task CreateSession_NotEnoughDistinctDistractors_Returns400WithoutConsumingQuota()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync();
        using var client = fixture.CreateClient(user);

        // Five published words share one translation, so the translation question has no distractors.
        using var response = await CreateQuizAsync(client, 5);

        await AssertStatusAsync(HttpStatusCode.BadRequest, response);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var started = await fixture.WithDbAsync(db => db.DailyUsages.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.DateUtc == today).Select(x => x.QuizzesStarted).SingleOrDefaultAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, started);
        Assert.False(await fixture.WithDbAsync(db => db.QuizSessions.AnyAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken)));
    }
}
