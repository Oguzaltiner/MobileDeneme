using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLearning.Application.Quiz;
using EnglishLearning.Domain;
using EnglishLearning.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static EnglishLearning.Tests.Integration.Infrastructure.TestData;

namespace EnglishLearning.Tests.Integration;

[Collection(Collections.Quiz), Trait("Category", "Integration")]
public sealed class QuizIntegrityTests(QuizFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, QuizSessionDto Session)> PremiumSessionAsync(int count = 10)
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync(plan: SubscriptionPlan.Premium);
        var client = fixture.CreateClient(user);
        return (client, await CreateQuizOkAsync(client, count));
    }

    /// <summary>Correct option text per question id, read from the database (never exposed by the API).</summary>
    private Task<Dictionary<Guid, string>> CorrectTextsAsync(Guid sessionId) => fixture.WithDbAsync(db => db.QuizOptions.AsNoTracking()
        .Where(x => x.Question.SessionId == sessionId && x.IsCorrect).ToDictionaryAsync(x => x.QuestionId, x => x.Text, Ct));

    private Task<Dictionary<Guid, string>> CorrectKeysAsync(Guid sessionId) => fixture.WithDbAsync(db => db.QuizOptions.AsNoTracking()
        .Where(x => x.Question.SessionId == sessionId && x.IsCorrect).ToDictionaryAsync(x => x.QuestionId, x => x.Key, Ct));

    [Fact]
    public async Task CreateSession_TenQuestions_EachHasFourDistinctOptionsAndExactlyOneCorrect()
    {
        var (_, session) = await PremiumSessionAsync(10);
        var correct = await CorrectTextsAsync(session.Id);
        var correctCounts = await fixture.WithDbAsync(db => db.QuizOptions.AsNoTracking()
            .Where(x => x.Question.SessionId == session.Id && x.IsCorrect).GroupBy(x => x.QuestionId).Select(g => g.Count()).ToListAsync(Ct));

        Assert.Equal(10, session.QuestionCount);
        Assert.Equal(10, session.Questions.Count);
        Assert.All(correctCounts, c => Assert.Equal(1, c));
        Assert.Equal(10, correctCounts.Count);
        Assert.All(session.Questions, q =>
        {
            Assert.Equal(4, q.Options.Count);
            Assert.Equal(4, q.Options.Select(o => o.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(new[] { "A", "B", "C", "D" }, q.Options.Select(o => o.Key).Order());
            // Exactly one option carries the answer text: no distractor is another spelling of it.
            Assert.Single(q.Options, o => o.Text.Equals(correct[q.Id], StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task Session_OptionsExposeOnlyKeyAndText_AndUnansweredQuestionsHideGrading()
    {
        fixture.SkipIfUnavailable();
        var user = await fixture.CreateUserAsync(plan: SubscriptionPlan.Premium);
        using var client = fixture.CreateClient(user);
        using var created = await CreateQuizAsync(client, 5);
        await AssertStatusAsync(HttpStatusCode.OK, created);
        var createdJson = await ReadJsonAsync(created);
        using var fetched = await client.GetAsync($"/api/v1/quizzes/sessions/{createdJson.GetProperty("id").GetGuid()}", Ct);
        await AssertStatusAsync(HttpStatusCode.OK, fetched);
        var fetchedJson = await ReadJsonAsync(fetched);

        foreach (var json in new[] { createdJson, fetchedJson })
        {
            foreach (var question in json.GetProperty("questions").EnumerateArray())
            {
                Assert.Equal(JsonValueKind.Null, question.GetProperty("isCorrect").ValueKind);
                Assert.Equal(JsonValueKind.Null, question.GetProperty("explanation").ValueKind);
                foreach (var option in question.GetProperty("options").EnumerateArray())
                    Assert.Equal(new[] { "key", "text" }, option.EnumerateObject().Select(p => p.Name).Order());
            }
        }
    }

    [Fact]
    public async Task Prompts_NeverContainTheAnswerAsWholeWord_ExceptListening()
    {
        var (_, session) = await PremiumSessionAsync(10);
        var correct = await CorrectTextsAsync(session.Id);

        Assert.Contains(session.Questions, q => q.Type == QuizQuestionType.Listening);
        foreach (var question in session.Questions.Where(q => q.Type != QuizQuestionType.Listening))
            Assert.False(VocabularyQuality.ContainsWholeWord(question.Prompt, correct[question.Id]),
                $"Question {question.Order} ({question.Type}) prompt '{question.Prompt}' reveals answer '{correct[question.Id]}'.");
    }

    [Fact]
    public async Task Distractors_PreferTheAnswerWordsLevel_WhenThreeSameLevelWordsExist()
    {
        var (_, session) = await PremiumSessionAsync(10);
        var correct = await CorrectTextsAsync(session.Id);
        var levelByText = fixture.Pool.SelectMany(w => new[] { (w.Term, w.Level), (w.Translation, w.Level) })
            .ToDictionary(x => x.Item1, x => x.Level, StringComparer.OrdinalIgnoreCase);

        // Every pool level has WordsPerLevel (6) words, so 5 same-level candidates always exist.
        foreach (var question in session.Questions)
        {
            var answerLevel = levelByText[correct[question.Id]];
            var distractorLevels = question.Options.Where(o => o.Text != correct[question.Id]).Select(o => levelByText[o.Text]).ToList();
            Assert.All(distractorLevels, level => Assert.Equal(answerLevel, level));
        }
    }

    [Fact]
    public async Task Answer_SameQuestionTwice_SecondIsRejected()
    {
        var (client, session) = await PremiumSessionAsync(5);
        var question = session.Questions[0];

        using var first = await AnswerAsync(client, session.Id, question.Id, "A");
        await AssertStatusAsync(HttpStatusCode.OK, first);
        using var second = await AnswerAsync(client, session.Id, question.Id, "B");
        await AssertStatusAsync(HttpStatusCode.BadRequest, second);

        var events = await fixture.WithDbAsync(db => db.ReviewEvents.CountAsync(x => x.ClientEventId == $"quiz:{session.Id}:{question.Id}", Ct));
        Assert.Equal(1, events);
        var fetched = await client.GetFromJsonAsync<QuizSessionDto>($"/api/v1/quizzes/sessions/{session.Id}", Json, Ct);
        Assert.Equal(1, fetched!.AnsweredCount);
    }

    [Fact]
    public async Task Answer_ConcurrentSubmitsForSameQuestion_ExactlyOneAcceptedAndNoServerError()
    {
        var (client, first) = await PremiumSessionAsync(5);
        var sessions = new List<QuizSessionDto> { first, await CreateQuizOkAsync(client, 5), await CreateQuizOkAsync(client, 5) };
        const int parallel = 4;

        // A race is probabilistic, so every question of three sessions gets `parallel`
        // simultaneous submits (one per key), all released together.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = sessions.SelectMany(s => s.Questions.Select(q => (Session: s.Id, Question: q.Id)))
            .SelectMany(target => Enumerable.Range(0, parallel).Select(i => Task.Run(async () =>
            {
                await gate.Task;
                using var response = await AnswerAsync(client, target.Session, target.Question, ((char)('A' + i)).ToString());
                return (target.Question, response.StatusCode, Body: await response.Content.ReadAsStringAsync(Ct));
            }, Ct))).ToList();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        foreach (var (session, question) in sessions.SelectMany(s => s.Questions.Select(q => (s, q))))
        {
            var statuses = results.Where(r => r.Question == question.Id).Select(r => r.StatusCode).ToList();
            var summary = string.Join(" | ", results.Where(r => r.Question == question.Id).Select(r => $"{(int)r.StatusCode}: {r.Body}"));
            Assert.False(statuses.Contains(HttpStatusCode.InternalServerError), $"Question {question.Order} produced a 500: {summary}");
            Assert.True(statuses.Count(s => s == HttpStatusCode.OK) == 1, $"Question {question.Order} expected exactly one accepted answer: {summary}");
            Assert.All(statuses.Where(s => s != HttpStatusCode.OK), s => Assert.Equal(HttpStatusCode.BadRequest, s));
            var events = await fixture.WithDbAsync(db => db.ReviewEvents.CountAsync(x => x.ClientEventId == $"quiz:{session.Id}:{question.Id}", Ct));
            Assert.Equal(1, events);
        }
    }

    [Fact]
    public async Task OtherUsersSession_CannotBeReadAnsweredOrCompleted()
    {
        var (owner, session) = await PremiumSessionAsync(5);
        var attacker = await fixture.CreateUserAsync(plan: SubscriptionPlan.Premium);
        using var attackerClient = fixture.CreateClient(attacker);
        var question = session.Questions[0];

        using (var get = await attackerClient.GetAsync($"/api/v1/quizzes/sessions/{session.Id}", Ct))
            await AssertStatusAsync(HttpStatusCode.NotFound, get);
        using (var answer = await AnswerAsync(attackerClient, session.Id, question.Id, "A"))
            await AssertStatusAsync(HttpStatusCode.BadRequest, answer);
        using (var complete = await attackerClient.PostAsync($"/api/v1/quizzes/sessions/{session.Id}/complete", null, Ct))
            await AssertStatusAsync(HttpStatusCode.NotFound, complete);

        var fetched = await owner.GetFromJsonAsync<QuizSessionDto>($"/api/v1/quizzes/sessions/{session.Id}", Json, Ct);
        Assert.Equal(QuizSessionStatus.InProgress, fetched!.Status);
        Assert.Equal(0, fetched.AnsweredCount);
        Assert.Equal(0, await fixture.WithDbAsync(db => db.ReviewEvents.CountAsync(x => x.UserId == attacker.Id, Ct)));
    }

    [Fact]
    public async Task Answer_IsGradedServerSide()
    {
        var (client, session) = await PremiumSessionAsync(5);
        var correctKeys = await CorrectKeysAsync(session.Id);
        var first = session.Questions[0];
        var second = session.Questions[1];
        var wrongKey = first.Options.Select(o => o.Key).First(k => k != correctKeys[first.Id]);

        using var wrong = await AnswerAsync(client, session.Id, first.Id, wrongKey);
        await AssertStatusAsync(HttpStatusCode.OK, wrong);
        var wrongResult = (await wrong.Content.ReadFromJsonAsync<QuizAnswerResult>(Json, Ct))!;
        Assert.False(wrongResult.IsCorrect);
        Assert.Equal(correctKeys[first.Id], wrongResult.CorrectOptionKey);
        Assert.Equal(0, wrongResult.CorrectCount);

        using var right = await AnswerAsync(client, session.Id, second.Id, correctKeys[second.Id].ToLowerInvariant());
        await AssertStatusAsync(HttpStatusCode.OK, right);
        var rightResult = (await right.Content.ReadFromJsonAsync<QuizAnswerResult>(Json, Ct))!;
        Assert.True(rightResult.IsCorrect);
        Assert.Equal(1, rightResult.CorrectCount);
        Assert.Equal(2, rightResult.AnsweredCount);

        using var complete = await client.PostAsync($"/api/v1/quizzes/sessions/{session.Id}/complete", null, Ct);
        await AssertStatusAsync(HttpStatusCode.OK, complete);
        var result = (await complete.Content.ReadFromJsonAsync<QuizResultDto>(Json, Ct))!;
        Assert.Equal(QuizSessionStatus.Completed, result.Status);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(20, result.ScorePercent);
    }

    [Fact]
    public async Task Answer_UnknownOptionKey_Returns400AndLeavesQuestionUnanswered()
    {
        var (client, session) = await PremiumSessionAsync(5);
        var question = session.Questions[0];

        using var response = await AnswerAsync(client, session.Id, question.Id, "Z");
        await AssertStatusAsync(HttpStatusCode.BadRequest, response);

        var fetched = await client.GetFromJsonAsync<QuizSessionDto>($"/api/v1/quizzes/sessions/{session.Id}", Json, Ct);
        Assert.False(fetched!.Questions.Single(q => q.Id == question.Id).Answered);
        Assert.Equal(0, fetched.AnsweredCount);
    }

    [Fact]
    public async Task CreateSession_WithoutToken_Returns401()
    {
        fixture.SkipIfUnavailable();
        using var client = fixture.CreateClient();
        using var response = await CreateQuizAsync(client, 5);
        await AssertStatusAsync(HttpStatusCode.Unauthorized, response);
    }
}
