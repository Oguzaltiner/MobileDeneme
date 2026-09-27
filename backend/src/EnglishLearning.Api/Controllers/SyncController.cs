using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnglishLearning.Domain;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

/// <summary>Idempotent delivery endpoint for mobile clients that were offline.</summary>
[ApiController, Authorize, Route("api/v1/sync")]
public sealed class SyncController(EnglishLearningDbContext db) : ControllerBase
{
    public sealed record PracticeEventInput(
        [param: Required, StringLength(100)] string ClientEventId,
        Guid SessionId,
        [param: Required, StringLength(40)] string StepKey,
        Guid? VocabularyWordId,
        bool? IsCorrect,
        ReviewRating? Rating,
        DateTime? OccurredAtUtc);

    public sealed record PracticeEventBatchRequest(
        [param: Required, MinLength(1), MaxLength(100)] IReadOnlyList<PracticeEventInput> Events);

    [HttpPost("practice-events")]
    public async Task<IActionResult> PushPracticeEvents(PracticeEventBatchRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var inputs = request.Events
            .Select(x => x with { ClientEventId = x.ClientEventId.Trim(), StepKey = x.StepKey.Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.ClientEventId))
            .ToList();
        if (inputs.Count != request.Events.Count || inputs.Select(x => x.ClientEventId).Distinct(StringComparer.Ordinal).Count() != inputs.Count)
            return BadRequest(new { message = "Client event ids must be unique and non-empty." });

        var sessionIds = inputs.Select(x => x.SessionId).Distinct().ToArray();
        var ownedSessions = await db.PracticeSessions.AsNoTracking()
            .Where(x => x.UserId == userId && sessionIds.Contains(x.Id))
            .Select(x => new { x.Id, StepKeys = x.Steps.Select(step => step.Key) })
            .ToListAsync(ct);
        if (ownedSessions.Count != sessionIds.Length)
            return BadRequest(new { message = "One or more practice sessions do not belong to this user." });
        var sessionSteps = ownedSessions.ToDictionary(x => x.Id, x => x.StepKeys.ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (inputs.Any(x => !sessionSteps.TryGetValue(x.SessionId, out var steps) || !steps.Contains(x.StepKey)))
            return BadRequest(new { message = "One or more practice steps do not belong to the session." });

        var clientIds = inputs.Select(x => x.ClientEventId).ToArray();
        var existing = await db.PracticeEvents.AsNoTracking()
            .Where(x => x.UserId == userId && x.ClientEventId != null && clientIds.Contains(x.ClientEventId))
            .Select(x => x.ClientEventId!).ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var accepted = 0;
        foreach (var input in inputs.Where(x => !existingSet.Contains(x.ClientEventId)))
        {
            db.PracticeEvents.Add(new PracticeEvent
            {
                UserId = userId,
                SessionId = input.SessionId,
                VocabularyWordId = input.VocabularyWordId,
                StepKey = input.StepKey,
                IsCorrect = input.IsCorrect,
                Rating = input.Rating,
                ClientEventId = input.ClientEventId,
                CreatedAtUtc = input.OccurredAtUtc is { } occurred && occurred <= DateTime.UtcNow.AddMinutes(5) ? occurred.ToUniversalTime() : DateTime.UtcNow
            });
            accepted++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { accepted, alreadyProcessed = existing.Count, total = inputs.Count });
    }
}
