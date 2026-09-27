using EnglishLearning.Application.Practice;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Practice;

public sealed class PracticeService(EnglishLearningDbContext db) : IPracticeService
{
    public async Task<PracticePlan> GetPlanAsync(Guid userId, CancellationToken ct)
    {
        var purpose = await db.UserSettings.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.LearningPurpose).SingleOrDefaultAsync(ct) ?? "general";
        var path = purpose switch
        {
            "travel" => ("travel", "Seyahat pratiği"),
            "business" => ("business", "İş İngilizcesi pratiği"),
            "academic" => ("academic", "Akademik pratik"),
            "exam" => ("exam", "Sınav odaklı pratik"),
            _ => ("general", "Günlük İngilizce pratiği")
        };
        var steps = new List<PracticeStep>
        {
            new("review", "Hızlı tekrar", "Zamanı gelen kelimeleri hatırla.", "DailyMission", 3),
            new("sentence", "Cümleyi tamamla", "Kelimeyi gerçek bağlamda seç.", "SentenceChallenge", 2),
            new("matching", "Eşleştirme", "İngilizce kelimeyi doğru anlamla eşleştir.", "MatchingChallenge", 1),
            new("writing", "Yazarak hatırla", "Tanımdan kelimeyi üret.", "WritingChallenge", 2),
            new("conversation", "Konuşma pratiği", "Gerçek hayat cümlelerini dinle ve tekrar et.", "ConversationPractice", 2)
        };
        if (purpose == "exam") steps.Reverse();
        return new(path.Item1, path.Item2, steps.Sum(x => x.EstimatedMinutes), steps);
    }

    public async Task<PracticeSessionDto> StartAsync(Guid userId, StartPracticeSessionRequest request, CancellationToken ct)
    {
        var plan = await GetPlanAsync(userId, ct);
        var pathKey = string.IsNullOrWhiteSpace(request.PathKey) ? plan.PathKey : request.PathKey.Trim().ToLowerInvariant();
        var session = new PracticeSession { UserId = userId, PathKey = pathKey };
        session.Steps = plan.Steps.Select((x, i) => new PracticeSessionStep { SessionId = session.Id, Order = i + 1, Key = x.Key, Title = x.Title, EstimatedMinutes = x.EstimatedMinutes }).ToList();
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<PracticeSessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).SingleOrDefaultAsync(ct);
        return session is null ? null : Map(session);
    }

    public async Task<PracticeSessionDto?> CompleteStepAsync(Guid userId, Guid sessionId, Guid stepId, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).SingleOrDefaultAsync(ct);
        var step = session?.Steps.SingleOrDefault(x => x.Id == stepId);
        if (session is null || step is null || session.Status != PracticeSessionStatus.InProgress) return null;
        step.Completed = true; step.CompletedAtUtc = DateTime.UtcNow;
        db.PracticeEvents.Add(new PracticeEvent { UserId = userId, SessionId = sessionId, StepKey = step.Key });
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<PracticeSessionDto?> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await Load(userId, sessionId).SingleOrDefaultAsync(ct);
        if (session is null || session.Status != PracticeSessionStatus.InProgress) return null;
        session.Status = PracticeSessionStatus.Completed; session.CompletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    private IQueryable<PracticeSession> Load(Guid userId, Guid sessionId) => db.PracticeSessions.Include(x => x.Steps).Where(x => x.UserId == userId && x.Id == sessionId);
    private static PracticeSessionDto Map(PracticeSession x) => new(x.Id, x.PathKey, x.Status, x.StartedAtUtc, x.CompletedAtUtc, x.Steps.OrderBy(s => s.Order).Select(s => new PracticeSessionStepDto(s.Id, s.Order, s.Key, s.Title, s.EstimatedMinutes, s.Completed)).ToList());
}
