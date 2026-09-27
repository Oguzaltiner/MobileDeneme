using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/v1/admin")]
public sealed class AdminController(EnglishLearningDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(new
    {
        users = await db.Users.CountAsync(ct),
        vocabularyWords = await db.VocabularyWords.CountAsync(ct),
        reviewEvents = await db.ReviewEvents.CountAsync(ct),
        completedQuizzes = await db.QuizSessions.CountAsync(x => x.Status == EnglishLearning.Domain.QuizSessionStatus.Completed, ct),
        premiumUsers = await db.UserEntitlements.CountAsync(x => x.Plan != EnglishLearning.Domain.SubscriptionPlan.Free, ct)
    });
}
