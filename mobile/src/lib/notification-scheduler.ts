import AsyncStorage from '@react-native-async-storage/async-storage';
import type { NotificationPreferences } from './api';

const KEY = 'notification-schedule-v1';
export type LocalNotificationSchedule = NotificationPreferences & { nextReminderAtUtc: string | null };

export async function scheduleLocalReminder(preferences: NotificationPreferences): Promise<LocalNotificationSchedule> {
  const next = preferences.enabled ? nextReminder(preferences.reminderHour, preferences.quietHoursStart, preferences.quietHoursEnd) : null;
  const value = { ...preferences, nextReminderAtUtc: next };
  await AsyncStorage.setItem(KEY, JSON.stringify(value));
  return value;
}

export async function getLocalNotificationSchedule(): Promise<LocalNotificationSchedule | null> {
  const raw = await AsyncStorage.getItem(KEY);
  return raw ? JSON.parse(raw) as LocalNotificationSchedule : null;
}

function nextReminder(hour: number, quietStart: number, quietEnd: number) {
  const now = new Date();
  const next = new Date(now);
  next.setHours(hour, 0, 0, 0);
  if (next <= now) next.setDate(next.getDate() + 1);
  // If the configured reminder falls in quiet hours, defer to the next quiet-window exit.
  const h = next.getHours();
  const inQuiet = quietStart > quietEnd ? h >= quietStart || h < quietEnd : h >= quietStart && h < quietEnd;
  if (inQuiet) { next.setHours(quietEnd, 0, 0, 0); if (next <= now) next.setDate(next.getDate() + 1); }
  return next.toISOString();
}
