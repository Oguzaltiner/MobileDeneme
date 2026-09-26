import { useQuery } from '@tanstack/react-query';
import { Pressable, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function QuizResultScreen({ route, navigation }: { route: { params: { sessionId: string } }; navigation: { navigate: (route: 'QuizStart') => void; goBack: () => void } }) {
  const query = useQuery({ queryKey: ['quiz', 'result', route.params.sessionId], queryFn: () => api.completeQuiz(route.params.sessionId) });
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Sonuç hesaplanıyor…</Text></View>;
  if (query.isError || !query.data) return <View className="flex-1 items-center justify-center bg-background px-6"><Text className="text-center text-red-700">Sonuç alınamadı.</Text></View>;
  const result = query.data;
  return <View className="flex-1 items-center justify-center bg-background px-6"><Text className="text-sm font-bold text-blue-700">QUIZ TAMAMLANDI</Text><Text className="mt-5 text-6xl font-bold text-foreground">%{result.scorePercent}</Text><Text className="mt-3 text-lg text-muted-foreground">{result.correctCount} / {result.questionCount} doğru</Text><Pressable onPress={() => navigation.navigate('QuizStart')} className="mt-10 w-full items-center rounded-2xl bg-amber-500 py-4"><Text className="font-bold text-white">Tekrar çöz</Text></Pressable><Pressable onPress={navigation.goBack} className="mt-4"><Text className="font-semibold text-blue-700">Geri dön</Text></Pressable></View>;
}
