using System.ComponentModel.DataAnnotations;

namespace EnglishLearning.Application.Notifications;

public sealed record NotificationPreferencesDto(bool Enabled, int ReminderHour, int QuietHoursStart, int QuietHoursEnd);
public sealed record UpdateNotificationPreferencesRequest(bool Enabled, [Range(0, 23)] int ReminderHour, [Range(0, 23)] int QuietHoursStart, [Range(0, 23)] int QuietHoursEnd);
