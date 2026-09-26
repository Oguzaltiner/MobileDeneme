using EnglishLearning.Application.Statistics;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Statistics;

public sealed class StatisticsService(EnglishLearningDbContext db) : IStatisticsService
{
    public async Task<WeeklyLearningStats> GetWeeklyAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-6);
        var events = await db.ReviewEvents.AsNoTracking()
            .Where(x => x.UserId == userId && x.CreatedAtUtc >= from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(x => new { x.CreatedAtUtc, x.Rating })
            .ToListAsync(ct);
        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var date = from.AddDays(offset);
            var daily = events.Where(x => DateOnly.FromDateTime(x.CreatedAtUtc) == date).ToList();
            return new DailyLearningStat(date, daily.Count, daily.Count(x => (int)x.Rating >= 2));
        }).ToList();
        var total = events.Count;
        var successful = events.Count(x => (int)x.Rating >= 2);
        return new(total, successful, total == 0 ? 0 : (int)Math.Round(successful * 100d / total), events.Select(x => x.CreatedAtUtc.Date).Distinct().Count(), days);
    }
}
