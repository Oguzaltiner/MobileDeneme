using System.Net.Http.Headers;
using System.Security.Cryptography;
using EnglishLearning.Application.Auth;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EnglishLearning.Tests.Integration.Infrastructure;

/// <summary>
/// Hosts the real API against a throwaway PostgreSQL database (one per xUnit collection).
/// TEST_POSTGRES must be an admin connection string to the "postgres" database; when it is
/// unset every integration test skips instead of failing.
/// </summary>
public abstract class PostgresApiFixture : IAsyncLifetime
{
    public const string AdminConnectionEnvVar = "TEST_POSTGRES";

    private string? adminConnectionString;
    private string? databaseName;
    private ApiFactory? factory;

    public string? SkipReason { get; private set; }
    public string ConnectionString { get; private set; } = "";
    public WebApplicationFactory<Program> Factory => factory ?? throw new InvalidOperationException(SkipReason ?? "Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        adminConnectionString = Environment.GetEnvironmentVariable(AdminConnectionEnvVar);
        if (string.IsNullOrWhiteSpace(adminConnectionString))
        {
            SkipReason = $"{AdminConnectionEnvVar} is not set; PostgreSQL integration tests are skipped.";
            return;
        }

        databaseName = $"el_test_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;

        // Random per-run key: tokens are only ever minted and validated inside this host.
        var signingKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        factory = new ApiFactory(ConnectionString, signingKey);
        await WithDbAsync(db => db.Database.MigrateAsync());
        await SeedAsync();
    }

    /// <summary>Collection-specific baseline data, run once after migrations.</summary>
    protected virtual Task SeedAsync() => Task.CompletedTask;

    public void SkipIfUnavailable()
    {
        if (SkipReason is not null) Assert.Skip(SkipReason);
    }

    public async Task WithDbAsync(Func<EnglishLearningDbContext, Task> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<EnglishLearningDbContext>());
    }

    public async Task<T> WithDbAsync<T>(Func<EnglishLearningDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<EnglishLearningDbContext>());
    }

    /// <summary>Creates a user straight in the database and mints a token with the app's own token service.</summary>
    public async Task<TestUser> CreateUserAsync(string role = "Learner", SubscriptionPlan? plan = null, DateTime? planExpiresAtUtc = null)
    {
        var user = new AppUser
        {
            Email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local",
            PasswordHash = "not-used-by-tests", DisplayName = $"Test {role}", Role = role
        };
        await WithDbAsync(async db =>
        {
            db.Users.Add(user);
            if (plan is not null)
                db.UserEntitlements.Add(new UserEntitlement
                {
                    UserId = user.Id, Plan = plan.Value, Provider = "test", ProductId = "test",
                    StartsAtUtc = DateTime.UtcNow.AddDays(-30), ExpiresAtUtc = planExpiresAtUtc
                });
            await db.SaveChangesAsync();
        });
        var token = Factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;
        return new TestUser(user.Id, user.Role, token);
    }

    public HttpClient CreateClient(TestUser? user = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (user is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        if (adminConnectionString is null || databaseName is null) return;

        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using (var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()", connection))
        {
            terminate.Parameters.AddWithValue("name", databaseName);
            await terminate.ExecuteNonQueryAsync();
        }
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", connection);
        await drop.ExecuteNonQueryAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class ApiFactory(string connectionString, string signingKey) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // "Testing" keeps Program from running the Development migration/seed path.
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Jwt:SigningKey", signingKey);
        }
    }
}

public sealed record TestUser(Guid Id, string Role, string Token);
