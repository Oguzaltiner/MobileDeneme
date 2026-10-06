using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Infrastructure.Reviews;

internal static class WordProgressStore
{
    /// <summary>
    /// Returns the tracked progress row for (user, word), creating it if needed, locked with
    /// <c>FOR UPDATE</c>. Must run inside a transaction: parallel reviews/quiz answers of the same
    /// word then queue on the row instead of racing on the insert (500) or losing an update.
    /// </summary>
    public static async Task<UserWordProgress> LockOrCreateAsync(EnglishLearningDbContext db, Guid userId, Guid wordId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_word_progress ("UserId", "VocabularyWordId", "Repetition", "IntervalDays", "EaseFactor", "DueAtUtc", "CorrectReviews", "Lapses", "MasteryScore", "TotalReviews")
            VALUES ({userId}, {wordId}, 0, 0, 2.5, {now}, 0, 0, 0, 0)
            ON CONFLICT DO NOTHING
            """, ct);
        return await db.UserWordProgress
            .FromSqlInterpolated($"""SELECT * FROM user_word_progress WHERE "UserId" = {userId} AND "VocabularyWordId" = {wordId} FOR UPDATE""")
            .SingleAsync(ct);
    }
}
