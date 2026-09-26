export type ApiError = { status: number; message: string };
export const apiConfig = { baseUrl: process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5057/api/v1' };
export type AuthUser = { id: string; email: string; displayName?: string | null; onboardingCompleted: boolean };
export type AuthResponse = { accessToken: string; refreshToken: string; accessTokenExpiresAtUtc: string; user: AuthUser };
export type LoginRequest = { email: string; password: string };
export type RegisterRequest = LoginRequest & { displayName?: string };
export type OnboardingRequest = { currentLevel: 'A1'|'A2'|'B1'|'B2'|'C1'|'C2'; dailyGoal: number; learningPurpose: 'general'|'business'|'academic'|'travel'|'exam' };
export type VocabularyOption = { value: string; label: string };
export type VocabularyWord = { id: string; term: string; pronunciation: string; partOfSpeech: string; definition: string; translation: string; level: string; category: string; exampleSentence?: string | null };
export type VocabularyPage = { items: VocabularyWord[]; page: number; pageSize: number; totalCount: number };
export type DashboardSummary = { currentLevel?: string | null; dailyGoal: number; todayProgress: number; totalWordsLearned: number; dueReviewCount: number };
export type QuizOption = { key: string; text: string };
export type QuizQuestion = { id: string; order: number; type: number; prompt: string; options: QuizOption[]; answered: boolean; isCorrect?: boolean | null };
export type QuizSession = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; questions: QuizQuestion[] };
export type QuizAnswerResult = { questionId: string; isCorrect: boolean; correctOptionKey: string; correctCount: number; answeredCount: number };
export type QuizResult = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; scorePercent: number };
export type Entitlement = { plan: number; isPremium: boolean; maxLevel: string; dailyWordLimit: number; dailyQuizLimit: number; adsEnabled: boolean; dailyWordsUsed: number; dailyQuizzesUsed: number; features: string[] };
export type ReviewResult = { wordId: string; rating: number; repetition: number; intervalDays: number; dueAtUtc: string; dailyWordsUsed: number; dailyWordLimit: number };
export type WeeklyLearningStats = { totalReviews: number; successfulReviews: number; successPercent: number; learnedWords: number; days: { date: string; reviews: number; successfulReviews: number }[] };
let accessToken: string | null = null;
export function setAccessToken(token: string | null) { accessToken = token; }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers); headers.set('Content-Type', 'application/json'); if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 15000);
  let response: Response;
  try { response = await fetch(`${apiConfig.baseUrl}${path}`, { ...init, headers, signal: controller.signal }); }
  catch (error) { throw { status: 0, message: (error as { name?: string }).name === 'AbortError' ? 'Sunucu yanıt vermedi. API adresini ve bağlantıyı kontrol edin.' : 'Sunucuya bağlanılamadı.' } satisfies ApiError; }
  finally { clearTimeout(timeout); }
  if (!response.ok) { let message = 'İstek başarısız oldu.'; try { message = ((await response.json()) as { message?: string }).message ?? message; } catch { /* empty */ } throw { status: response.status, message } satisfies ApiError; }
  return response.status === 204 ? (undefined as T) : await response.json() as T;
}
export const api = {
  login: (body: LoginRequest) => request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  register: (body: RegisterRequest) => request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  refresh: (refreshToken: string) => request<AuthResponse>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
  completeOnboarding: (body: OnboardingRequest) => request<AuthUser>('/auth/onboarding', { method: 'PUT', body: JSON.stringify(body) }),
  info: () => request<{ service: string; version: string; status: string }>('/info'),
  vocabularyLevels: () => request<VocabularyOption[]>('/vocabulary/levels'),
  vocabularyCategories: () => request<VocabularyOption[]>('/vocabulary/categories'),
  words: (params: { level?: string; category?: string; search?: string } = {}) => {
    const query = new URLSearchParams();
    if (params.level) query.set('level', params.level);
    if (params.category) query.set('category', params.category);
    if (params.search) query.set('search', params.search);
    return request<VocabularyPage>(`/vocabulary/words${query.toString() ? `?${query.toString()}` : ''}`);
  },
  word: (id: string) => request<VocabularyWord>(`/vocabulary/words/${encodeURIComponent(id)}`),
  dashboard: () => request<DashboardSummary>('/dashboard'),
  createQuiz: (body: { questionCount: 5 | 10; level?: string; category?: string }) => request<QuizSession>('/quizzes/sessions', { method: 'POST', body: JSON.stringify(body) }),
  getQuiz: (id: string) => request<QuizSession>(`/quizzes/sessions/${id}`),
  answerQuiz: (sessionId: string, questionId: string, optionKey: string) => request<QuizAnswerResult>(`/quizzes/sessions/${sessionId}/questions/${questionId}/answers`, { method: 'POST', body: JSON.stringify({ optionKey }) }),
  completeQuiz: (id: string) => request<QuizResult>(`/quizzes/sessions/${id}/complete`, { method: 'POST' }),
  entitlement: () => request<Entitlement>('/me/entitlement'),
  verifyGooglePurchase: (body: { productId: string; purchaseToken: string }) => request<Entitlement>('/billing/google-play/verify', { method: 'POST', body: JSON.stringify(body) }),
  submitReview: (body: { wordId: string; rating: 0 | 1 | 2 | 3 }) => request<ReviewResult>('/reviews', { method: 'POST', body: JSON.stringify(body) }),
  weeklyStats: () => request<WeeklyLearningStats>('/statistics/weekly'),
};
