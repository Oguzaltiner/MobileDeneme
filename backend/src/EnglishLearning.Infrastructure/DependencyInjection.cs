using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnglishLearning.Infrastructure.Persistence;
using EnglishLearning.Application.Auth;
using EnglishLearning.Domain;
using Microsoft.AspNetCore.Identity;
using EnglishLearning.Infrastructure.Auth;
using EnglishLearning.Application.Vocabulary;
using EnglishLearning.Application.Dashboard;
using EnglishLearning.Application.Entitlements;
using EnglishLearning.Infrastructure.Vocabulary;
using EnglishLearning.Infrastructure.Leaderboard;
using EnglishLearning.Application.Leaderboard;
using EnglishLearning.Application.LearningPaths;
using EnglishLearning.Infrastructure.LearningPaths;
using EnglishLearning.Infrastructure.Dashboard;
using EnglishLearning.Infrastructure.Entitlements;
using EnglishLearning.Application.Reviews;
using EnglishLearning.Infrastructure.Reviews;
using EnglishLearning.Application.Statistics;
using EnglishLearning.Infrastructure.Statistics;
using EnglishLearning.Application.Quiz;
using EnglishLearning.Infrastructure.Quiz;

namespace EnglishLearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<EnglishLearningDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthStore, AuthStore>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IVocabularyService, VocabularyService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<ILearningPathService, LearningPathService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IQuizService, QuizService>();

        return services;
    }
}
