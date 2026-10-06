import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { api, type DailyMission, type MissionListening, type MissionQuestionStepKey, type MissionRecallQuestion, type MissionResult, type MissionStep, type MissionStepKey, type MissionWordStepKey, type VocabularyWord } from '../../../lib/api';
import { enqueueReview, flushReviewQueue } from '../../../lib/offline-review-queue';
import { ChoiceQuestionCard, type AnswerFeedback } from './ChoiceQuestionCard';
import { announce, MissionLimitCard, MissionNoticeBanner, type MissionNotice } from './MissionBanners';
import { MissionStepper } from './MissionStepper';
import { DAILY_MISSION_QUERY_KEY, errorStatus, formatMissionDate, isConnectionError, OFFLINE_TEXT, patchStep, retryableNotice, SERVER_ERROR_TEXT, sortSteps, stepLabel, STEP_LABELS, storeMission } from './queries';
import { WordStepCard, type ReviewRating } from './WordStepCard';

type StepFlags = Partial<Record<MissionStepKey, boolean>>;
type ActiveContent =
  | { kind: 'word'; step: MissionStep; stepKey: MissionWordStepKey; word: VocabularyWord; index: number; total: number }
  | { kind: 'recall'; step: MissionStep; question: MissionRecallQuestion; position: number; total: number; isLast: boolean }
  | { kind: 'listening'; step: MissionStep; listening: MissionListening }
  | { kind: 'finish' };

const QUEUED_TEXT = 'Bağlantı yok; değerlendirmen cihazda saklandı ve bağlantı gelince gönderilecek.';
const AUTH_TEXT = 'Oturumun doğrulanamadı. Çıkış yapıp yeniden giriş yapman gerekebilir.';
const PRACTICE_STEP_KEYS: MissionStepKey[] = ['review', 'new-words', 'recall', 'listening'];

/** Deterministic per mission + step + word so a retried or replayed rating is never counted twice by the server. */
const reviewEventId = (missionId: string, stepKey: MissionWordStepKey, wordId: string) => `mreview:${missionId}:${stepKey}:${wordId}`;

/**
 * Picks what to show: the question whose feedback is on screen, otherwise the first incomplete step in order
 * (review → new-words → recall → listening). Capped steps (free quota hit) and steps without content are skipped.
 */
function resolveContent(mission: DailyMission, capped: StepFlags, skipped: Partial<Record<MissionWordStepKey, number>>, feedback?: AnswerFeedback): ActiveContent {
  const steps = sortSteps(mission.steps);
  if (feedback) {
    const step = steps.find((item) => item.key === feedback.stepKey);
    if (step && feedback.stepKey === 'recall') {
      const index = mission.recallQuestions.findIndex((question) => question.id === feedback.questionId);
      const question = mission.recallQuestions[index];
      if (question) return { kind: 'recall', step, question, position: index + 1, total: mission.recallQuestions.length, isLast: !mission.recallQuestions.some((item) => !item.answered && item.id !== question.id) };
    }
    if (step && feedback.stepKey === 'listening' && mission.listening) return { kind: 'listening', step, listening: mission.listening };
  }
  for (const step of steps) {
    if (step.completed || capped[step.key]) continue;
    if (step.key === 'review' || step.key === 'new-words') {
      const words = step.key === 'review' ? mission.reviewWords : mission.newWords;
      // Words are rated in list order, so the server's `done` is the index of the next word.
      const index = step.done + (skipped[step.key] ?? 0);
      const word = words[index];
      if (word) return { kind: 'word', step, stepKey: step.key, word, index, total: words.length };
    } else if (step.key === 'recall') {
      const open = mission.recallQuestions.filter((question) => !question.answered);
      const question = open[0];
      if (question) return { kind: 'recall', step, question, position: mission.recallQuestions.indexOf(question) + 1, total: mission.recallQuestions.length, isLast: open.length === 1 };
    } else if (step.key === 'listening' && mission.listening && !mission.listening.answered) {
      return { kind: 'listening', step, listening: mission.listening };
    }
  }
  return { kind: 'finish' };
}

const contentKey = (content: ActiveContent) => content.kind === 'word' ? `w:${content.stepKey}:${content.word.id}` : content.kind === 'recall' ? `r:${content.question.id}` : content.kind === 'listening' ? `l:${content.listening.id}` : 'finish';

type Props = {
  mission: DailyMission;
  isPremium: boolean;
  /** Yesterday's unfinished mission (`pendingMission`), shown before today's. */
  isPendingMission: boolean;
  onCompleted: (result: MissionResult) => void;
  onPremium: () => void;
  scrollToTop: () => void;
};

