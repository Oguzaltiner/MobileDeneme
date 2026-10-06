import { useEffect } from 'react';
import { AccessibilityInfo, ActivityIndicator, Platform, Pressable, Text, View } from 'react-native';

export type MissionNotice = { kind: 'offline' | 'server' | 'queued' | 'error'; text: string } | null;

/** VoiceOver ignores Android-only live regions, so iOS gets an explicit announcement. */
export function announce(text: string) {
  if (Platform.OS === 'ios' && text) AccessibilityInfo.announceForAccessibility(text);
}

/** Inline notice: offline or server error (retryable), queued (saved on device) or a plain error. */
export function MissionNoticeBanner({ notice, onRetry, retrying = false, onDismiss }: { notice: NonNullable<MissionNotice>; onRetry?: () => void; retrying?: boolean; onDismiss?: () => void }) {
  useEffect(() => { announce(notice.text); }, [notice.text]);
  const warn = notice.kind === 'offline' || notice.kind === 'server';
  const tone = notice.kind === 'queued' ? 'bg-blue-50 border-blue-100' : warn ? 'bg-amber-50 border-amber-200' : 'bg-red-50 border-red-100';
  const text = notice.kind === 'queued' ? 'text-blue-800' : warn ? 'text-amber-800' : 'text-red-700';
  return <View accessibilityRole="alert" accessibilityLiveRegion="polite" className={`mt-4 flex-row items-center rounded-2xl border p-4 ${tone}`}>
    <Text className="mr-3 text-lg">{notice.kind === 'queued' ? '💾' : notice.kind === 'offline' ? '📡' : '⚠️'}</Text>
    <Text className={`flex-1 text-sm font-medium leading-5 ${text}`}>{notice.text}</Text>
    {onRetry ? <Pressable accessibilityRole="button" accessibilityLabel="Tekrar dene" accessibilityState={{ disabled: retrying, busy: retrying }} disabled={retrying} onPress={onRetry} className="ml-3 rounded-full bg-white px-3 py-2"><Text className={`text-xs font-bold ${text}`}>{retrying ? '…' : 'Tekrar dene'}</Text></Pressable> : null}
    {!onRetry && onDismiss ? <Pressable accessibilityRole="button" accessibilityLabel="Bildirimi kapat" hitSlop={8} onPress={onDismiss} className="ml-3 px-2 py-1"><Text className={`font-bold ${text}`}>✕</Text></Pressable> : null}
  </View>;
}

/** Shown when the free daily word quota ran out mid-mission: the step is capped, the mission can still finish. */
export function MissionLimitCard({ onPremium }: { onPremium: () => void }) {
  return <View className="mt-4 rounded-3xl bg-foreground p-5">
    <Text className="text-xs font-bold uppercase tracking-widest text-amber-300">Günlük sınır</Text>
    <Text className="mt-2 text-lg font-bold text-white">Bugünkü ücretsiz kelime hakkın doldu</Text>
    <Text className="mt-1 text-sm leading-5 text-white/70">Bu adım sınırlı sayıldı; görevin kalanını tamamlayıp XP kazanabilirsin. Premium ile sınırsız çalış.</Text>
    <Pressable accessibilityRole="button" accessibilityLabel="Premium paketlerini gör" onPress={onPremium} className="mt-4 items-center rounded-2xl bg-amber-500 py-3"><Text className="font-bold text-white">Premium'a geç</Text></Pressable>
  </View>;
}

export function MissionLoading({ text = 'Günün görevi hazırlanıyor…' }: { text?: string }) {
  return <View accessibilityLiveRegion="polite" className="flex-1 items-center justify-center bg-background px-6"><ActivityIndicator size="large" color="#2563EB" /><Text className="mt-4 text-center font-medium text-muted-foreground">{text}</Text></View>;
}

export function MissionLoadError({ status, retrying, onRetry, onBack, paddingBottom }: { status: number; retrying: boolean; onRetry: () => void; onBack: () => void; paddingBottom: number }) {
  const offline = status === 0;
  const title = offline ? 'Bağlantı yok' : status >= 500 ? 'Sunucu hatası' : 'Günün görevi yüklenemedi';
  const body = offline ? 'Günlük görev için internet bağlantısı gerekli. Bağlantını kontrol edip tekrar dene.' : status >= 500 ? 'Sunucu hatası, tekrar dene.' : 'Bir sorun oluştu. Biraz sonra tekrar dene.';
  useEffect(() => { announce(`${title}. ${body}`); }, [title, body]);
  return <View className="flex-1 items-center justify-center bg-background px-6" style={{ paddingBottom }}>
    <View className="w-full rounded-3xl border border-border bg-surface p-6">
      <Text className="text-3xl">{offline ? '📡' : '⚠️'}</Text>
      <Text accessibilityRole="header" className="mt-3 text-xl font-bold text-foreground">{title}</Text>
      <Text accessibilityRole="alert" className="mt-2 leading-5 text-muted-foreground">{body}</Text>
      <Pressable accessibilityRole="button" accessibilityLabel="Tekrar dene" accessibilityState={{ disabled: retrying, busy: retrying }} disabled={retrying} onPress={onRetry} className={`mt-6 items-center rounded-2xl py-4 ${retrying ? 'bg-slate-300' : 'bg-primary'}`}><Text className={`font-bold ${retrying ? 'text-slate-600' : 'text-white'}`}>{retrying ? 'Deneniyor…' : 'Tekrar dene'}</Text></Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel="Ana sayfaya dön" onPress={onBack} className="mt-3 items-center py-2"><Text className="font-semibold text-primary">Ana sayfaya dön</Text></Pressable>
    </View>
  </View>;
}
