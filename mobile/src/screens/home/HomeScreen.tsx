import { useQuery } from '@tanstack/react-query';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';
import { useAuthStore } from '../../store/auth-store';

export function HomeScreen({ navigation }: { navigation: { navigate: (route: 'Vocabulary' | 'Learn' | 'QuizStart') => void } }) {
  const user = useAuthStore((state) => state.user);
  const signOut = useAuthStore((state) => state.signOut);
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: api.dashboard });
  const data = dashboard.data;
  const progress = data && data.dailyGoal > 0 ? Math.min(1, data.todayProgress / data.dailyGoal) : 0;

  return <ScrollView className="flex-1 bg-background px-6 pt-16" contentContainerStyle={{ paddingBottom: 36 }}>
    <View className="flex-row items-start justify-between"><View><Text className="text-sm text-muted-foreground">Tekrar hoş geldin</Text><Text className="mt-1 text-3xl font-bold text-foreground">{user?.displayName || user?.email?.split('@')[0] || 'Öğrenci'}</Text></View><Pressable onPress={() => void signOut()}><Text className="text-blue-700">Çıkış</Text></Pressable></View>
    {dashboard.isError ? <Text className="mt-5 rounded-xl bg-red-50 p-3 text-red-700">İstatistikler yüklenemedi.</Text> : null}
    <View className="mt-8 rounded-3xl bg-blue-600 p-6"><Text className="text-sm text-blue-100">Bugünün hedefi</Text><Text className="mt-2 text-4xl font-bold text-white">{data?.todayProgress ?? 0} / {data?.dailyGoal ?? '—'}</Text><View className="mt-4 h-2 rounded-full bg-blue-400"><View className="h-2 rounded-full bg-white" style={{ width: `${progress * 100}%` }} /></View><Text className="mt-3 text-blue-100">Kelime çalışmaya devam et.</Text></View>
    <View className="mt-5 flex-row gap-3"><View className="flex-1 rounded-2xl bg-white p-4"><Text className="text-2xl font-bold text-foreground">{data?.dueReviewCount ?? 0}</Text><Text className="mt-1 text-xs text-muted-foreground">Bekleyen tekrar</Text></View><View className="flex-1 rounded-2xl bg-white p-4"><Text className="text-2xl font-bold text-foreground">{data?.totalWordsLearned ?? 0}</Text><Text className="mt-1 text-xs text-muted-foreground">Öğrenilen kelime</Text></View></View>
    <Pressable onPress={() => navigation.navigate('Learn')} className="mt-8 rounded-2xl bg-foreground p-5"><Text className="text-lg font-bold text-white">Bugünkü çalışmaya başla</Text><Text className="mt-1 text-white/70">Kısa bir tekrar ile ilerlemeni koru.</Text></Pressable>
    <Pressable onPress={() => navigation.navigate('QuizStart')} className="mt-4 rounded-2xl bg-amber-500 p-5"><Text className="text-lg font-bold text-white">Quiz çöz</Text><Text className="mt-1 text-white/80">4 seçenekli mini sınavla kendini ölç.</Text></Pressable>
    <Pressable onPress={() => navigation.navigate('Vocabulary')} className="mt-4 rounded-2xl bg-white p-5"><Text className="text-lg font-bold text-foreground">Kelime kataloğu</Text><Text className="mt-1 text-muted-foreground">Seviyene uygun kelimeleri keşfet.</Text></Pressable>
  </ScrollView>;
}