/** One mission's step-by-step flow. Render with `key={mission.id}` so local state never leaks across missions. */
export function MissionFlow({ mission, isPremium, isPendingMission, onCompleted, onPremium, scrollToTop }: Props) {
  const queryClient = useQueryClient();
  const [capped, setCapped] = useState<StepFlags>({});
  const [skipped, setSkipped] = useState<Partial<Record<MissionWordStepKey, number>>>({});
  const [feedback, setFeedback] = useState<AnswerFeedback>();
  const [notice, setNotice] = useState<MissionNotice>(null);
  const [incompleteSteps, setIncompleteSteps] = useState<MissionStepKey[]>([]);
  const [retrying, setRetrying] = useState(false);
  // Synchronous guard: a second tap in the same frame fires before `isPending` re-renders the buttons disabled.
  const inFlight = useRef(new Set<string>());

  const steps = useMemo(() => sortSteps(mission.steps), [mission.steps]);
  const content = useMemo(() => resolveContent(mission, capped, skipped, feedback), [mission, capped, skipped, feedback]);
  const currentContentKey = contentKey(content);

  useEffect(() => { scrollToTop(); }, [currentContentKey, scrollToTop]);
  useEffect(() => { if (notice) scrollToTop(); }, [notice, scrollToTop]);

  const showFailure = (error: unknown, fallback: string) => setNotice(retryableNotice(error) ?? (errorStatus(error) === 401 ? { kind: 'error', text: AUTH_TEXT } : { kind: 'error', text: fallback }));
  const capStep = (key: MissionStepKey) => setCapped((current) => ({ ...current, [key]: true }));
  const refreshMission = () => void queryClient.invalidateQueries({ queryKey: DAILY_MISSION_QUERY_KEY });

  const rate = useMutation({
    mutationFn: async (vars: { stepKey: MissionWordStepKey; wordId: string; wordIndex: number; rating: ReviewRating }) => {
      const item = { wordId: vars.wordId, rating: vars.rating, clientEventId: reviewEventId(mission.id, vars.stepKey, vars.wordId), practiceSessionId: mission.practiceSessionId, practiceStepKey: vars.stepKey };
      try { await api.submitReview(item); return 'sent' as const; }
      catch (error) {
        // Reviews are idempotent by clientEventId, so offline/5xx ratings are queued and the mission moves on.
        if (isConnectionError(error)) { await enqueueReview({ ...item, schemaVersion: 1, retryCount: 0 }); return 'queued' as const; }
        throw error;
      }
    },
    onSuccess: (outcome, vars) => {
      // Idempotent: rating word N means at least N+1 words are done, so a replayed success can't add 2.
      storeMission(queryClient, mission.id, (current) => patchStep(current, vars.stepKey, (step) => { const done = Math.max(step.done, vars.wordIndex + 1); return { done, completed: step.completed || done >= step.required }; }));
      setNotice(outcome === 'queued' ? { kind: 'queued', text: QUEUED_TEXT } : null);
    },
    onError: (error, vars) => {
      const status = errorStatus(error);
      if (status === 429) { capStep(vars.stepKey); setNotice(null); return; }
      if (status === 404 || status === 400) {
        // The word is no longer available (or the step key was rejected); skip it rather than blocking the step.
        setSkipped((current) => ({ ...current, [vars.stepKey]: (current[vars.stepKey] ?? 0) + 1 }));
        setNotice({ kind: 'error', text: 'Bu kelime kaydedilemedi ve atlandı.' });
        return;
      }
      showFailure(error, 'Değerlendirme kaydedilemedi. Tekrar dene.');
    },
    onSettled: (_data, _error, vars) => { inFlight.current.delete(`rate:${vars.wordId}`); },
  });

  const answer = useMutation({
    mutationFn: (vars: { stepKey: MissionQuestionStepKey; questionId: string; optionKey: string }) => api.answerMission(mission.id, { stepKey: vars.stepKey, questionId: vars.questionId, optionKey: vars.optionKey }),
    onSuccess: (response, vars) => {
      setNotice(null);
      // The first stored answer wins. If it was correct it was the correct option; if it was wrong and we sent the
      // correct option now, the stored wrong option is unknown, so no option is marked as "yours".
      const selectedKey = response.isCorrect ? response.correctOptionKey : vars.optionKey === response.correctOptionKey ? undefined : vars.optionKey;
      setFeedback({ stepKey: vars.stepKey, questionId: vars.questionId, selectedKey, correctOptionKey: response.correctOptionKey, isCorrect: response.isCorrect, explanation: response.explanation, term: response.term, translation: response.translation });
      storeMission(queryClient, mission.id, (current) => {
        const next = response.step ? patchStep(current, response.step.key ?? vars.stepKey, () => ({ done: response.step.done, required: response.step.required, completed: response.step.completed })) : current;
        if (vars.stepKey === 'recall') return { ...next, recallQuestions: next.recallQuestions.map((question) => (question.id === vars.questionId ? { ...question, answered: true, isCorrect: response.isCorrect } : question)) };
        return next.listening ? { ...next, listening: { ...next.listening, answered: true, isCorrect: response.isCorrect } } : next;
      });
    },
    onError: (error) => {
      const status = errorStatus(error);
      if (status === 409) { setNotice({ kind: 'error', text: 'Bu görev artık cevap kabul etmiyor; görev yenileniyor.' }); refreshMission(); return; }
      if (status === 400 || status === 404) { setNotice({ kind: 'error', text: 'Bu soru artık geçerli değil; görev yenileniyor.' }); refreshMission(); return; }
      showFailure(error, 'Cevap gönderilemedi. Tekrar dene.');
    },
    onSettled: (_data, _error, vars) => { inFlight.current.delete(`answer:${vars.questionId}`); },
  });

  const complete = useMutation({
    mutationFn: async () => {
      // Queued offline ratings must reach the server first, otherwise the word steps look incomplete.
      const flush = await flushReviewQueue((item) => api.submitReview(item));
      if (flush.stoppedBy === 'network') throw { status: 0, message: OFFLINE_TEXT };
      if (flush.stoppedBy === 'server') throw { status: 503, message: SERVER_ERROR_TEXT };
      if (flush.stoppedBy === 'auth') throw { status: 401, message: AUTH_TEXT };
      return api.completeMission(mission.id);
    },
    onSuccess: (result) => {
      setIncompleteSteps([]); setNotice(null);
      storeMission(queryClient, mission.id, (current) => ({ ...current, status: 'completed', result }));
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      void queryClient.invalidateQueries({ queryKey: ['leaderboard'] });
      void queryClient.invalidateQueries({ queryKey: ['entitlement'] });
      void queryClient.invalidateQueries({ queryKey: ['daily-mission'] });
      onCompleted(result);
    },
    onError: (error) => {
      const status = errorStatus(error);
      if (status === 409) {
        const raw = (error as { body?: { incompleteSteps?: unknown } }).body?.incompleteSteps;
        const keys = (Array.isArray(raw) ? raw : []).filter((key): key is MissionStepKey => PRACTICE_STEP_KEYS.includes(key as MissionStepKey));
        setIncompleteSteps(keys);
        // No step keys means the completion window has closed (end of the mission day + 6 hours).
        setNotice(keys.length ? null : { kind: 'error', text: (error as { message?: string }).message || 'Bu görevin tamamlama süresi doldu.' });
        // Re-open those steps locally and resync with the server's view of the mission.
        setCapped((current) => { const next = { ...current }; keys.forEach((key) => { delete next[key]; }); return next; });
        setSkipped({});
        refreshMission();
        return;
      }
      if (status === 404) { setNotice({ kind: 'error', text: 'Görev bulunamadı; yenileniyor.' }); refreshMission(); return; }
      showFailure(error, 'Görev tamamlanamadı. Tekrar dene.');
    },
    onSettled: () => { inFlight.current.delete('complete'); },
  });

  const retryConnection = async () => {
    if (retrying) return;
    setRetrying(true); setNotice(null);
    try { await flushReviewQueue((item) => api.submitReview(item)); await queryClient.invalidateQueries({ queryKey: DAILY_MISSION_QUERY_KEY }); }
    finally { setRetrying(false); }
  };

  const rateWord = (rating: ReviewRating) => {
    if (content.kind !== 'word' || rate.isPending) return;
    const key = `rate:${content.word.id}`;
    if (inFlight.current.has(key)) return;
    inFlight.current.add(key);
    rate.mutate({ stepKey: content.stepKey, wordId: content.word.id, wordIndex: content.index, rating });
  };
  const selectOption = (stepKey: MissionQuestionStepKey, questionId: string, optionKey: string) => {
    if (answer.isPending || feedback) return;
    const key = `answer:${questionId}`;
    if (inFlight.current.has(key)) return;
    inFlight.current.add(key);
    answer.mutate({ stepKey, questionId, optionKey });
  };
  const finish = () => {
    if (complete.isPending || inFlight.current.has('complete')) return;
    inFlight.current.add('complete');
    complete.mutate();
  };
  const nextQuestion = () => { setFeedback(undefined); };
  useEffect(() => { if (incompleteSteps.length) announce('Görevi bitirmek için eksik adımlar var.'); }, [incompleteSteps]);

  const pendingKey = (questionId: string) => (answer.isPending && answer.variables?.questionId === questionId ? answer.variables.optionKey : undefined);
  const currentKey = content.kind !== 'finish' ? content.step.key : undefined;
  const currentIndex = currentKey ? steps.findIndex((step) => step.key === currentKey) + 1 : steps.length;
  const anyCapped = steps.some((step) => capped[step.key] && !step.completed);
  const retryable = notice?.kind === 'offline' || notice?.kind === 'server';

  return <View>
    <View className="mt-1 flex-row items-center justify-between">
      <Text className="flex-1 pr-3 text-muted-foreground">{isPendingMission ? `Dünkü görev · ${formatMissionDate(mission.missionDate)}` : `Yaklaşık ${mission.estimatedMinutes} dakika`}</Text>
      {steps.length ? <Text className="text-sm font-bold text-primary">Adım {Math.min(currentIndex, steps.length)} / {steps.length}</Text> : null}
    </View>
    <Text className="mt-1 text-xs text-muted-foreground">İstediğin an çıkıp sonra kaldığın yerden devam edebilirsin.</Text>
    {steps.length ? <MissionStepper steps={steps} currentKey={currentKey} capped={capped} /> : null}
    {notice ? <MissionNoticeBanner notice={notice} onRetry={retryable ? () => void retryConnection() : undefined} retrying={retrying} onDismiss={() => setNotice(null)} /> : null}
    {incompleteSteps.length ? <View accessibilityRole="alert" className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 p-4"><Text className="font-bold text-amber-800">Görevi bitirmek için eksik adımlar var</Text><Text className="mt-1 text-sm leading-5 text-amber-800">{incompleteSteps.map((key) => { const step = steps.find((item) => item.key === key); return step ? stepLabel(step) : STEP_LABELS[key]; }).join(', ')} adımını tamamla, sonra tekrar dene.</Text></View> : null}
    {anyCapped && !isPremium ? <MissionLimitCard onPremium={onPremium} /> : null}

    {content.kind === 'word' ? <WordStepCard key={currentContentKey} word={content.word} stepKey={content.stepKey} position={content.index + 1} total={content.total} submitting={rate.isPending} onRate={rateWord} /> : null}
    {content.kind === 'recall' ? <ChoiceQuestionCard key={currentContentKey} stepKey="recall" eyebrow={`Hatırlama · ${content.position} / ${content.total}`} heading={content.question.prompt} speakText={content.question.speakText} speakLabel="İngilizce terimi dinle" options={content.question.options} pendingKey={pendingKey(content.question.id)} feedback={feedback?.questionId === content.question.id ? feedback : undefined} isLast={content.isLast} onSelect={(key) => selectOption('recall', content.question.id, key)} onNext={nextQuestion} /> : null}
    {content.kind === 'listening' ? <ChoiceQuestionCard key={currentContentKey} stepKey="listening" eyebrow={`Dinleme · ${content.listening.level}`} heading={content.listening.title || 'Dinle ve cevapla'} prompt={content.listening.prompt} speakText={content.listening.transcript} speakLabel="Dinleme metnini seslendir" options={content.listening.options} pendingKey={pendingKey(content.listening.id)} feedback={feedback?.questionId === content.listening.id ? feedback : undefined} isLast onSelect={(key) => selectOption('listening', content.listening.id, key)} onNext={nextQuestion} /> : null}
    {content.kind === 'finish' ? <View className="mt-6 rounded-3xl border border-border bg-surface p-6">
      <Text className="text-3xl">🏁</Text>
      <Text accessibilityRole="header" className="mt-3 text-2xl font-bold text-foreground">Son adım: görevi tamamla</Text>
      <Text className="mt-2 leading-5 text-muted-foreground">{anyCapped ? 'Günlük sınıra takılan adımlar sınırlı sayılır. Görevi tamamlayıp XP ve serini alabilirsin.' : 'Tüm adımları bitirdin. Görevi tamamla; XP ve serin hesaplansın.'}</Text>
      <Pressable accessibilityRole="button" accessibilityLabel="Görevi tamamla ve XP kazan" accessibilityState={{ disabled: complete.isPending, busy: complete.isPending }} disabled={complete.isPending} onPress={finish} className={`mt-6 items-center rounded-2xl py-4 ${complete.isPending ? 'bg-slate-300' : 'bg-amber-500'}`}><Text className={`text-base font-bold ${complete.isPending ? 'text-slate-600' : 'text-white'}`}>{complete.isPending ? 'Tamamlanıyor…' : 'Görevi tamamla'}</Text></Pressable>
    </View> : null}
  </View>;
}
