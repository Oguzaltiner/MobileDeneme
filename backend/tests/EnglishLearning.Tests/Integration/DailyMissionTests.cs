using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLearning.Application.Missions;
using EnglishLearning.Application.Reviews;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Leaderboard;
using EnglishLearning.Infrastructure.Missions;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

/// <summary>Daily mission contract (docs/DAILY_MISSION.md) against the real API and PostgreSQL.</summary>
[Collection(Collections.Mission), Trait("Category", "Integration")]
public sealed class DailyMissionTests(MissionFixture fixture)
{
    private const string Today = "/api/v1/missions/today";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(TestUser User, HttpClient Client)> UserAsync(SubscriptionPlan? plan = SubscriptionPlan.Premium)
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync(plan: plan);
        return (user, fixture.CreateClient(user));
    }

    private static Task<HttpResponseMessage> StartAsync(HttpClient client, string? timeZone = "Europe/Istanbul", int? offset = 180) =>
        client.PostAsJsonAsync(Today, new StartMissionRequest(timeZone, offset), Ct);

    private static async Task<DailyMissionDto> StartOkAsync(HttpClient client, string? timeZone = "Europe/Istanbul", int? offset = 180)
    {
        using var response = await StartAsync(client, timeZone, offset);
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<DailyMissionDto>(Json, Ct))!;
    }

    private static async Task<MissionTodayDto> TodayOkAsync(HttpClient client, string query = "?timeZone=Europe/Istanbul&utcOffsetMinutes=180")
    {
        using var response = await client.GetAsync(Today + query, Ct);
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<MissionTodayDto>(Json, Ct))!;
    }

    private static Task<HttpResponseMessage> MissionAnswerAsync(HttpClient client, Guid missionId, string stepKey, Guid questionId, string optionKey) =>
        client.PostAsJsonAsync($"/api/v1/missions/{missionId}/answers", new MissionAnswerRequest(stepKey, questionId, optionKey), Ct);

    private static Task<HttpResponseMessage> CompleteAsync(HttpClient client, Guid missionId) =>
        client.PostAsync($"/api/v1/missions/{missionId}/complete", null, Ct);

    private static Task<HttpResponseMessage> ReviewAsync(HttpClient client, Guid wordId, Guid? sessionId, string? stepKey, string? clientEventId = null) =>
        client.PostAsJsonAsync("/api/v1/reviews", new SubmitReviewRequest(wordId, ReviewRating.Good, clientEventId ?? Guid.NewGuid().ToString(), sessionId, stepKey), Ct);

    /// <summary>Reviews every mission word and answers every question with "A"; returns the number of correct answers.</summary>
    private static async Task<int> FinishStepsAsync(HttpClient client, DailyMissionDto mission)
    {
        foreach (var (words, key) in new[] { (mission.ReviewWords, "review"), (mission.NewWords, "new-words") })
            foreach (var word in words)
            {
                using var review = await ReviewAsync(client, word.Id, mission.PracticeSessionId, key);
                await AssertStatusAsync(HttpStatusCode.OK, review);
            }
        var correct = 0;
        var questions = mission.RecallQuestions.Select(q => ("recall", q.Id)).ToList();
        if (mission.Listening is not null) questions.Add(("listening", mission.Listening.Id));
        foreach (var (step, id) in questions)
        {
            using var answer = await MissionAnswerAsync(client, mission.Id, step, id, "A");
            await AssertStatusAsync(HttpStatusCode.OK, answer);
            if ((await answer.Content.ReadFromJsonAsync<MissionAnswerResultDto>(Json, Ct))!.IsCorrect) correct++;
        }
        return correct;
    }

    private Task SetUsageAsync(Guid userId, int wordsUsed) => fixture.WithDbAsync(async db =>
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (await db.DailyUsages.AnyAsync(x => x.UserId == userId && x.DateUtc == today, Ct))
            await db.DailyUsages.Where(x => x.UserId == userId && x.DateUtc == today).ExecuteUpdateAsync(s => s.SetProperty(x => x.WordsUsed, wordsUsed), Ct);
        else
        {
            db.DailyUsages.Add(new DailyUsage { UserId = userId, DateUtc = today, WordsUsed = wordsUsed });
            await db.SaveChangesAsync(Ct);
        }
    });

    private Task<UserSettings> SettingsAsync(Guid userId) => fixture.WithDbAsync(db => db.UserSettings.AsNoTracking().SingleAsync(x => x.UserId == userId, Ct));

    [Fact]
    public async Task Start_ParallelRequests_CreateExactlyOneMission()
    {
        var (user, client) = await UserAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 6).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            using var response = await StartAsync(client);
            return (response.StatusCode, Body: await response.Content.ReadAsStringAsync(Ct));
        }, Ct)).ToList();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.StatusCode == HttpStatusCode.OK, r.Body));
        var ids = results.Select(r => JsonSerializer.Deserialize<DailyMissionDto>(r.Body, Json)!.Id).Distinct().ToList();
        Assert.Single(ids);
        Assert.Equal(1, await fixture.WithDbAsync(db => db.DailyMissions.CountAsync(x => x.UserId == user.Id, Ct)));
        Assert.Equal(1, await fixture.WithDbAsync(db => db.PracticeSessions.CountAsync(x => x.UserId == user.Id && x.PathKey == DailyMission.PracticePathKey, Ct)));
        Assert.Equal(ids[0], (await StartOkAsync(client)).Id);
    }

    [Fact]
    public async Task Start_PinsTimeZone_AndReplacesItOnlyAfterSevenDays()
    {
        var (user, client) = await UserAsync();
        await StartOkAsync(client, "Europe/Istanbul", 180);
        Assert.Equal("Europe/Istanbul", (await SettingsAsync(user.Id)).TimeZone);

        await StartOkAsync(client, "America/New_York", -240);
        Assert.Equal("Europe/Istanbul", (await SettingsAsync(user.Id)).TimeZone);

        await fixture.WithDbAsync(db => db.UserSettings.Where(x => x.UserId == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TimeZoneUpdatedAtUtc, DateTime.UtcNow.AddDays(-8)), Ct));
        await StartOkAsync(client, "America/New_York", -240);
        var settings = await SettingsAsync(user.Id);
        Assert.Equal("America/New_York", settings.TimeZone);
        Assert.True(settings.TimeZoneUpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Start_UnknownZone_FallsBackToOffsetOrStoredZone_And400OnlyWhenNothingIsUsable()
    {
        var (user, client) = await UserAsync();
        using (var invalid = await StartAsync(client, "Mars/Olympus", null))
            await AssertStatusAsync(HttpStatusCode.BadRequest, invalid);
        using (var outOfRange = await StartAsync(client, null, 5000))
            await AssertStatusAsync(HttpStatusCode.BadRequest, outOfRange);
        using (var getInvalid = await client.GetAsync(Today + "?timeZone=Mars/Olympus", Ct))
            await AssertStatusAsync(HttpStatusCode.BadRequest, getInvalid);
        Assert.Null((await SettingsAsync(user.Id)).TimeZone);

        await StartOkAsync(client, "Mars/Olympus", 180);
        Assert.Equal("UTC+03:00", (await SettingsAsync(user.Id)).TimeZone);

        // An unusable request now falls back to the pinned zone instead of failing.
        await StartOkAsync(client, "Mars/Olympus", null);
        await TodayOkAsync(client, "?timeZone=Not/AZone");
        Assert.Equal("UTC+03:00", (await SettingsAsync(user.Id)).TimeZone);
    }

    [Fact]
    public async Task Mission_BeforeAnswering_ExposesNoAnswerKeysTranslationsOrWordIds()
    {
        var (_, client) = await UserAsync();
        using var created = await StartAsync(client);
        await AssertStatusAsync(HttpStatusCode.OK, created);
        using var fetched = await client.GetAsync(Today + "?timeZone=Europe/Istanbul", Ct);
        await AssertStatusAsync(HttpStatusCode.OK, fetched);
        var terms = fixture.Pool.Select(w => w.Term).ToHashSet();

        foreach (var mission in new[] { await ReadJsonAsync(created), (await ReadJsonAsync(fetched)).GetProperty("mission") })
        {
            var recall = mission.GetProperty("recallQuestions").EnumerateArray().ToList();
            Assert.Equal(DailyMissionService.RecallQuestionCount, recall.Count);
            foreach (var question in recall)
            {
                Assert.Equal(new[] { "answered", "id", "isCorrect", "options", "prompt", "speakText" }, question.EnumerateObject().Select(p => p.Name).Order());
                foreach (var option in question.GetProperty("options").EnumerateArray())
                    Assert.Equal(new[] { "key", "text" }, option.EnumerateObject().Select(p => p.Name).Order());
                var prompt = question.GetProperty("prompt").GetString()!;
                var speak = question.GetProperty("speakText");
                // speakText is the English term only when the prompt shows it; never the answer.
                if (terms.Contains(prompt)) Assert.Equal(prompt, speak.GetString());
                else Assert.Equal(JsonValueKind.Null, speak.ValueKind);
            }
            var listening = mission.GetProperty("listening");
            Assert.Equal(new[] { "answered", "id", "isCorrect", "level", "options", "prompt", "title", "transcript" }, listening.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(JsonValueKind.Null, mission.GetProperty("result").ValueKind);
        }
    }

    [Fact]
    public async Task Answer_FirstAnswerWins_ReplayReturnsStoredResult_AndRevealsWordAfterwards()
    {
        var (_, client) = await UserAsync();
        var mission = await StartOkAsync(client);
        var question = mission.RecallQuestions[0];

        using var first = await MissionAnswerAsync(client, mission.Id, "recall", question.Id, "A");
        await AssertStatusAsync(HttpStatusCode.OK, first);
        var firstResult = (await first.Content.ReadFromJsonAsync<MissionAnswerResultDto>(Json, Ct))!;
        var otherKey = firstResult.CorrectOptionKey == "A" ? "B" : firstResult.CorrectOptionKey;
        using var replay = await MissionAnswerAsync(client, mission.Id, "RECALL", question.Id, otherKey);
        await AssertStatusAsync(HttpStatusCode.OK, replay);
        var replayResult = (await replay.Content.ReadFromJsonAsync<MissionAnswerResultDto>(Json, Ct))!;

        Assert.Equal(firstResult.IsCorrect, replayResult.IsCorrect);
        Assert.Equal(firstResult.CorrectOptionKey, replayResult.CorrectOptionKey);
        Assert.Equal(1, firstResult.Step.Done);
        Assert.Equal(1, replayResult.Step.Done);
        var word = fixture.Pool.Single(w => w.Id == firstResult.WordId);
        Assert.Equal(word.Term, firstResult.Term);
        Assert.Equal(word.Translation, firstResult.Translation);
        var eventId = DailyMissionService.AnswerEventId(mission.Id, question.Id);
        Assert.Equal(1, await fixture.WithDbAsync(db => db.PracticeEvents.CountAsync(x => x.ClientEventId == eventId, Ct)));

        using var listening = await MissionAnswerAsync(client, mission.Id, "listening", mission.Listening!.Id, "A");
        await AssertStatusAsync(HttpStatusCode.OK, listening);
        var listeningJson = await ReadJsonAsync(listening);
        Assert.Equal(JsonValueKind.Null, listeningJson.GetProperty("wordId").ValueKind);
        Assert.Equal(JsonValueKind.Null, listeningJson.GetProperty("term").ValueKind);
        Assert.Equal(JsonValueKind.Null, listeningJson.GetProperty("translation").ValueKind);

        using (var unknownStep = await MissionAnswerAsync(client, mission.Id, "grammar", question.Id, "A"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, unknownStep);
        using (var unknownQuestion = await MissionAnswerAsync(client, mission.Id, "recall", Guid.NewGuid(), "A"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, unknownQuestion);
        using (var invalidOption = await MissionAnswerAsync(client, mission.Id, "recall", mission.RecallQuestions[1].Id, "Z"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, invalidOption);
        var (_, other) = await UserAsync();
        using (var foreign = await MissionAnswerAsync(other, mission.Id, "recall", mission.RecallQuestions[1].Id, "A"))
            await AssertStatusAsync(HttpStatusCode.NotFound, foreign);
        using (var foreignComplete = await CompleteAsync(other, mission.Id))
            await AssertStatusAsync(HttpStatusCode.NotFound, foreignComplete);
    }

    [Fact]
    public async Task Complete_WithIncompleteSteps_Returns409WithStepKeys()
    {
        var (_, client) = await UserAsync();
        var mission = await StartOkAsync(client);
        Assert.Equal(DailyMissionService.MaxNewWords, mission.NewWords.Count);

        using var response = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.Conflict, response);
        var steps = (await ReadJsonAsync(response)).GetProperty("incompleteSteps").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(new[] { "listening", "new-words", "recall" }, steps.Order());
    }

    [Fact]
    public async Task Complete_ParallelCalls_AwardXpOnce_AndReplayIsStable()
    {
        var (user, client) = await UserAsync();
        var mission = await StartOkAsync(client);
        var correct = await FinishStepsAsync(client, mission);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 6).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            using var response = await CompleteAsync(client, mission.Id);
            return (response.StatusCode, Body: await response.Content.ReadAsStringAsync(Ct));
        }, Ct)).ToList();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.StatusCode == HttpStatusCode.OK, r.Body));
        var parsed = results.Select(r => JsonSerializer.Deserialize<MissionResultDto>(r.Body, Json)!).ToList();
        var fresh = Assert.Single(parsed, r => !r.AlreadyCompleted);
        var expectedXp = MissionXpRules.Base + mission.NewWords.Count * MissionXpRules.PerNewWord + mission.ReviewWords.Count * MissionXpRules.PerReview + correct * MissionXpRules.PerCorrectAnswer;
        Assert.Equal(expectedXp, fresh.XpAwarded);
        Assert.All(parsed, r => Assert.Equal(expectedXp, r.XpAwarded));
        Assert.Equal(new MissionResultStreakDto(1, 1, false), fresh.Streak);
        Assert.Equal(correct, fresh.CorrectAnswers);
        Assert.Equal(DailyMissionService.RecallQuestionCount + 1, fresh.TotalAnswers);
        var stored = await fixture.WithDbAsync(db => db.DailyMissions.AsNoTracking().SingleAsync(x => x.Id == mission.Id, Ct));
        Assert.Equal(DailyMissionStatus.Completed, stored.Status);
        Assert.Equal(expectedXp, stored.XpAwarded);
        Assert.Equal(PracticeSessionStatus.Completed, await fixture.WithDbAsync(db => db.PracticeSessions.Where(x => x.Id == mission.PracticeSessionId).Select(x => x.Status).SingleAsync(Ct)));

        using var replay = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.OK, replay);
        var replayResult = (await replay.Content.ReadFromJsonAsync<MissionResultDto>(Json, Ct))!;
        Assert.True(replayResult.AlreadyCompleted);
        Assert.Equal(fresh.XpBreakdown, replayResult.XpBreakdown);
        Assert.Equal(fresh.Streak, replayResult.Streak);

        using var lateAnswer = await MissionAnswerAsync(client, mission.Id, "recall", mission.RecallQuestions[0].Id, "A");
        await AssertStatusAsync(HttpStatusCode.Conflict, lateAnswer);
        var today = await TodayOkAsync(client);
        Assert.Equal("completed", today.Status);
        Assert.True(today.Streak.CompletedToday);
        Assert.True(today.Mission!.Result!.AlreadyCompleted);
        Assert.Equal(user.Id, stored.UserId);
    }

    [Fact]
    public async Task Complete_ThirdConsecutiveDay_AddsStreakBonusAndExtendsStreak()
    {
        var (user, client) = await UserAsync();
        var mission = await StartOkAsync(client);
        await fixture.WithDbAsync(async db =>
        {
            foreach (var date in new[] { mission.MissionDate.AddDays(-1), mission.MissionDate.AddDays(-2) })
            {
                var session = new PracticeSession { UserId = user.Id, PathKey = DailyMission.PracticePathKey, Status = PracticeSessionStatus.Completed };
                db.DailyMissions.Add(new DailyMission
                {
                    UserId = user.Id, PracticeSessionId = session.Id, PracticeSession = session, MissionDate = date, TimeZone = "UTC",
                    Status = DailyMissionStatus.Completed, CompletedAtUtc = DateTime.UtcNow.AddDays(-1), XpAwarded = MissionXpRules.Base,
                    PayloadJson = """{"reviewWordIds":[],"newWordIds":[],"recall":[],"listening":null}"""
                });
            }
            await db.SaveChangesAsync(Ct);
        });
        var correct = await FinishStepsAsync(client, mission);

        using var response = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.OK, response);
        var result = (await response.Content.ReadFromJsonAsync<MissionResultDto>(Json, Ct))!;

        Assert.Equal(new MissionResultStreakDto(3, 3, true), result.Streak);
        Assert.Equal(MissionXpRules.StreakBonus, result.XpBreakdown.StreakBonus);
        Assert.Equal(MissionXpRules.Base + mission.NewWords.Count * MissionXpRules.PerNewWord + mission.ReviewWords.Count * MissionXpRules.PerReview
                     + correct * MissionXpRules.PerCorrectAnswer + MissionXpRules.StreakBonus, result.XpAwarded);
    }

    [Fact]
    public async Task FreeUser_QuotaRunsOutMidMission_WordStepsStayCompletedAfterQuotaReset()
    {
        var (user, client) = await UserAsync(plan: null);
        var mission = await StartOkAsync(client);
        Assert.Equal(DailyMissionService.MaxNewWords, mission.NewWords.Count);
        Assert.False(mission.Steps.Single(x => x.Key == "new-words").Completed);

        await SetUsageAsync(user.Id, 20);
        var capped = (await TodayOkAsync(client)).Mission!;
        Assert.True(capped.Steps.Single(x => x.Key == "new-words").Completed);
        Assert.True(capped.Steps.Single(x => x.Key == "review").Completed);
        Assert.True(await fixture.WithDbAsync(db => db.DailyMissions.Where(x => x.Id == mission.Id).Select(x => x.WordStepsCapped).SingleAsync(Ct)));

        // The UTC quota resets; the persisted capped state keeps the word steps completed.
        await SetUsageAsync(user.Id, 0);
        var afterReset = (await TodayOkAsync(client)).Mission!;
        Assert.True(afterReset.Steps.Single(x => x.Key == "new-words").Completed);
        using var incomplete = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.Conflict, incomplete);
        var steps = (await ReadJsonAsync(incomplete)).GetProperty("incompleteSteps").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.DoesNotContain("new-words", steps);
        Assert.DoesNotContain("review", steps);
    }

    [Fact]
    public async Task FreeUser_NoQuotaLeft_MissionHasNoWordStepsWork_AndDoesNotConsumeQuizQuota()
    {
        var (user, client) = await UserAsync(plan: null);
        await SetUsageAsync(user.Id, 20);
        var today = await TodayOkAsync(client);
        Assert.Equal(0, today.Limits.DailyWordsRemaining);
        Assert.Equal(0, today.Preview.NewWordCount);

        var mission = await StartOkAsync(client);
        Assert.Empty(mission.NewWords);
        Assert.Empty(mission.ReviewWords);
        Assert.All(mission.Steps.Where(x => x.Key is "review" or "new-words"), s => Assert.True(s.Completed && s.Required == 0));
        Assert.Equal(DailyMissionService.RecallQuestionCount, mission.RecallQuestions.Count);
        var usage = await fixture.WithDbAsync(db => db.DailyUsages.AsNoTracking().SingleAsync(x => x.UserId == user.Id && x.DateUtc == DateOnly.FromDateTime(DateTime.UtcNow), Ct));
        Assert.Equal(0, usage.QuizzesStarted);
    }

    [Fact]
    public async Task Today_ReturnsPendingMissionFromPreviousDay_OnlyWhileItsWindowIsOpen()
    {
        var (user, client) = await UserAsync();
        // Pinned at UTC+14 the local day T always satisfies now < T 10:00 UTC; a mission dated T-1 in
        // UTC-12 closes at T 12:00 + 6h UTC, so it is reliably still open.
        await fixture.WithDbAsync(db => db.UserSettings.Where(x => x.UserId == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TimeZone, "UTC+14:00").SetProperty(x => x.TimeZoneUpdatedAtUtc, DateTime.UtcNow), Ct));
        var mission = await StartOkAsync(client);
        var day = mission.MissionDate;
        await fixture.WithDbAsync(db => db.DailyMissions.Where(x => x.Id == mission.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MissionDate, day.AddDays(-1)).SetProperty(x => x.TimeZone, "UTC-12:00"), Ct));

        var today = await TodayOkAsync(client);
        Assert.Equal(day, today.MissionDate);
        Assert.Equal("notStarted", today.Status);
        Assert.Null(today.Mission);
        Assert.Equal(mission.Id, today.PendingMission?.Id);
        using (var stillOpen = await CompleteAsync(client, mission.Id))
        {
            await AssertStatusAsync(HttpStatusCode.Conflict, stillOpen);
            Assert.NotEmpty((await ReadJsonAsync(stillOpen)).GetProperty("incompleteSteps").EnumerateArray());
        }

        await fixture.WithDbAsync(db => db.DailyMissions.Where(x => x.Id == mission.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MissionDate, day.AddDays(-3)), Ct));
        Assert.Null((await TodayOkAsync(client)).PendingMission);
        using var expired = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.Conflict, expired);
        Assert.Empty((await ReadJsonAsync(expired)).GetProperty("incompleteSteps").EnumerateArray());
    }

    [Fact]
    public async Task Leaderboard_IncludesMissionXp_AndNeverShowsEmails()
    {
        var (user, client) = await UserAsync();
        await fixture.WithDbAsync(db => db.Users.Where(x => x.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DisplayName, (string?)null), Ct));
        var mission = await StartOkAsync(client);
        await FinishStepsAsync(client, mission);
        using var completed = await CompleteAsync(client, mission.Id);
        await AssertStatusAsync(HttpStatusCode.OK, completed);
        var xp = (await completed.Content.ReadFromJsonAsync<MissionResultDto>(Json, Ct))!.XpAwarded;

        using var response = await client.GetAsync("/api/v1/leaderboard/weekly", Ct);
        await AssertStatusAsync(HttpStatusCode.OK, response);
        var json = await ReadJsonAsync(response);
        var names = json.GetProperty("entries").EnumerateArray().Select(x => x.GetProperty("displayName").GetString()!).ToList();
        Assert.DoesNotContain(names, n => n.Contains('@'));
        var mine = json.GetProperty("entries").EnumerateArray().Single(x => x.GetProperty("isCurrentUser").GetBoolean());
        Assert.Equal(LeaderboardService.PublicName(user.Id, null), mine.GetProperty("displayName").GetString());
        Assert.StartsWith("Öğrenci #", mine.GetProperty("displayName").GetString());
        Assert.Equal(xp, json.GetProperty("xpBreakdown").GetProperty("missionXp").GetInt32());
        Assert.Equal(1, json.GetProperty("xpBreakdown").GetProperty("missionCount").GetInt32());
    }

    [Fact]
    public async Task Review_ConcurrentDuplicateSubmit_ReturnsStoredResultAndConsumesQuotaOnce()
    {
        var (user, client) = await UserAsync(plan: null);
        var word = fixture.Pool[0];
        var clientEventId = Guid.NewGuid().ToString();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 6).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            using var response = await ReviewAsync(client, word.Id, null, null, clientEventId);
            return (response.StatusCode, Body: await response.Content.ReadAsStringAsync(Ct));
        }, Ct)).ToList();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {r.Body}"));
        Assert.Equal(1, await fixture.WithDbAsync(db => db.ReviewEvents.CountAsync(x => x.UserId == user.Id && x.ClientEventId == clientEventId, Ct)));
        var usage = await fixture.WithDbAsync(db => db.DailyUsages.AsNoTracking().SingleAsync(x => x.UserId == user.Id && x.DateUtc == DateOnly.FromDateTime(DateTime.UtcNow), Ct));
        Assert.Equal(1, usage.WordsUsed);
    }

    [Fact]
    public async Task Review_MissionSession_ValidatesStepKeysCaseInsensitively()
    {
        var (user, client) = await UserAsync();
        var mission = await StartOkAsync(client);
        var word = mission.NewWords[0];

        using (var upper = await ReviewAsync(client, word.Id, mission.PracticeSessionId, "NEW-WORDS"))
            await AssertStatusAsync(HttpStatusCode.OK, upper);
        Assert.Equal("new-words", await fixture.WithDbAsync(db => db.PracticeEvents.Where(x => x.SessionId == mission.PracticeSessionId && x.VocabularyWordId == word.Id).Select(x => x.StepKey).SingleAsync(Ct)));
        using (var recall = await ReviewAsync(client, word.Id, mission.PracticeSessionId, "recall"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, recall);
        using (var tooLong = await ReviewAsync(client, word.Id, mission.PracticeSessionId, new string('x', 80)))
            await AssertStatusAsync(HttpStatusCode.BadRequest, tooLong);
        using (var reserved = await ReviewAsync(client, word.Id, mission.PracticeSessionId, "review", $"mission:{mission.Id:N}:{Guid.NewGuid():N}"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, reserved);
        using var sync = await client.PostAsJsonAsync("/api/v1/sync/practice-events", new
        {
            events = new[] { new { clientEventId = Guid.NewGuid().ToString(), sessionId = mission.PracticeSessionId, stepKey = "review", vocabularyWordId = word.Id, isCorrect = true } }
        }, Ct);
        await AssertStatusAsync(HttpStatusCode.BadRequest, sync);
        Assert.Equal(1, await fixture.WithDbAsync(db => db.PracticeEvents.CountAsync(x => x.UserId == user.Id && x.SessionId == mission.PracticeSessionId, Ct)));
    }
}
