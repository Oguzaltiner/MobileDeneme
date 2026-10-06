import { useQuery } from '@tanstack/react-query';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import { DAILY_MISSION_QUERY_KEY, fetchTodayMission, isOpenMission, missionProgress } from './queries';

/**
 * Home hero for the daily mission (GET /missions/today). Yesterday's unfinished mission is offered first.
 * Tapping always opens the mission screen, which decides what to show.
 */
export function MissionHomeCard({ onOpen }: { onOpen: () => void }) {
  const today = useQuery({ queryKey: DAILY_MISSION_QUERY_KEY, queryFn: fetchTodayMission });
  const data = today.data;
  const pending = isOpenMission(data?.pendingMission) ? data?.pendingMission ?? null : null;
  const mission = pending ?? data?.mission;
  const status = data?.status;
  const done = !pending && status === 'completed';
  const minutes = mission?.estimatedMinutes ?? data?.preview.estimatedMinutes;
  const totals = mission ? missionProgress(mission) : undefined;
  const progress = done ? 1 : totals && totals.required > 0 ? totals.done / totals.required : 0;
  const cta = pending ? 'Dünkü görevi bitir' : done ? 'Tamamlandı ✓' : status === 'inProgress' ? 'Devam et' : 'Başla';
  const title = pending ? 'Dünkü görevini bitir' : done ? 'Bugünü tamamladın' : status === 'inProgress' ? 'Kaldığın yerden devam et' : 'Bugünkü seansın hazır';
  const subtitle = today.isError && !data ? 'Görev şu an yüklenemedi. Açmak için dokun.'
    : !data ? 'Bugünün görevi hazırlanıyor…'
    : pending ? 'Dünkü görevin hâlâ tamamlanabilir. Önce onu bitir, sonra bugünün görevine geç.'
    : done ? 'Bugünkü görevini bitirdin. Yarın yeni görev seni bekliyor.'
    : `${data.preview.reviewCount} tekrar · ${data.preview.newWordCount} yeni kelime${data.preview.hasListening ? ' · dinleme' : ''}`;
  const a11y = `Günün görevi. ${cta}. ${minutes ? `Yaklaşık ${minutes} dakika. ` : ''}${subtitle}${data ? ` Görev serisi ${data.streak.current} gün.` : ''}`;
  const tone = pending ? 'bg-foreground' : done ? 'bg-emerald-700' : 'bg-primary';
  return <Pressable accessibilityRole="button" accessibilityLabel={a11y} onPress={onOpen} className={`mt-7 overflow-hidden rounded-3xl p-6 ${tone}`}>
    <View className="flex-row items-center justify-between">
      <Text className={`text-xs font-bold uppercase tracking-widest ${pending ? 'text-amber-300' : 'text-white/80'}`}>{pending ? 'Yarım kalan görev' : 'Günün görevi'}</Text>
      <View className="flex-row items-center gap-2">
        {minutes ? <Text className="rounded-full bg-white/15 px-3 py-1 text-xs font-semibold text-white">~{minutes} dk</Text> : null}
        {data ? <Text className="rounded-full bg-white/15 px-3 py-1 text-xs font-semibold text-white">{data.streak.current}🔥</Text> : null}
      </View>
    </View>
    <Text className="mt-3 text-2xl font-bold text-white">{title}</Text>
    <Text className="mt-1 text-sm leading-5 text-white/75">{subtitle}</Text>
    {pending || status === 'inProgress' || done ? <View className="mt-4 h-2 overflow-hidden rounded-full bg-white/20"><View className={`h-2 rounded-full ${pending ? 'bg-amber-400' : 'bg-white'}`} style={{ width: `${Math.min(1, progress) * 100}%` }} /></View> : null}
    <View className="mt-5 flex-row items-center justify-between">
      {data ? <Text className="flex-1 pr-3 text-xs font-medium text-white/70">En uzun seri: {data.streak.longest} gün</Text> : <View />}
      <View className={`flex-row items-center rounded-2xl px-5 py-3 ${done ? 'bg-white/15' : pending ? 'bg-amber-500' : 'bg-white'}`}>
        {today.isLoading ? <ActivityIndicator size="small" color="#2563EB" /> : <Text className={`font-bold ${done || pending ? 'text-white' : 'text-primary'}`}>{cta}{done ? '' : '  →'}</Text>}
      </View>
    </View>
  </Pressable>;
}
