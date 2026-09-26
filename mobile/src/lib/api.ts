export type ApiError = { status: number; message: string };
export const apiConfig = { baseUrl: process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5057/api/v1' };
export type AuthUser = { id: string; email: string; displayName?: string | null; onboardingCompleted: boolean };
export type AuthResponse = { accessToken: string; refreshToken: string; accessTokenExpiresAtUtc: string; user: AuthUser };
export type LoginRequest = { email: string; password: string };
export type RegisterRequest = LoginRequest & { displayName?: string };
export type OnboardingRequest = { currentLevel: 'A1'|'A2'|'B1'|'B2'|'C1'|'C2'; dailyGoal: number; learningPurpose: 'general'|'business'|'academic'|'travel'|'exam' };
let accessToken: string | null = null;
export function setAccessToken(token: string | null) { accessToken = token; }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers); headers.set('Content-Type', 'application/json'); if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(`${apiConfig.baseUrl}${path}`, { ...init, headers });
  if (!response.ok) { let message = 'İstek başarısız oldu.'; try { message = ((await response.json()) as { message?: string }).message ?? message; } catch { /* empty */ } throw { status: response.status, message } satisfies ApiError; }
  return response.status === 204 ? (undefined as T) : await response.json() as T;
}
export const api = {
  login: (body: LoginRequest) => request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  register: (body: RegisterRequest) => request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  refresh: (refreshToken: string) => request<AuthResponse>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
  completeOnboarding: (body: OnboardingRequest) => request<AuthUser>('/auth/onboarding', { method: 'PUT', body: JSON.stringify(body) }),
  info: () => request<{ service: string; version: string; status: string }>('/info'),
};
