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
export type Achievement = { key: string; title: string; description: string; unlocked: boolean };
export type DashboardSummary = { currentLevel?: string | null; dailyGoal: number; todayProgress: number; totalWordsLearned: number; dueReviewCount: number; currentStreak: number; longestStreak: number; weeklyReviewGoal: number; weeklyReviewProgress: number; achievements: Achievement[]; coachTitle: string; coachMessage: string; recommendedSessionSize: number; coachReason: string };
export type QuizOption = { key: string; text: string };
export type QuizQuestion = { id: string; order: number; type: number; prompt: string; options: QuizOption[]; answered: boolean; isCorrect?: boolean | null };
export type QuizSession = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; questions: QuizQuestion[] };
export type QuizAnswerResult = { questionId: string; isCorrect: boolean; correctOptionKey: string; correctCount: number; answeredCount: number };
export type QuizResult = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; scorePercent: number };
export type Entitlement = { plan: number; isPremium: boolean; maxLevel: string; dailyWordLimit: number; dailyQuizLimit: number; adsEnabled: boolean; dailyWordsUsed: number; dailyQuizzesUsed: number; features: string[] };
export type ReviewResult = { wordId: string; rating: number; repetition: number; intervalDays: number; dueAtUtc: string; dailyWordsUsed: number; dailyWordLimit: number };
export type WeeklyLearningStats = { totalReviews: number; successfulReviews: number; successPercent: number; learnedWords: number; days: { date: string; reviews: number; successfulReviews: number }[] };
export type SentenceChallenge = { wordId: string; sentence: string; answer: string; options: string[]; translation: string; explanation: string };
export type WritingChallenge = { wordId: string; prompt: string; answer: string; translation: string; hint: string };
export type MatchingChallenge = { wordId: string; term: string; answer: string; options: string[] };
export type LeaderboardEntry = { rank: number; displayName: string; points: number; isCurrentUser: boolean };
export type LeaderboardSummary = { league: string; periodEndsAtUtc: string; entries: LeaderboardEntry[]; currentUserRank: number; currentUserPoints: number; xpBreakdown: { reviewXp: number; quizXp: number; reviewCount: number; quizCount: number }; reward: { title: string; description: string }; personalBestPoints: number; isClosingSoon: boolean };
export type LearningPath = { key: string; title: string; description: string; purpose: string; recommended: boolean };
export type PracticePlan = { pathKey: string; pathTitle: string; estimatedMinutes: number; steps: { key: string; title: string; description: string; route: string; estimatedMinutes: number }[] };
export type PracticeSession = { id: string; pathKey: string; status: number; startedAtUtc: string; completedAtUtc?: string | null; steps: { id: string; order: number; key: string; title: string; estimatedMinutes: number; completed: boolean }[] };
export type PlacementTest = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; estimatedLevel?: string | null; questions: { id: string; order: number; difficulty: number; type: number; prompt: string; options: { key: string; text: string }[]; answered: boolean; isCorrect?: boolean | null }[] };
export type PlacementAnswer = { questionId: string; isCorrect: boolean; correctOptionKey: string; correctCount: number; answeredCount: number };
export type PlacementResult = { id: string; status: number; questionCount: number; answeredCount: number; correctCount: number; scorePercent: number; estimatedLevel: string };
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
  startPlacement: (questionCount = 8) => request<PlacementTest>('/placement/sessions', { method: 'POST', body: JSON.stringify({ questionCount }) }),
  getPlacement: (attemptId: string) => request<PlacementTest>(`/placement/sessions/${attemptId}`),
  answerPlacement: (attemptId: string, questionId: string, optionKey: string) => request<PlacementAnswer>(`/placement/sessions/${attemptId}/questions/${questionId}/answers`, { method: 'POST', body: JSON.stringify({ optionKey }) }),
  completePlacement: (attemptId: string) => request<PlacementResult>(`/placement/sessions/${attemptId}/complete`, { method: 'POST' }),
  entitlement: () => request<Entitlement>('/me/entitlement'),
  verifyGooglePurchase: (body: { productId: string; purchaseToken: string }) => request<Entitlement>('/billing/google-play/verify', { method: 'POST', body: JSON.stringify(body) }),
  submitReview: (body: { wordId: string; rating: 0 | 1 | 2 | 3; clientEventId: string }) => request<ReviewResult>('/reviews', { method: 'POST', body: JSON.stringify(body) }),
  weeklyStats: () => request<WeeklyLearningStats>('/statistics/weekly'),
  dueWords: (limit = 20) => request<VocabularyWord[]>(`/reviews/due?limit=${Math.min(Math.max(limit, 1), 50)}`),
  sentenceChallenge: () => request<SentenceChallenge>('/vocabulary/sentence-challenge'),
  writingChallenge: () => request<WritingChallenge>('/vocabulary/writing-challenge'),
  matchingChallenge: () => request<MatchingChallenge>('/vocabulary/matching-challenge'),
  weeklyLeaderboard: () => request<LeaderboardSummary>('/leaderboard/weekly'),
  learningPaths: () => request<LearningPath[]>('/learning-paths'),
  selectLearningPath: (key: string) => request<void>(`/learning-paths/${encodeURIComponent(key)}/select`, { method: 'PUT' }),
  practicePlan: () => request<PracticePlan>('/practice/plan'),
  startPracticeSession: (pathKey?: string) => request<PracticeSession>('/practice/sessions', { method: 'POST', body: JSON.stringify({ pathKey }) }),
  completePracticeStep: (sessionId: string, stepId: string) => request<PracticeSession>(`/practice/sessions/${sessionId}/steps/${stepId}/complete`, { method: 'POST' }),
  completePracticeSession: (sessionId: string) => request<PracticeSession>(`/practice/sessions/${sessionId}/complete`, { method: 'POST' }),
};
