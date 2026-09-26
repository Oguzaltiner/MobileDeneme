using EnglishLearning.Application.Dashboard;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Dashboard;

public sealed class DashboardService(EnglishLearningDbContext db) : IDashboardService
{
    public async Task<DashboardSummary?> GetAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return settings is null ? null : new DashboardSummary(settings.CurrentLevel, settings.DailyGoal, 0, 0, 0);
    }
}
