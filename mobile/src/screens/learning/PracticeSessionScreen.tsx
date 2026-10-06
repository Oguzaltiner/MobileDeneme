import { useMutation, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

type Route = 'DailyMission' | 'Learn' | 'SentenceChallenge' | 'MatchingChallenge' | 'WritingChallenge' | 'ConversationPractice' | 'ListeningLab';
type Navigation = { goBack: () => void; navigate: (route: Route) => void };

export function PracticeSessionScreen({ navigation }: { navigation: Navigation }) {
  const query = useQuery({ queryKey: ['practice-plan'], queryFn: api.practicePlan });
  const start = useMutation({ mutationFn: api.startPracticeSession });
  const [sessionId, setSessionId] = useState<string>();
  const begin = async (route: Route, pathKey?: string) => { const session = await start.mutateAsync(pathKey); setSessionId(session.id); navigation.navigate(route); };
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Seans hazırlanıyor…</Text></View>;
  if (query.isError || !query.data) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Kişisel seans yüklenemedi.</Text><Pressable onPress={navigation.goBack} className="mt-6 rounded-2xl bg-primary px-6 py-4"><Text className="font-bold text-white">Geri dön</Text></Pressable></View>;
  const plan = query.data;
  return <ScrollView className="flex-1 bg-background px-5 pt-14" contentContainerStyle={{ paddingBottom: 56 }}><Pressable accessibilityRole="button" accessibilityLabel="Geri dön" onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Kişisel pratik</Text><Text className="mt-2 text-base text-muted-foreground">{plan.pathTitle} · yaklaşık {plan.estimatedMinutes} dakika</Text>{sessionId ? <Text className="mt-3 text-xs font-semibold text-emerald-700">Seans kaydedildi · adımlarını sırayla tamamla</Text> : null}<View className="mt-7 gap-3">{plan.steps.map((step, index) => <View key={step.key} className="rounded-3xl border border-border bg-surface p-5"><View className="flex-row items-center"><View className="mr-4 h-10 w-10 items-center justify-center rounded-full bg-primary-soft"><Text className="font-bold text-primary">{index + 1}</Text></View><View className="flex-1"><Text className="text-lg font-bold text-foreground">{step.title}</Text><Text className="mt-1 text-sm leading-5 text-muted-foreground">{step.description} · {step.estimatedMinutes} dk</Text></View><Pressable disabled={start.isPending} accessibilityRole="button" accessibilityLabel={`${step.title} başlat`} onPress={() => void begin(step.route as Route, plan.pathKey)} className="rounded-xl bg-primary px-3 py-2"><Text className="font-bold text-white">{start.isPending ? '…' : 'Başla'}</Text></Pressable></View></View>)}</View>{start.isError ? <Text accessibilityRole="alert" className="mt-4 text-center text-red-700">Seans kaydedilemedi, tekrar deneyin.</Text> : null}</ScrollView>;
}
