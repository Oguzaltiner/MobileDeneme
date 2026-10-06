import { Pressable, Text, View } from 'react-native';
import type { DailyMissionToday } from '../../../lib/api';
import { formatMissionDate } from './queries';

/** Pre-start overview: what today's mission contains, streak and free quota. */
export function MissionIntro({ today, starting, onStart, onPremium }: { today: DailyMissionToday; starting: boolean; onStart: () => void; onPremium: () => void }) {
  const { preview, streak, limits } = today;
  const rows = [
    { icon: '🔁', title: 'Tekrar', detail: preview.reviewCount > 0 ? `${preview.reviewCount} kelime` : 'Bugün tekrar yok' },
    { icon: '✨', title: 'Yeni kelimeler', detail: preview.newWordCount > 0 ? `${preview.newWordCount} kelime` : 'Bugün yeni kelime yok' },
    { icon: '🧠', title: 'Hatırlama', detail: '4 şıklı sorular' },
    ...(preview.hasListening ? [{ icon: '🎧', title: 'Dinleme', detail: '1 soru' }] : []),
  ];
  const quotaLeft = !limits.isPremium && limits.dailyWordsRemaining >= 0 ? limits.dailyWordsRemaining : null;
  const label = today.status === 'inProgress' ? 'Devam et' : 'Görevi başlat';
  return <View>
    <View className="mt-6 overflow-hidden rounded-3xl bg-foreground p-6">
      <View className="flex-row items-center justify-between"><Text className="text-xs font-bold uppercase tracking-widest text-blue-200">{formatMissionDate(today.missionDate)}</Text><Text className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white">~{preview.estimatedMinutes} dk</Text></View>
      <Text accessibilityRole="header" className="mt-4 text-3xl font-bold text-white">Bugünün kişisel görevi</Text>
      <Text className="mt-2 leading-5 text-white/70">Tekrar et, yeni kelimeler öğren, hatırla ve dinle. Tek seferde yaklaşık {preview.estimatedMinutes} dakika.</Text>
      <View className="mt-5 flex-row items-center rounded-2xl bg-white/10 p-4"><Text className="text-2xl">🔥</Text><View className="ml-3 flex-1"><Text className="font-bold text-white">{streak.current} günlük görev serisi</Text><Text className="text-xs text-white/60">En uzun serin: {streak.longest} gün</Text></View></View>
    </View>
    <View className="mt-4 rounded-3xl border border-border bg-surface p-5">
      <Text className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Görev adımları</Text>
      {rows.map((row, index) => <View key={row.title} accessible accessibilityLabel={`${index + 1}. adım, ${row.title}: ${row.detail}`} className={`flex-row items-center py-3 ${index > 0 ? 'border-t border-border' : ''}`}>
        <View className="h-10 w-10 items-center justify-center rounded-2xl bg-primary-soft"><Text className="text-lg">{row.icon}</Text></View>
        <View className="ml-3 flex-1"><Text className="font-bold text-foreground">{row.title}</Text><Text className="text-xs text-muted-foreground">{row.detail}</Text></View>
        <Text className="text-xs font-bold text-muted-foreground">{index + 1}</Text>
      </View>)}
    </View>
    {quotaLeft !== null ? <View className={`mt-4 rounded-2xl border p-4 ${quotaLeft === 0 ? 'border-amber-200 bg-amber-50' : 'border-border bg-surface'}`}>
      <Text className={`text-sm font-medium leading-5 ${quotaLeft === 0 ? 'text-amber-800' : 'text-muted-foreground'}`}>{quotaLeft === 0 ? 'Bugünkü ücretsiz kelime hakkın doldu. Görevi yine tamamlayabilirsin; kelime adımları sınırlı sayılır.' : `Bugün kalan ücretsiz kelime hakkın: ${quotaLeft}. Görev bu hakka göre boyutlandı.`}</Text>
      {quotaLeft === 0 ? <Pressable accessibilityRole="button" accessibilityLabel="Premium paketlerini gör" onPress={onPremium} className="mt-3 self-start rounded-full bg-amber-500 px-4 py-2"><Text className="text-sm font-bold text-white">Premium ile sınırsız çalış</Text></Pressable> : null}
    </View> : null}
    <Pressable accessibilityRole="button" accessibilityLabel={label} accessibilityState={{ disabled: starting, busy: starting }} disabled={starting} onPress={onStart} className={`mt-6 items-center rounded-2xl py-4 ${starting ? 'bg-slate-300' : 'bg-primary'}`}><Text className={`text-base font-bold ${starting ? 'text-slate-600' : 'text-white'}`}>{starting ? 'Görev hazırlanıyor…' : label}</Text></Pressable>
  </View>;
}
