import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function SentenceChallengeScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const query = useQuery({ queryKey: ['sentence-challenge'], queryFn: api.sentenceChallenge });
  const [selected, setSelected] = useState<string>();
  const challenge = query.data;
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Cümle hazırlanıyor…</Text></View>;
  if (query.isError || !challenge) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Cümle challenge yüklenemedi.</Text><Pressable onPress={navigation.goBack} className="mt-6"><Text className="font-bold text-primary">Geri dön</Text></Pressable></View>;
  const answered = selected !== undefined;
  const correct = selected === challenge.answer;
  return <ScrollView className="flex-1 bg-background px-6 pt-16" contentContainerStyle={{ paddingBottom: 48 }}><Pressable accessibilityRole="button" accessibilityLabel="Geri dön" onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Cümleyi tamamla.</Text><Text className="mt-2 text-muted-foreground">Bağlamı kullan, doğru kelimeyi seç.</Text><View className="mt-8 rounded-3xl bg-surface p-6"><Text className="text-2xl font-semibold leading-10 text-foreground">{challenge.sentence}</Text><View className="mt-7 gap-3">{challenge.options.map(option => <Pressable key={option} accessibilityRole="button" accessibilityState={{ selected: selected === option }} onPress={() => !answered && setSelected(option)} className={`rounded-2xl border-2 p-4 ${selected === option ? option === challenge.answer ? 'border-emerald-500 bg-emerald-50' : 'border-red-400 bg-red-50' : 'border-border bg-background'}`}><Text className="text-lg font-semibold text-foreground">{option}</Text></Pressable>)}</View>{answered ? <View className={`mt-6 rounded-2xl p-4 ${correct ? 'bg-emerald-50' : 'bg-amber-50'}`}><Text className="text-lg font-bold text-foreground">{correct ? 'Mükemmel!' : `Doğru cevap: ${challenge.answer}`}</Text><Text className="mt-1 text-muted-foreground">{challenge.translation} · {challenge.explanation}</Text></View> : null}<Pressable disabled={!answered} onPress={() => navigation.goBack()} className={`mt-6 items-center rounded-2xl py-4 ${answered ? 'bg-primary' : 'bg-slate-200'}`}><Text className="font-bold text-white">Devam</Text></Pressable></View></ScrollView>;
}
