using EnglishLearning.Application.Quiz;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Quiz;

public sealed class QuizService(EnglishLearningDbContext db, IEntitlementService entitlements) : IQuizService
{
    public async Task<QuizSessionDto?> CreateAsync(Guid userId, CreateQuizRequest request, CancellationToken ct)
    {
        var count = request.QuestionCount is 10 ? 10 : 5;
        if (!await entitlements.CanAccessLevelAsync(userId, request.Level, ct)) return null;
        var query = db.VocabularyWords.AsNoTracking().AsQueryable();
        var entitlement = await entitlements.GetAsync(userId, ct);
        if (!entitlement.IsPremium) query = query.Where(x => x.Level == "A1" || x.Level == "A2");
        if (!string.IsNullOrWhiteSpace(request.Level)) query = query.Where(x => x.Level == request.Level);
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(x => x.Category == request.Category);
        var words = await query.OrderBy(_ => Guid.NewGuid()).Take(count).ToListAsync(ct);
        if (words.Count < count) return null;
        var all = await db.VocabularyWords.AsNoTracking().ToListAsync(ct);
        var session = new QuizSession { UserId = userId, Level = request.Level, Category = request.Category, QuestionCount = count };
        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            var type = i % 2 == 0 ? QuizQuestionType.Translation : QuizQuestionType.Definition;
            var correct = type == QuizQuestionType.Translation ? word.Translation : word.Term;
            var candidates = all.Where(x => x.Id != word.Id && (type == QuizQuestionType.Translation ? x.Translation != correct : x.Term != correct))
                .OrderBy(_ => Guid.NewGuid()).Take(3).Select(x => type == QuizQuestionType.Translation ? x.Translation : x.Term).ToList();
            if (candidates.Count < 3) return null;
            candidates.Add(correct);
            var question = new QuizQuestion { Session = session, VocabularyWordId = word.Id, Order = i + 1, Type = type };
            foreach (var option in candidates.OrderBy(_ => Guid.NewGuid()).Select((text, index) => new QuizOption { Question = question, Key = ((char)('A' + index)).ToString(), Text = text, IsCorrect = text == correct })) question.Options.Add(option);
            session.Questions.Add(question);
        }
        db.QuizSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, session.Id, ct);
    }

    public async Task<QuizSessionDto?> GetAsync(Guid userId, Guid sessionId, CancellationToken ct) =>
        await Load(userId, sessionId).SingleOrDefaultAsync(ct) is { } session ? Map(session) : null;

    public async Task<QuizAnswerResult?> AnswerAsync(Guid userId, Guid sessionId, Guid questionId, SubmitAnswerRequest request, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).SingleOrDefaultAsync(ct);
        var question = session?.Questions.SingleOrDefault(x => x.Id == questionId);
        if (session is null || question is null || session.Status != QuizSessionStatus.InProgress || question.Answered) return null;
        var selected = question.Options.SingleOrDefault(x => x.Key.Equals(request.OptionKey, StringComparison.OrdinalIgnoreCase));
        if (selected is null) return null;
        question.Answered = true; question.IsCorrect = selected.IsCorrect;
        session.CorrectCount = session.Questions.Count(x => x.Answered && x.IsCorrect);
        await db.SaveChangesAsync(ct);
        return new(question.Id, selected.IsCorrect, question.Options.Single(x => x.IsCorrect).Key, session.CorrectCount, session.Questions.Count(x => x.Answered));
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
    private static QuizSessionDto Map(QuizSession s) => new(s.Id, s.Status, s.QuestionCount, s.Questions.Count(x => x.Answered), s.CorrectCount, s.Questions.OrderBy(x => x.Order).Select(q => new QuizQuestionDto(q.Id, q.Order, q.Type, q.Type == QuizQuestionType.Translation ? q.VocabularyWord.Term : q.VocabularyWord.Definition, q.Options.OrderBy(x => x.Key).Select(o => new QuizOptionDto(o.Key, o.Text)).ToList(), q.Answered, q.Answered ? q.IsCorrect : null)).ToList());
}
