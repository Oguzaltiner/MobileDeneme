using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnglishLearning.Infrastructure.Persistence;

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

        return services;
    }
}
