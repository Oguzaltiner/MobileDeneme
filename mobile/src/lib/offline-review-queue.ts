import AsyncStorage from '@react-native-async-storage/async-storage';

export type QueuedReview = { clientEventId: string; wordId: string; rating: 0 | 1 | 2 | 3 };
const STORAGE_KEY = 'offline-review-queue-v1';

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

export async function flushReviewQueue(submit: (item: QueuedReview) => Promise<unknown>) {
  const queue = await readQueue();
  for (const item of queue) {
    try {
      await submit(item);
      const latest = await readQueue();
      await writeQueue(latest.filter(x => x.clientEventId !== item.clientEventId));
    } catch {
      break;
    }
  }
}

export function createReviewEventId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}
