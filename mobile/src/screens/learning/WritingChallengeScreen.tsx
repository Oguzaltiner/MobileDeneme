import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import { api } from '../../lib/api';

export function WritingChallengeScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const query = useQuery({ queryKey: ['writing-challenge'], queryFn: api.writingChallenge });
  const [answer, setAnswer] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const challenge = query.data;
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Yazma görevi hazırlanıyor…</Text></View>;
  if (query.isError || !challenge) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Yazma görevi yüklenemedi.</Text><Pressable onPress={navigation.goBack} className="mt-6"><Text className="font-bold text-primary">Geri dön</Text></Pressable></View>;
  const correct = answer.trim().toLocaleLowerCase('en-US') === challenge.answer.toLocaleLowerCase('en-US');
  return <ScrollView className="flex-1 bg-background px-6 pt-16" contentContainerStyle={{ paddingBottom: 48 }}><Pressable onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Yazarak hatırla.</Text><Text className="mt-2 text-base leading-6 text-muted-foreground">Tanımı okuyup İngilizce kelimeyi yaz.</Text><View className="mt-8 rounded-3xl bg-surface p-6"><Text className="text-xs font-bold uppercase tracking-widest text-primary">TANIM</Text><Text className="mt-4 text-2xl font-semibold leading-9 text-foreground">{challenge.prompt}</Text><Text className="mt-5 text-sm text-muted-foreground">İpucu: {challenge.hint}</Text><TextInput accessibilityLabel="Cevabını yaz" autoCapitalize="none" editable={!submitted} value={answer} onChangeText={setAnswer} placeholder="İngilizce kelimeyi yaz" placeholderTextColor="#94A3B8" className="mt-7 rounded-2xl border border-border bg-background px-4 py-4 text-lg text-foreground" />{submitted ? <View className={`mt-5 rounded-2xl p-4 ${correct ? 'bg-emerald-50' : 'bg-amber-50'}`}><Text className="text-lg font-bold text-foreground">{correct ? 'Harika, doğru cevap!' : `Doğru cevap: ${challenge.answer}`}</Text><Text className="mt-1 text-muted-foreground">Türkçesi: {challenge.translation}</Text></View> : null}<Pressable disabled={!answer.trim() || submitted} onPress={() => setSubmitted(true)} className={`mt-6 items-center rounded-2xl py-4 ${answer.trim() && !submitted ? 'bg-primary' : 'bg-slate-200'}`}><Text className="font-bold text-white">Cevabı kontrol et</Text></Pressable>{submitted ? <Pressable onPress={navigation.goBack} className="mt-4 items-center"><Text className="font-semibold text-primary">Devam et</Text></Pressable> : null}</View></ScrollView>;
}
