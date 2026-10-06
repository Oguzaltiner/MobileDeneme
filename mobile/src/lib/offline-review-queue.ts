import AsyncStorage from '@react-native-async-storage/async-storage';
import type { MissionWordStepKey } from './api';

export type QueuedReview = { clientEventId: string; wordId: string; rating: 0 | 1 | 2 | 3; practiceSessionId?: string; practiceStepKey?: MissionWordStepKey; schemaVersion?: 1; retryCount?: number; lastError?: string };
/** Why a flush stopped early: `network` (offline/timeout), `server` (5xx), `limit` (429), `auth` (401/403). */
export type FlushStopReason = 'network' | 'server' | 'limit' | 'auth';
export type FlushResult = { sent: number; dropped: number; remaining: number; stoppedBy?: FlushStopReason };
const STORAGE_KEY = 'offline-review-queue-v1';
// The server will never accept these (unknown word, invalid event id); retrying would block the queue forever.
const PERMANENT_FAILURES = new Set([400, 404, 410, 422]);

async function readQueue(): Promise<QueuedReview[]> {
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) as QueuedReview[] : [];
  } catch { return []; }
}

async function writeQueue(items: QueuedReview[]) {
  await AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(items));
}

export async function enqueueReview(item: QueuedReview) {
  const queue = await readQueue();
  if (!queue.some(x => x.clientEventId === item.clientEventId)) await writeQueue([...queue, item]);
}

export async function getQueuedReviewCount() { return (await readQueue()).length; }

/** Sign-out: queued ratings belong to the previous account and must never be sent with the next one's token. */
export async function clearReviewQueue() {
  try { await AsyncStorage.removeItem(STORAGE_KEY); } catch { /* storage unavailable: nothing to clear */ }
}

function statusOf(error: unknown) {
  const status = (error as { status?: unknown } | null)?.status;
  return typeof status === 'number' ? status : 0;
}

function stopReasonFor(status: number): FlushStopReason {
  if (status === 429) return 'limit';
  if (status === 401 || status === 403) return 'auth';
  if (status >= 500) return 'server';
  return 'network';
}

async function runFlush(submit: (item: QueuedReview) => Promise<unknown>): Promise<FlushResult> {
  const result: FlushResult = { sent: 0, dropped: 0, remaining: 0 };
  const queue = await readQueue();
  for (const item of queue) {
    try {
      await submit(item);
      result.sent++;
      const latest = await readQueue();
      await writeQueue(latest.filter(x => x.clientEventId !== item.clientEventId));
    } catch (error) {
      const status = statusOf(error);
      const latest = await readQueue();
      if (PERMANENT_FAILURES.has(status)) {
        result.dropped++;
        if (__DEV__) console.warn(`Kuyruktaki değerlendirme sunucu tarafından reddedildi (${status}); kuyruktan çıkarıldı.`);
        await writeQueue(latest.filter(x => x.clientEventId !== item.clientEventId));
        continue;
      }
      // Network, 5xx, 429 and auth failures keep the item and stop this pass; the next flush (app foreground,
      // screen open) retries once, so a quota-limited queue never spins.
      const failed = latest.find(x => x.clientEventId === item.clientEventId);
      if (failed) { failed.retryCount = (failed.retryCount ?? 0) + 1; failed.lastError = status ? `HTTP ${status}` : 'Sunucuya gönderilemedi'; await writeQueue(latest); }
      result.stoppedBy = stopReasonFor(status);
      break;
    }
  }
  result.remaining = (await readQueue()).length;
  return result;
}

let inFlight: Promise<FlushResult> | null = null;

/** Sends queued reviews in order. Concurrent callers are serialized so the same item is not sent twice at once. */
export async function flushReviewQueue(submit: (item: QueuedReview) => Promise<unknown>): Promise<FlushResult> {
  while (inFlight) await inFlight.catch(() => undefined);
  const run = runFlush(submit);
  inFlight = run;
  try { return await run; } finally { if (inFlight === run) inFlight = null; }
}

export function createReviewEventId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}
