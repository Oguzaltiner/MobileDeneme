using EnglishLearning.Application.Placement;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Placement;

public sealed class PlacementTestService(EnglishLearningDbContext db) : IPlacementTestService
{
    private static readonly string[] Levels = ["A1", "A2", "B1", "B2"];

    public async Task<PlacementTestDto?> StartAsync(Guid userId, StartPlacementTestRequest request, CancellationToken ct)
    {
        var words = await db.VocabularyWords.AsNoTracking()
            .Where(x => x.PublicationStatus == VocabularyPublicationStatus.Published)
            .OrderBy(x => Guid.NewGuid()).ToListAsync(ct);
        if (words.Count < 4) return null;

        await db.PlacementTestAttempts.Where(x => x.UserId == userId && x.Status == PlacementTestStatus.InProgress)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.Status, PlacementTestStatus.Abandoned), ct);

        var count = Math.Clamp(request.QuestionCount, 8, 20);
        var attempt = new PlacementTestAttempt { UserId = userId, QuestionCount = count };
        for (var i = 0; i < count; i++)
        {
            var word = words[i % words.Count];
            var type = i % 2 == 0 ? PlacementQuestionType.Translation : PlacementQuestionType.Definition;
            var correct = type == PlacementQuestionType.Translation ? word.Translation : word.Term;
            var candidates = words.Where(x => x.Id != word.Id)
                .Select(x => type == PlacementQuestionType.Translation ? x.Translation : x.Term)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(correct, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(_ => Guid.NewGuid()).Take(3).ToList();
            if (candidates.Count < 3) return null;
            candidates.Add(correct);
            var question = new PlacementTestQuestion
            {
                Attempt = attempt, VocabularyWordId = word.Id, Order = i + 1,
                Difficulty = LevelToBand(word.Level), Type = type
            };
            foreach (var option in candidates.OrderBy(_ => Guid.NewGuid()).Select((text, index) => new PlacementTestOption
            {
                Question = question, Key = ((char)('A' + index)).ToString(), Text = text, IsCorrect = text == correct
            })) question.Options.Add(option);
            attempt.Questions.Add(question);
        }
        db.PlacementTestAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, attempt.Id, ct);
    }

    public async Task<PlacementTestDto?> GetAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await Load(userId, attemptId).SingleOrDefaultAsync(ct);
        return attempt is null ? null : Map(attempt);
    }

    public async Task<PlacementAnswerResult?> AnswerAsync(Guid userId, Guid attemptId, Guid questionId, PlacementAnswerRequest request, CancellationToken ct)
    {
        var attempt = await Load(userId, attemptId).SingleOrDefaultAsync(ct);
        var question = attempt?.Questions.SingleOrDefault(x => x.Id == questionId);
        if (attempt is null || question is null || attempt.Status != PlacementTestStatus.InProgress || question.Answered) return null;
        var selected = question.Options.SingleOrDefault(x => x.Key.Equals(request.OptionKey, StringComparison.OrdinalIgnoreCase));
        if (selected is null) return null;
        question.Answered = true;
        question.IsCorrect = selected.IsCorrect;
        attempt.AnsweredCount = attempt.Questions.Count(x => x.Answered);
        attempt.CorrectCount = attempt.Questions.Count(x => x.Answered && x.IsCorrect);
        await db.SaveChangesAsync(ct);
        return new(question.Id, selected.IsCorrect, question.Options.Single(x => x.IsCorrect).Key, attempt.CorrectCount, attempt.AnsweredCount);
    }

    public async Task<PlacementResultDto?> CompleteAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await Load(userId, attemptId).SingleOrDefaultAsync(ct);
        if (attempt is null) return null;
        attempt.AnsweredCount = attempt.Questions.Count(x => x.Answered);
        attempt.CorrectCount = attempt.Questions.Count(x => x.Answered && x.IsCorrect);
        attempt.ScorePercent = attempt.QuestionCount == 0 ? 0 : (int)Math.Round(attempt.CorrectCount * 100d / attempt.QuestionCount);
        if (attempt.Status == PlacementTestStatus.InProgress)
        {
            attempt.EstimatedLevel = EstimateLevel(attempt.Questions);
            attempt.Status = PlacementTestStatus.Completed;
            attempt.CompletedAtUtc = DateTime.UtcNow;
            var settings = await db.UserSettings.SingleOrDefaultAsync(x => x.UserId == userId, ct);
            if (settings is not null) settings.CurrentLevel = attempt.EstimatedLevel;
            await db.SaveChangesAsync(ct);
        }
        return new(attempt.Id, attempt.Status, attempt.QuestionCount, attempt.AnsweredCount, attempt.CorrectCount,
            attempt.ScorePercent ?? 0, attempt.EstimatedLevel ?? "A1");
    }

    private IQueryable<PlacementTestAttempt> Load(Guid userId, Guid attemptId) => db.PlacementTestAttempts
        .Include(x => x.Questions).ThenInclude(x => x.Options)
        .Include(x => x.Questions).ThenInclude(x => x.VocabularyWord)
        .Where(x => x.UserId == userId && x.Id == attemptId);

    private static PlacementTestDto Map(PlacementTestAttempt attempt) => new(
        attempt.Id, attempt.Status, attempt.QuestionCount, attempt.Questions.Count(x => x.Answered),
        attempt.Questions.Count(x => x.Answered && x.IsCorrect), attempt.EstimatedLevel,
        attempt.Questions.OrderBy(x => x.Order).Select(x => new PlacementTestQuestionDto(
            x.Id, x.Order, x.Difficulty, x.Type,
            x.Type == PlacementQuestionType.Translation ? x.VocabularyWord.Term : x.VocabularyWord.Definition,
            x.Options.OrderBy(o => o.Key).Select(o => new PlacementTestOptionDto(o.Key, o.Text)).ToList(),
            x.Answered, x.Answered ? x.IsCorrect : null)).ToList());

    private static int LevelToBand(string? level) => level?.ToUpperInvariant() switch
    {
        "A1" => 1, "A2" => 2, "B1" => 3, "B2" => 4, "C1" => 4, "C2" => 4, _ => 2
    };

    private static string EstimateLevel(IEnumerable<PlacementTestQuestion> questions)
    {
        var answered = questions.Where(x => x.Answered).ToList();
        if (answered.Count == 0) return "A1";
        var band = Enumerable.Range(1, 4)
            .Where(level => answered.Where(x => x.Difficulty == level).ToList() is { Count: > 0 } group && group.Count(x => x.IsCorrect) / (double)group.Count >= 0.60)
            .DefaultIfEmpty(1).Max();
        return Levels[band - 1];
    }
}
