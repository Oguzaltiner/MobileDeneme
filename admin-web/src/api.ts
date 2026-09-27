export type Overview = { users: number; vocabularyWords: number; publishedVocabularyWords: number; vocabularyCapacity: number; reviewEvents: number; completedQuizzes: number; premiumUsers: number };
export type QueueItem = { id: string; term: string; translation: string; level: string; category: string; status: string; publishedAtUtc: string | null };
export type AuditItem = { id: string; userId: string; action: string; entityType: string; entityId: string | null; createdAtUtc: string };
export type VocabularyInput = { term: string; pronunciation: string; partOfSpeech: string; definition: string; translation: string; level: string; category: string; exampleSentence: string };

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5057/api/v1';
const tokenKey = 'english-learning-admin-token';
export const tokenStore = { get: () => localStorage.getItem(tokenKey), set: (value: string) => localStorage.setItem(tokenKey, value), clear: () => localStorage.removeItem(tokenKey) };

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers); headers.set('Content-Type', 'application/json');
  const token = tokenStore.get(); if (token) headers.set('Authorization', `Bearer ${token}`);
  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  if (!response.ok) { let message = `İstek başarısız (${response.status}).`; try { message = (await response.json()).message ?? message; } catch { /* empty */ } throw new Error(message); }
  return response.status === 204 ? undefined as T : await response.json() as T;
}

export const adminApi = {
  login: (email: string, password: string) => request<{ accessToken: string; user: { email: string } }>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  overview: () => request<Overview>('/admin/overview'),
  queue: (status = '') => request<QueueItem[]>(`/admin/content/vocabulary/review-queue${status ? `?status=${status}` : ''}`),
  audit: () => request<AuditItem[]>('/admin/audit?limit=50'),
  createVocabulary: (body: VocabularyInput) => request<{ id: string }>('/admin/vocabulary', { method: 'POST', body: JSON.stringify(body) }),
  submitReview: (id: string) => request<void>(`/admin/content/vocabulary/${id}/submit-review`, { method: 'POST' }),
  publish: (id: string) => request<void>(`/admin/content/vocabulary/${id}/publish`, { method: 'POST' }),
  reject: (id: string) => request<void>(`/admin/content/vocabulary/${id}/reject`, { method: 'POST' })
};

export function tokenRole(token: string | null): string {
  if (!token) return '';
  try { const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))); return payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? ''; } catch { return ''; }
}
