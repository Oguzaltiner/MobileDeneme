import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function MatchingChallengeScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const query = useQuery({ queryKey: ['matching-challenge'], queryFn: api.matchingChallenge });
  const [selected, setSelected] = useState<string>();
  const challenge = query.data;
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Eşleştirme hazırlanıyor…</Text></View>;
  if (query.isError || !challenge) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Eşleştirme yüklenemedi.</Text><Pressable onPress={navigation.goBack} className="mt-6"><Text className="font-bold text-primary">Geri dön</Text></Pressable></View>;
  const answered = selected !== undefined;
  const correct = selected === challenge.answer;
  return <ScrollView className="flex-1 bg-background px-6 pt-16" contentContainerStyle={{ paddingBottom: 48 }}><Pressable onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Eşleştir.</Text><Text className="mt-2 text-base leading-6 text-muted-foreground">Kelimeyi doğru anlamıyla eşleştir.</Text><View className="mt-8 items-center rounded-3xl bg-foreground p-8"><Text className="text-xs font-bold uppercase tracking-widest text-blue-200">KELİME</Text><Text className="mt-4 text-5xl font-bold text-white">{challenge.term}</Text></View><View className="mt-6 gap-3">{challenge.options.map(option => <Pressable key={option} disabled={answered} onPress={() => setSelected(option)} className={`rounded-2xl border-2 p-5 ${selected === option ? option === challenge.answer ? 'border-emerald-500 bg-emerald-50' : 'border-red-400 bg-red-50' : 'border-border bg-surface'}`}><Text className="text-lg font-semibold text-foreground">{option}</Text></Pressable>)}</View>{answered ? <Pressable onPress={navigation.goBack} className="mt-6 items-center rounded-2xl bg-primary py-4"><Text className="font-bold text-white">{correct ? 'Doğru · Devam et' : `Doğru cevap: ${challenge.answer}`}</Text></Pressable> : null}</ScrollView>;
}
