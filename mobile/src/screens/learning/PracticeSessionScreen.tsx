import { useQuery } from '@tanstack/react-query';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

type Route = 'DailyMission' | 'SentenceChallenge' | 'MatchingChallenge' | 'WritingChallenge';
type Navigation = { goBack: () => void; navigate: (route: Route) => void };

export function PracticeSessionScreen({ navigation }: { navigation: Navigation }) {
  const query = useQuery({ queryKey: ['practice-plan'], queryFn: api.practicePlan });
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Seans hazırlanıyor…</Text></View>;
  if (query.isError || !query.data) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Kişisel seans yüklenemedi.</Text><Pressable onPress={navigation.goBack} className="mt-6 rounded-2xl bg-primary px-6 py-4"><Text className="font-bold text-white">Geri dön</Text></Pressable></View>;
  const plan = query.data;
  return <ScrollView className="flex-1 bg-background px-5 pt-14" contentContainerStyle={{ paddingBottom: 56 }}><Pressable accessibilityRole="button" accessibilityLabel="Geri dön" onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Kişisel pratik</Text><Text className="mt-2 text-base text-muted-foreground">{plan.pathTitle} · yaklaşık {plan.estimatedMinutes} dakika</Text><View className="mt-7 gap-3">{plan.steps.map((step, index) => <View key={step.key} className="rounded-3xl border border-border bg-surface p-5"><View className="flex-row items-center"><View className="mr-4 h-10 w-10 items-center justify-center rounded-full bg-primary-soft"><Text className="font-bold text-primary">{index + 1}</Text></View><View className="flex-1"><Text className="text-lg font-bold text-foreground">{step.title}</Text><Text className="mt-1 text-sm leading-5 text-muted-foreground">{step.description} · {step.estimatedMinutes} dk</Text></View><Pressable accessibilityRole="button" accessibilityLabel={`${step.title} başlat`} onPress={() => navigation.navigate(step.route as Route)} className="rounded-xl bg-primary px-3 py-2"><Text className="font-bold text-white">Başla</Text></Pressable></View></View>)}</View></ScrollView>;
}
