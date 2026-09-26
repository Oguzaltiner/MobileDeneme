using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnglishLearning.Infrastructure.Persistence;
using EnglishLearning.Application.Auth;
using EnglishLearning.Domain;
using Microsoft.AspNetCore.Identity;
using EnglishLearning.Infrastructure.Auth;

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

        return services;
    }
}
