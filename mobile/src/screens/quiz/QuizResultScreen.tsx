import { useQuery } from '@tanstack/react-query';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function QuizResultScreen({ route, navigation }: { route: { params: { sessionId: string } }; navigation: { navigate: (route: 'QuizStart') => void; popToTop: () => void } }) {
  const query = useQuery({ queryKey: ['quiz', 'result', route.params.sessionId], queryFn: () => api.completeQuiz(route.params.sessionId) });
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Sonuç hesaplanıyor…</Text></View>;
  if (query.isError || !query.data) return <View className="flex-1 items-center justify-center bg-background px-6"><Text className="text-center text-red-700">Sonuç alınamadı.</Text></View>;
  const result = query.data;
  return <ScrollView className="flex-1 bg-background px-6" contentContainerStyle={{ flexGrow: 1, paddingBottom: 48, justifyContent: 'center' }}><View className="items-center rounded-3xl bg-surface p-7"><Text className="text-xs font-bold uppercase tracking-widest text-primary">QUIZ TAMAMLANDI</Text><Text className="mt-5 text-7xl font-bold text-foreground">%{result.scorePercent}</Text><Text className="mt-3 text-lg text-muted-foreground">{result.correctCount} / {result.questionCount} doğru</Text><View className="mt-6 w-full rounded-2xl bg-primary-soft p-4"><Text className="text-center text-sm font-medium text-primary">Bugünkü öğrenme ritmini koruyorsun.</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Quizi tekrar çöz" onPress={() => navigation.navigate('QuizStart')} className="mt-8 w-full items-center rounded-2xl bg-accent py-4"><Text className="font-bold text-white">Tekrar çöz</Text></Pressable><Pressable accessibilityRole="button" accessibilityLabel="Ana sayfaya dön" onPress={navigation.popToTop} className="mt-4"><Text className="font-semibold text-primary">Ana sayfaya dön</Text></Pressable></View></ScrollView>;
}
