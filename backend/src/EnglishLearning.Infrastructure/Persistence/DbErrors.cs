using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishLearning.Infrastructure.Persistence;

internal static class DbErrors
{
    /// <summary>True when PostgreSQL rejected the save because of a unique index (a lost idempotency race).</summary>
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        (ex.InnerException as PostgresException)?.SqlState == PostgresErrorCodes.UniqueViolation;
}
