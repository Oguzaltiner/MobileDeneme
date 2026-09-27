using EnglishLearning.Application.Practice;
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
            new("writing", "Yazarak hatırla", "Tanımdan kelimeyi üret.", "WritingChallenge", 2)
        };
        if (purpose == "exam") steps.Reverse();
        return new(path.Item1, path.Item2, steps.Sum(x => x.EstimatedMinutes), steps);
    }
}
