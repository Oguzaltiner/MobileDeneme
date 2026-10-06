using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishLearning.Api.Controllers;
using EnglishLearning.Application.Quiz;
using EnglishLearning.Domain;

namespace EnglishLearning.Tests.Integration.Infrastructure;

public static class TestData
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Suffix() => Guid.NewGuid().ToString("N")[..10];

    /// <summary>
    /// A publishable word. The definition contains the term (so blanking is exercised) and the
    /// translation embeds it without a word boundary (so it never counts as a whole-word match).
    /// </summary>
    public static VocabularyWord Word(string term, string level = "A2",
        VocabularyPublicationStatus status = VocabularyPublicationStatus.Published, string? translation = null) => new()
    {
        Id = Guid.NewGuid(), Term = term, Pronunciation = "/test/", PartOfSpeech = "noun",
        Definition = $"a test meaning for {term} in context", Translation = translation ?? $"{term}_tr",
        Level = level, Category = "Testing", ExampleSentence = $"I use {term} every day.",
        PublicationStatus = status, PublishedAtUtc = status == VocabularyPublicationStatus.Published ? DateTime.UtcNow : null
    };

    public static VocabularyImportItem ImportItem(string term, string level = "A2", string? definition = null) =>
        new(term, "/test/", "noun", definition ?? $"an imported meaning for {term}", $"{term}_tr", level, "Testing", $"We import {term} today.");

    public static Task<HttpResponseMessage> CreateQuizAsync(HttpClient client, int questionCount = 5, string? level = null) =>
        client.PostAsJsonAsync("/api/v1/quizzes/sessions", new CreateQuizRequest(questionCount, level), TestContext.Current.CancellationToken);

    public static async Task<QuizSessionDto> CreateQuizOkAsync(HttpClient client, int questionCount = 5, string? level = null)
    {
        using var response = await CreateQuizAsync(client, questionCount, level);
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await response.Content.ReadFromJsonAsync<QuizSessionDto>(Json, TestContext.Current.CancellationToken))!;
    }

    public static Task<HttpResponseMessage> AnswerAsync(HttpClient client, Guid sessionId, Guid questionId, string key) =>
        client.PostAsJsonAsync($"/api/v1/quizzes/sessions/{sessionId}/questions/{questionId}/answers", new SubmitAnswerRequest(key), TestContext.Current.CancellationToken);

    /// <summary>Status assertion that prints the body, so a failing run shows the server's message.</summary>
    public static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        if (response.StatusCode == expected) return;
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Fail($"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Json, TestContext.Current.CancellationToken));
}
