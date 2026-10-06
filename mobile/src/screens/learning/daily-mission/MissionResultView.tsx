import { Pressable, Text, View } from 'react-native';
import type { DailyMissionToday, MissionResult } from '../../../lib/api';
import { formatMissionDate } from './queries';

function StatTile({ value, label, tone = 'default' }: { value: string; label: string; tone?: 'default' | 'amber' | 'emerald' }) {
  const box = tone === 'amber' ? 'border-amber-200 bg-amber-50' : tone === 'emerald' ? 'border-emerald-200 bg-emerald-50' : 'border-border bg-surface';
  const text = tone === 'amber' ? 'text-amber-700' : tone === 'emerald' ? 'text-emerald-700' : 'text-foreground';
  return <View accessible accessibilityLabel={`${label}: ${value}`} className={`flex-1 rounded-2xl border p-4 ${box}`}><Text className={`text-2xl font-bold ${text}`}>{value}</Text><Text className={`mt-1 text-xs font-medium ${tone === 'default' ? 'text-muted-foreground' : text}`}>{label}</Text></View>;
}

function HomeButton({ onHome, secondary = false }: { onHome: () => void; secondary?: boolean }) {
  return <Pressable accessibilityRole="button" accessibilityLabel="Ana sayfaya dön" onPress={onHome} className={`items-center rounded-2xl py-4 ${secondary ? 'mt-3 border border-border bg-surface' : 'mt-6 bg-primary'}`}><Text className={`font-bold ${secondary ? 'text-primary' : 'text-white'}`}>Ana sayfaya dön</Text></Pressable>;
}

/** Completion screen: XP with breakdown, mission streak, accuracy and tomorrow's preview. */
export function MissionResultView({ result, onHome, onContinueToday }: { result: MissionResult; onHome: () => void; /** Set after finishing yesterday's pending mission. */ onContinueToday?: () => void }) {
  // A re-opened or repeated completion must not celebrate a streak extension again.
  const extended = result.streak.extended && !result.alreadyCompleted;
  const accuracy = result.totalAnswers > 0 ? Math.round((result.correctAnswers / result.totalAnswers) * 100) : null;
  const breakdown = [
    { label: 'Görev tabanı', value: result.xpBreakdown.base },
    { label: 'Kelimeler', value: result.xpBreakdown.items },
    { label: 'Doğru cevaplar', value: result.xpBreakdown.accuracy },
    { label: 'Seri bonusu', value: result.xpBreakdown.streakBonus },
  ];
  return <View>
    <View className="mt-6 overflow-hidden rounded-3xl bg-foreground p-6">
      <Text className="text-xs font-bold uppercase tracking-widest text-blue-200">{onContinueToday ? 'Dünkü görev tamamlandı' : 'Görev tamamlandı'}</Text>
      <Text accessibilityRole="header" className="mt-3 text-3xl font-bold text-white">Harika iş! 🎉</Text>
      <Text accessibilityLabel={`${result.xpAwarded} XP kazandın`} className="mt-4 text-5xl font-bold text-amber-300">+{result.xpAwarded} <Text className="text-2xl text-white/60">XP</Text></Text>
      {result.alreadyCompleted ? <Text className="mt-2 text-xs text-white/55">Bu görevi daha önce tamamladın; XP yalnızca bir kez verilir.</Text> : null}
      <View className="mt-5 gap-2 rounded-2xl bg-white/10 p-4">
        {breakdown.map((item) => <View key={item.label} accessible accessibilityLabel={`${item.label}: ${item.value} XP`} className="flex-row items-center justify-between"><Text className="text-sm text-white/75">{item.label}</Text><Text className="text-sm font-bold text-white">+{item.value}</Text></View>)}
      </View>
    </View>
    <View className="mt-4 flex-row gap-2">
      <StatTile tone="amber" value={`${result.streak.current}🔥`} label={extended ? 'Seri uzadı!' : 'Görev serisi'} />
      <StatTile value={`${result.streak.longest}`} label="En uzun seri" />
      <StatTile tone="emerald" value={accuracy === null ? '—' : `%${accuracy}`} label={`Doğruluk${result.totalAnswers > 0 ? ` · ${result.correctAnswers}/${result.totalAnswers}` : ''}`} />
    </View>
    <View className="mt-2 flex-row gap-2">
      <StatTile value={`${result.reviewedWords}`} label="Tekrar edilen" />
      <StatTile value={`${result.newWords}`} label="Yeni kelime" />
    </View>
    <View className="mt-4 rounded-3xl bg-primary-soft p-5">
      <Text className="text-xs font-bold uppercase tracking-widest text-primary">Yarın · {formatMissionDate(result.tomorrow.date)}</Text>
      <Text className="mt-2 text-lg font-bold text-foreground">Yaklaşık {result.tomorrow.estimatedMinutes} dakikalık görev</Text>
      <Text className="mt-1 text-sm leading-5 text-muted-foreground">{result.tomorrow.dueReviewCount} tekrar · {result.tomorrow.newWordCount} yeni kelime seni bekliyor. Seriyi korumak için yarın tekrar gel.</Text>
    </View>
    {onContinueToday ? <Pressable accessibilityRole="button" accessibilityLabel="Bugünün görevine geç" onPress={onContinueToday} className="mt-6 items-center rounded-2xl bg-amber-500 py-4"><Text className="font-bold text-white">Bugünün görevine geç</Text></Pressable> : null}
    <HomeButton onHome={onHome} secondary={Boolean(onContinueToday)} />
  </View>;
}

/** Completed today but no result payload available (e.g. reopened later): show the streak only. */
export function MissionCompletedCard({ today, onHome }: { today: DailyMissionToday; onHome: () => void }) {
  return <View>
    <View className="mt-6 rounded-3xl bg-foreground p-6">
      <Text className="text-xs font-bold uppercase tracking-widest text-blue-200">{formatMissionDate(today.missionDate)}</Text>
      <Text accessibilityRole="header" className="mt-3 text-3xl font-bold text-white">Bugünün görevi tamam ✓</Text>
      <Text className="mt-2 leading-5 text-white/70">XP'n hesabına eklendi. Yarın yeni bir görev seni bekliyor.</Text>
    </View>
    <View className="mt-4 flex-row gap-2">
      <StatTile tone="amber" value={`${today.streak.current}🔥`} label="Görev serisi" />
      <StatTile value={`${today.streak.longest}`} label="En uzun seri" />
    </View>
    <HomeButton onHome={onHome} />
  </View>;
}
