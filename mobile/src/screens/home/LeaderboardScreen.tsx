import { useQuery } from '@tanstack/react-query';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function LeaderboardScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const query = useQuery({ queryKey: ['leaderboard', 'weekly'], queryFn: api.weeklyLeaderboard });
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Ligin hazırlanıyor…</Text></View>;
  const data = query.data;
  return <ScrollView className="flex-1 bg-background px-5 pt-14" contentContainerStyle={{ paddingBottom: 48 }}><Pressable onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-7 text-4xl font-bold text-foreground">Haftalık liderlik</Text><Text className="mt-2 text-muted-foreground">{data?.league ?? 'Mavi Lig'} · Bu hafta</Text><View className="mt-6 rounded-3xl bg-primary p-6"><Text className="text-sm font-bold uppercase tracking-widest text-blue-100">SENİN SIRALAMAN</Text><Text className="mt-2 text-5xl font-bold text-white">#{data?.currentUserRank ?? '-'}</Text><Text className="mt-1 text-blue-100">{data?.currentUserPoints ?? 0} XP</Text></View><View className="mt-6 overflow-hidden rounded-3xl border border-border bg-surface">{data?.entries.map(entry => <View key={`${entry.rank}-${entry.displayName}`} className={`flex-row items-center justify-between border-b border-border px-5 py-4 ${entry.isCurrentUser ? 'bg-primary-soft' : ''}`}><View className="flex-row items-center gap-4"><Text className="w-6 text-lg font-bold text-muted-foreground">{entry.rank}</Text><Text className="font-semibold text-foreground">{entry.displayName}{entry.isCurrentUser ? ' · Sen' : ''}</Text></View><Text className="font-bold text-amber-600">{entry.points} XP</Text></View>)}</View></ScrollView>;
}
