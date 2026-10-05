export type Overview = { users: number; vocabularyWords: number; publishedVocabularyWords: number; vocabularyCapacity: number; reviewEvents: number; completedQuizzes: number; premiumUsers: number; quizAccuracyPercent: number; reviewSuccessPercent: number };
export type QueueItem = { id: string; term: string; translation: string; level: string; category: string; status: string; publishedAtUtc: string | null };
export type AuditItem = { id: string; userId: string; action: string; entityType: string; entityId: string | null; createdAtUtc: string };
export type VocabularyInput = { term: string; pronunciation: string; partOfSpeech: string; definition: string; translation: string; level: string; category: string; exampleSentence: string };
export type VocabularyItem = VocabularyInput & { id: string; status: string; publishedAtUtc: string | null };
export type VocabularyPage = { items: VocabularyItem[]; page: number; pageSize: number; totalCount: number; totalPages: number };
export type VocabularyImportWord = VocabularyInput;
export type VocabularyImportResult = { created: number; updated: number; skippedPublished: string[]; total: number };
export type VocabularyImportInvalid = { index: number; term: string; reason: string };
export type ContentItem = { id: string; key: string; title: string; type: string; level: string; category: string; payloadJson: string; status: string; version: number; createdAtUtc: string; updatedAtUtc: string };

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5057/api/v1';
const tokenKey = 'english-learning-admin-token';
export const tokenStore = { get: () => localStorage.getItem(tokenKey), set: (value: string) => localStorage.setItem(tokenKey, value), clear: () => localStorage.removeItem(tokenKey) };

export class ApiError extends Error {
  readonly status: number; readonly body: unknown;
  constructor(message: string, status: number, body: unknown) { super(message); this.name = 'ApiError'; this.status = status; this.body = body; }
}

const nonBlank = (value: unknown) => typeof value === 'string' && value.trim() ? value : undefined;
function errorMessage(status: number, body: unknown): string {
  const data = (body && typeof body === 'object' ? body : {}) as Record<string, unknown>;
  const validation = data.errors && typeof data.errors === 'object' && !Array.isArray(data.errors) ? Object.values(data.errors as Record<string, unknown>).flat().filter((x): x is string => typeof x === 'string').join(' ') : '';
  return nonBlank(data.message) ?? nonBlank(data.detail) ?? nonBlank(validation) ?? (status === 401 ? 'Oturum geçersiz veya süresi dolmuş (401). Lütfen yeniden giriş yapın.' : status === 403 ? 'Bu işlem için yetkiniz yok (403).' : undefined) ?? nonBlank(data.title) ?? `İstek başarısız (${status}).`;
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers); headers.set('Content-Type', 'application/json');
  const token = tokenStore.get(); if (token) headers.set('Authorization', `Bearer ${token}`);
  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  if (!response.ok) { let body: unknown = null; try { body = await response.json(); } catch { /* empty */ } throw new ApiError(errorMessage(response.status, body), response.status, body); }
  return response.status === 204 ? undefined as T : await response.json() as T;
}

export const adminApi = {
  login: (email: string, password: string) => request<{ accessToken: string; user: { email: string } }>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  overview: () => request<Overview>('/admin/overview'),
  vocabulary: (params: { search?: string; level?: string; category?: string; status?: string; page?: number; pageSize?: number } = {}) => {
    const query = new URLSearchParams(); Object.entries(params).forEach(([key, value]) => { if (value) query.set(key, String(value)); });
    return request<VocabularyPage>(`/admin/vocabulary?${query.toString()}`);
  },
  content: (status = '') => request<ContentItem[]>(`/admin/content${status ? `?status=${encodeURIComponent(status)}` : ''}`),
  createContent: (body: { key: string; title: string; type: string; level: string; category: string; payloadJson: string }) => request<{ id: string }>('/admin/content', { method: 'POST', body: JSON.stringify(body) }),
  contentAction: (id: string, action: 'submit' | 'publish' | 'archive' | 'draft') => request<void>(`/admin/content/${id}/${action}`, { method: 'POST' }),
  queue: (status = '') => request<QueueItem[]>(`/admin/content/vocabulary/review-queue${status ? `?status=${status}` : ''}`),
  audit: () => request<AuditItem[]>('/admin/audit?limit=50'),
  createVocabulary: (body: VocabularyInput) => request<{ id: string }>('/admin/vocabulary', { method: 'POST', body: JSON.stringify(body) }),
  submitReview: (id: string) => request<void>(`/admin/content/vocabulary/${id}/submit-review`, { method: 'POST' }),
  publish: (id: string) => request<void>(`/admin/content/vocabulary/${id}/publish`, { method: 'POST' }),
  bulkPublish: (ids: string[]) => request<{ published: number }>('/admin/content/vocabulary/bulk-publish', { method: 'POST', body: JSON.stringify({ ids }) }),
  importVocabulary: (words: VocabularyImportWord[]) => request<VocabularyImportResult>('/admin/content/vocabulary/import', { method: 'POST', body: JSON.stringify({ words }) }),
  reject: (id: string) => request<void>(`/admin/content/vocabulary/${id}/reject`, { method: 'POST' })
};

export function tokenRole(token: string | null): string {
  if (!token) return '';
  try { const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))); return payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? ''; } catch { return ''; }
}
