import type { QueryClient } from '@tanstack/react-query';
import { api, type ApiError, type DailyMission, type DailyMissionToday, type MissionStep, type MissionStepKey, type MissionTimeZone } from '../../../lib/api';
import { getDeviceTimeZone } from '../../../lib/time-zone';

/** Shared by Home (hero card) and DailyMissionScreen so both always show the same mission state. */
export const DAILY_MISSION_QUERY_KEY = ['daily-mission', 'today'] as const;

export const isApiError = (error: unknown): error is ApiError => typeof error === 'object' && error !== null && typeof (error as { status?: unknown }).status === 'number';
export const errorStatus = (error: unknown) => (isApiError(error) ? error.status : -1);
/** Offline, timeout or a 5xx: the action can be retried later. */
export const isConnectionError = (error: unknown) => { const status = errorStatus(error); return status === 0 || status >= 500; };

export const OFFLINE_TEXT = 'Bağlantı yok. Cevaplar ve görevi tamamlama için internet gerekli; bağlantı gelince tekrar dene.';
export const SERVER_ERROR_TEXT = 'Sunucu hatası, tekrar dene.';
/** Retryable notice for offline (status 0) vs server failure (5xx); `null` when the error is neither. */
export function retryableNotice(error: unknown): { kind: 'offline' | 'server'; text: string } | null {
  const status = errorStatus(error);
  if (status === 0) return { kind: 'offline', text: OFFLINE_TEXT };
  if (status >= 500) return { kind: 'server', text: SERVER_ERROR_TEXT };
  return null;
}

/** A mission that still accepts work: today's in-progress one or yesterday's unfinished `pendingMission`. */
export const isOpenMission = (mission: DailyMission | null | undefined): mission is DailyMission => Boolean(mission) && mission?.status === 'inProgress';
export const missionProgress = (mission: DailyMission) => mission.steps.reduce((sum, step) => ({ done: sum.done + Math.min(step.done, step.required), required: sum.required + step.required }), { done: 0, required: 0 });

// An IANA name the server rejects (400) must not lock the user out: retry once with the UTC offset only.
async function withTimeZoneFallback<T>(call: (zone: MissionTimeZone) => Promise<T>): Promise<T> {
  const zone = getDeviceTimeZone();
  try { return await call(zone); }
  catch (error) {
    if (zone.timeZone && errorStatus(error) === 400) return call({ utcOffsetMinutes: zone.utcOffsetMinutes });
    throw error;
  }
}

export const fetchTodayMission = () => withTimeZoneFallback(api.missionToday);
export const startTodayMission = () => withTimeZoneFallback(api.startMission);

export const STEP_LABELS: Record<MissionStepKey, string> = { review: 'Tekrar', 'new-words': 'Yeni kelimeler', recall: 'Hatırlama', listening: 'Dinleme' };
export const stepLabel = (step: Pick<MissionStep, 'key' | 'title'>) => step.title || STEP_LABELS[step.key] || step.key;
export const sortSteps = (steps: MissionStep[]) => [...steps].sort((a, b) => a.order - b.order);

const MONTHS = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
/** "2026-10-07" → "7 Ekim". Manual on purpose: Intl date locales are incomplete on some Hermes builds. */
export function formatMissionDate(date: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(date);
  const month = match ? MONTHS[Number(match[2]) - 1] : undefined;
  return match && month ? `${Number(match[3])} ${month}` : date;
}

/**
 * Patches one mission inside the cached `GET /today` payload, whether it is today's `mission` or the unfinished
 * `pendingMission` from yesterday. Only today's mission drives the top-level `status`.
 */
export function storeMission(queryClient: QueryClient, missionId: string, update: (mission: DailyMission) => DailyMission) {
  queryClient.setQueryData<DailyMissionToday>(DAILY_MISSION_QUERY_KEY, (old) => {
    if (!old) return old;
    let next = old;
    if (old.mission?.id === missionId) { const mission = update(old.mission); next = { ...next, mission, status: mission.status }; }
    if (old.pendingMission?.id === missionId) next = { ...next, pendingMission: update(old.pendingMission) };
    return next;
  });
}

export function patchStep(mission: DailyMission, key: MissionStepKey, patch: (step: MissionStep) => Partial<MissionStep>): DailyMission {
  return { ...mission, steps: mission.steps.map((step) => (step.key === key ? { ...step, ...patch(step) } : step)) };
}
