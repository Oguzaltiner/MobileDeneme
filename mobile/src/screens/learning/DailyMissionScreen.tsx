import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useRef, useState, type ReactNode } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { type DailyMissionToday, type MissionResult } from '../../lib/api';
import { MissionLoadError, MissionLoading, MissionNoticeBanner, type MissionNotice } from './daily-mission/MissionBanners';
import { MissionFlow } from './daily-mission/MissionFlow';
import { MissionIntro } from './daily-mission/MissionIntro';
import { MissionCompletedCard, MissionResultView } from './daily-mission/MissionResultView';
import { DAILY_MISSION_QUERY_KEY, errorStatus, fetchTodayMission, formatMissionDate, isOpenMission, missionProgress, retryableNotice, startTodayMission } from './daily-mission/queries';

type MissionNavigation = { goBack: () => void; navigate: (route: 'Premium') => void };
type PendingChoice = 'undecided' | 'resume' | 'skip';

/**
 * Daily mission: yesterday's unfinished mission (if any) is offered first, then today's.
 * The per-mission flow lives in `MissionFlow`; this screen picks which mission/state to show.
 */
export function DailyMissionScreen({ navigation }: { navigation: MissionNavigation }) {
  const insets = useSafeAreaInsets();
  const queryClient = useQueryClient();
  const today = useQuery({ queryKey: DAILY_MISSION_QUERY_KEY, queryFn: fetchTodayMission });
  const [started, setStarted] = useState(false);
  const [pendingChoice, setPendingChoice] = useState<PendingChoice>('undecided');
  const [result, setResult] = useState<{ value: MissionResult; wasPending: boolean } | null>(null);
  const [notice, setNotice] = useState<MissionNotice>(null);
  const scrollRef = useRef<ScrollView>(null);
  const scrollToTop = useCallback(() => { scrollRef.current?.scrollTo({ y: 0, animated: true }); }, []);

  const start = useMutation({
    mutationFn: startTodayMission,
    onSuccess: (fresh) => {
      queryClient.setQueryData<DailyMissionToday>(DAILY_MISSION_QUERY_KEY, (old) => (old ? { ...old, mission: fresh, status: fresh.status } : old));
      setStarted(true); setNotice(null);
    },
    onError: (error) => { setNotice(retryableNotice(error) ?? { kind: 'error', text: errorStatus(error) === 400 ? 'Saat dilimin doğrulanamadı. Cihazının tarih ve saat ayarlarını kontrol et.' : 'Görev başlatılamadı. Biraz sonra tekrar dene.' }); scrollToTop(); },
  });

  // Insets keep the header clear of the notch and the last button above the home indicator / gesture bar.
  const contentStyle = { paddingTop: Math.max(insets.top + 12, 56), paddingBottom: insets.bottom + 40, paddingHorizontal: 20 };
  if (today.isLoading) return <MissionLoading />;
  const data = today.data;
  if (!data) return <MissionLoadError status={errorStatus(today.error)} retrying={today.isFetching} onRetry={() => void today.refetch()} onBack={navigation.goBack} paddingBottom={insets.bottom} />;

  const pending = isOpenMission(data.pendingMission) && pendingChoice !== 'skip' ? data.pendingMission : null;
  const todayMission = data.mission ?? null;
  const todayCompleted = data.status === 'completed' || todayMission?.status === 'completed';
  const todayInFlow = isOpenMission(todayMission) && (started || data.status === 'inProgress');
  const inFlow = !result && (pending ? pendingChoice === 'resume' : todayInFlow && !todayCompleted);
  const refreshNotice: MissionNotice = today.isError ? retryableNotice(today.error) ?? { kind: 'error', text: 'Görev güncellenemedi.' } : null;
  const visibleNotice = notice ?? refreshNotice;
  const completeFlow = (value: MissionResult, wasPending: boolean) => { setResult({ value, wasPending }); if (wasPending) setPendingChoice('skip'); scrollToTop(); };
  const goPremium = () => navigation.navigate('Premium');

  let body: ReactNode;
  if (result) {
    body = <MissionResultView result={result.value} onHome={navigation.goBack} onContinueToday={result.wasPending && !todayCompleted ? () => { setResult(null); scrollToTop(); } : undefined} />;
  } else if (pending && pendingChoice === 'resume') {
    body = <MissionFlow key={pending.id} mission={pending} isPremium={data.limits.isPremium} isPendingMission onCompleted={(value) => completeFlow(value, true)} onPremium={goPremium} scrollToTop={scrollToTop} />;
  } else if (pending) {
    const progress = missionProgress(pending);
    body = <View className="mt-6 rounded-3xl bg-foreground p-6">
      <Text className="text-xs font-bold uppercase tracking-widest text-amber-300">Yarım kalan görev · {formatMissionDate(pending.missionDate)}</Text>
      <Text accessibilityRole="header" className="mt-3 text-2xl font-bold text-white">Dünkü görevini bitir</Text>
      <Text className="mt-2 leading-5 text-white/70">Dünkü görevin hâlâ tamamlanabilir. Bitir, XP'ni ve serini al; sonra bugünün görevine geç.</Text>
      <View className="mt-4 h-2 overflow-hidden rounded-full bg-white/15"><View className="h-2 rounded-full bg-amber-400" style={{ width: `${progress.required > 0 ? Math.min(1, progress.done / progress.required) * 100 : 0}%` }} /></View>
      <Text className="mt-2 text-xs text-white/60">{progress.done} / {progress.required} tamamlandı</Text>
      <Pressable accessibilityRole="button" accessibilityLabel="Dünkü görevini bitir" onPress={() => { setPendingChoice('resume'); scrollToTop(); }} className="mt-5 items-center rounded-2xl bg-amber-500 py-4"><Text className="font-bold text-white">Dünkü görevini bitir</Text></Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel="Şimdilik atla ve bugünün görevine geç" onPress={() => { setPendingChoice('skip'); scrollToTop(); }} className="mt-3 items-center py-2"><Text className="font-semibold text-white/80">Şimdilik atla, bugünün görevine geç</Text></Pressable>
    </View>;
  } else if (todayCompleted) {
    body = todayMission?.result ? <MissionResultView result={todayMission.result} onHome={navigation.goBack} /> : <MissionCompletedCard today={data} onHome={navigation.goBack} />;
  } else if (todayInFlow && todayMission) {
    body = <MissionFlow key={todayMission.id} mission={todayMission} isPremium={data.limits.isPremium} isPendingMission={false} onCompleted={(value) => completeFlow(value, false)} onPremium={goPremium} scrollToTop={scrollToTop} />;
  } else {
    body = <MissionIntro today={data} starting={start.isPending} onStart={() => { if (!start.isPending) start.mutate(); }} onPremium={goPremium} />;
  }

  return <ScrollView ref={scrollRef} className="flex-1 bg-background" contentContainerStyle={contentStyle}>
    <View className="flex-row items-center justify-between">
      <Pressable accessibilityRole="button" accessibilityLabel={inFlow ? 'Görevden çık. İlerlemen kaydedilir, sonra devam edebilirsin.' : 'Geri dön'} hitSlop={8} onPress={navigation.goBack} className="rounded-full bg-surface px-4 py-2"><Text className="font-semibold text-primary">‹ {inFlow ? 'Çık' : 'Geri'}</Text></Pressable>
    </View>
    <Text accessibilityRole="header" className="mt-6 text-3xl font-bold text-foreground">{pending && !result ? 'Dünkü görev' : 'Günün görevi'}</Text>
    {visibleNotice ? <MissionNoticeBanner notice={visibleNotice} onRetry={visibleNotice.kind === 'offline' || visibleNotice.kind === 'server' ? () => { setNotice(null); void today.refetch(); } : undefined} retrying={today.isFetching} onDismiss={() => setNotice(null)} /> : null}
    {body}
  </ScrollView>;
}
