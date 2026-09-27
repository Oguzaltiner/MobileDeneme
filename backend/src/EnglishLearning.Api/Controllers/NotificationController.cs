using System.Security.Claims;
using EnglishLearning.Application.Notifications;
using EnglishLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/me/notifications")]
public sealed class NotificationController(EnglishLearningDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationPreferencesDto>> Get(CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return settings is null ? NotFound() : Ok(new NotificationPreferencesDto(settings.NotificationsEnabled, settings.ReminderHour, settings.QuietHoursStart, settings.QuietHoursEnd));
    }

    [HttpPut]
    public async Task<ActionResult<NotificationPreferencesDto>> Update(UpdateNotificationPreferencesRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var settings = await db.UserSettings.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (settings is null) return NotFound();
        settings.NotificationsEnabled = request.Enabled; settings.ReminderHour = request.ReminderHour; settings.QuietHoursStart = request.QuietHoursStart; settings.QuietHoursEnd = request.QuietHoursEnd;
        await db.SaveChangesAsync(ct);
        return Ok(new NotificationPreferencesDto(settings.NotificationsEnabled, settings.ReminderHour, settings.QuietHoursStart, settings.QuietHoursEnd));
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
