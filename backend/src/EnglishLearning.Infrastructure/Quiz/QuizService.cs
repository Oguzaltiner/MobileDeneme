using EnglishLearning.Application.Quiz;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using EnglishLearning.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Quiz;

public sealed class QuizService(EnglishLearningDbContext db, IEntitlementService entitlements) : IQuizService
{
    public async Task<QuizSessionDto?> CreateAsync(Guid userId, CreateQuizRequest request, CancellationToken ct)
    {
        var count = request.QuestionCount is 10 ? 10 : 5;
        var level = string.IsNullOrWhiteSpace(request.Level) ? null : request.Level.Trim().ToUpperInvariant();
        if (!await entitlements.CanAccessLevelAsync(userId, level, ct)) throw new LevelLockedException("Bu seviye Premium üyelikte açılır.");
        var adaptiveDifficulty = request.Difficulty is >= 1 and <= 4 ? request.Difficulty.Value : await GetAdaptiveDifficultyAsync(userId, ct);
        var query = db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published).AsQueryable();
        var entitlement = await entitlements.GetAsync(userId, ct);
        if (!entitlement.IsPremium) query = query.Where(x => x.Level == "A1" || x.Level == "A2");
        if (level is not null) query = query.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        var targetLevel = BandToLevel(adaptiveDifficulty);
        var words = await query.Where(x => x.Level == targetLevel).OrderBy(_ => EF.Functions.Random()).Take(count).ToListAsync(ct);
        if (words.Count < count)
            words = await query.OrderBy(_ => EF.Functions.Random()).Take(count).ToListAsync(ct);
        if (words.Count < count) return null;
        var all = await db.VocabularyWords.AsNoTracking().Where(x => x.PublicationStatus == Domain.VocabularyPublicationStatus.Published)
            .Select(x => new { x.Id, x.Term, x.Translation, x.Level }).ToListAsync(ct);
        var session = new QuizSession { UserId = userId, Level = level, Category = request.Category, QuestionCount = count };
        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var type = (QuizQuestionType)(i % 6 + 1);
            // Writing/matching/ordering still use option-based grading in the MVP contract;
            // the type lets clients render the richer interaction and keeps the answer API stable.
            var correct = type == QuizQuestionType.Translation ? word.Translation : word.Term;
            // Distractors are unique, never another spelling of the answer, and prefer the target's level.
            var candidates = all.Where(x => x.Id != word.Id)
                .Select(x => new { x.Level, Text = type == QuizQuestionType.Translation ? x.Translation : x.Term })
                .Where(x => !string.IsNullOrWhiteSpace(x.Text) && !x.Text.Equals(correct, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Level == word.Level ? 0 : 1).ThenBy(_ => Random.Shared.Next())
                .Select(x => x.Text).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
            if (candidates.Count < 3) return null;
            candidates.Add(correct);
            var skill = type switch
            {
                QuizQuestionType.Translation => "meaning", QuizQuestionType.Definition => "definition",
                QuizQuestionType.SentenceCompletion => "grammar", QuizQuestionType.Listening => "listening",
                QuizQuestionType.Writing => "writing", _ => "recall"
            };
            var question = new QuizQuestion { Session = session, VocabularyWordId = word.Id, Order = i + 1, Type = type, Difficulty = adaptiveDifficulty, Skill = skill, Explanation = $"{word.Term} = {word.Translation}. {word.Definition}", ErrorTag = skill };
            foreach (var option in candidates.OrderBy(_ => Guid.NewGuid()).Select((text, index) => new QuizOption { Question = question, Key = ((char)('A' + index)).ToString(), Text = text, IsCorrect = text == correct })) question.Options.Add(option);
            session.Questions.Add(question);
        }
        if (!await entitlements.TryConsumeQuizAsync(userId, ct)) throw new DailyLimitExceededException("Daily quiz limit reached. Upgrade to Premium for unlimited quizzes.");
        db.QuizSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, session.Id, ct);
    }

    public async Task<QuizSessionDto?> GetAsync(Guid userId, Guid sessionId, CancellationToken ct) =>
        await Load(userId, sessionId).SingleOrDefaultAsync(ct) is { } session ? Map(session) : null;

    public async Task<QuizAnswerResult?> AnswerAsync(Guid userId, Guid sessionId, Guid questionId, SubmitAnswerRequest request, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).AsNoTracking().SingleOrDefaultAsync(ct);
        var question = session?.Questions.SingleOrDefault(x => x.Id == questionId);
        if (session is null || question is null || session.Status != QuizSessionStatus.InProgress || question.Answered) return null;
        var selected = question.Options.SingleOrDefault(x => x.Key.Equals(request.OptionKey, StringComparison.OrdinalIgnoreCase));
        if (selected is null) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Claim the question atomically: parallel submits race on this row and only one flips it.
        var claimed = await db.QuizQuestions.Where(x => x.Id == questionId && x.SessionId == sessionId && !x.Answered)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Answered, true).SetProperty(x => x.IsCorrect, selected.IsCorrect), ct);
        if (claimed == 0) return null;
        if (selected.IsCorrect)
            await db.QuizSessions.Where(x => x.Id == sessionId).ExecuteUpdateAsync(s => s.SetProperty(x => x.CorrectCount, x => x.CorrectCount + 1), ct);
        var rating = selected.IsCorrect ? ReviewRating.Good : ReviewRating.Again;
        var progress = await WordProgressStore.LockOrCreateAsync(db, userId, question.VocabularyWordId, ct);
        progress.TotalReviews++;
        progress.LastRating = rating;
        if (selected.IsCorrect) progress.CorrectReviews++; else { progress.Lapses++; progress.Repetition = 0; }
        progress.Repetition = selected.IsCorrect ? progress.Repetition + 1 : 0;
        progress.MasteryScore = Math.Clamp(progress.MasteryScore * 0.8m + (selected.IsCorrect ? 15m : 0m), 0m, 100m);
        progress.LastReviewedAtUtc = DateTime.UtcNow;
        progress.DueAtUtc = progress.LastReviewedAtUtc.Value.AddDays(selected.IsCorrect ? Math.Max(1, progress.IntervalDays) : 1);
        db.ReviewEvents.Add(new ReviewEvent { UserId = userId, VocabularyWordId = question.VocabularyWordId, Rating = rating, ClientEventId = $"quiz:{sessionId}:{questionId}" });
        await db.SaveChangesAsync(ct);
        var correctCount = await db.QuizSessions.Where(x => x.Id == sessionId).Select(x => x.CorrectCount).SingleAsync(ct);
        var answeredCount = await db.QuizQuestions.CountAsync(x => x.SessionId == sessionId && x.Answered, ct);
        await transaction.CommitAsync(ct);
        return new(question.Id, selected.IsCorrect, question.Options.Single(x => x.IsCorrect).Key, correctCount, answeredCount);
    }

    public async Task<QuizResultDto?> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).SingleOrDefaultAsync(ct);
        if (session is null) return null;
        if (session.Status == QuizSessionStatus.InProgress) { session.Status = QuizSessionStatus.Completed; session.CompletedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
        var answered = session.Questions.Count(x => x.Answered);
        return new(session.Id, session.Status, session.QuestionCount, answered, session.CorrectCount, session.QuestionCount == 0 ? 0 : (int)Math.Round(session.CorrectCount * 100d / session.QuestionCount));
    }

    private IQueryable<QuizSession> Load(Guid userId, Guid id) => db.QuizSessions.Include(x => x.Questions).ThenInclude(x => x.Options).Include(x => x.Questions).ThenInclude(x => x.VocabularyWord).Where(x => x.Id == id && x.UserId == userId);
    private static QuizSessionDto Map(QuizSession s) => new(s.Id, s.Status, s.QuestionCount, s.Questions.Count(x => x.Answered), s.CorrectCount, s.Questions.OrderBy(x => x.Order).Select(q => new QuizQuestionDto(q.Id, q.Order, q.Type, q.Difficulty, q.Skill, q.Answered ? q.Explanation : null, q.Answered ? q.ErrorTag : null, Prompt(q.Type, q.VocabularyWord), q.Options.OrderBy(x => x.Key).Select(o => new QuizOptionDto(o.Key, o.Text)).ToList(), q.Answered, q.Answered ? q.IsCorrect : null)).ToList());

    // Term-answered prompts never show the term. Listening is spoken (the client hides its text),
    // so it carries the full sentence, or just the term when the sentence lacks the whole word.
    private static string Prompt(QuizQuestionType type, VocabularyWord word)
    {
        var hasSentence = VocabularyQuality.ContainsWholeWord(word.ExampleSentence, word.Term);
        return type switch
        {
            QuizQuestionType.Translation => word.Term,
            QuizQuestionType.Writing => word.Translation,
            QuizQuestionType.Listening => hasSentence ? word.ExampleSentence! : word.Term,
            QuizQuestionType.Definition => VocabularyQuality.BlankWholeWord(word.Definition, word.Term),
            _ => VocabularyQuality.BlankWholeWord(hasSentence ? word.ExampleSentence! : word.Definition, word.Term)
        };
    }

    private async Task<int> GetAdaptiveDifficultyAsync(Guid userId, CancellationToken ct)
    {
        var recent = await db.QuizQuestions.AsNoTracking()
            .Where(x => x.Session.UserId == userId && x.Answered)
            .OrderByDescending(x => x.Session.CreatedAtUtc).ThenByDescending(x => x.Order)
            .Take(20).Select(x => x.IsCorrect).ToListAsync(ct);
        if (recent.Count == 0) return 2;
        var success = recent.Count(x => x) / (double)recent.Count;
        var latestDifficulty = await db.QuizQuestions.AsNoTracking()
            .Where(x => x.Session.UserId == userId && x.Answered)
            .OrderByDescending(x => x.Session.CreatedAtUtc).ThenByDescending(x => x.Order)
            .Select(x => (int?)x.Difficulty).FirstOrDefaultAsync(ct) ?? 2;
        return Math.Clamp(latestDifficulty + (success >= 0.85 ? 1 : success <= 0.55 ? -1 : 0), 1, 4);
    }

    private static string BandToLevel(int difficulty) => difficulty switch
    {
        1 => "A1", 2 => "A2", 3 => "B1", _ => "B2"
    };
}
