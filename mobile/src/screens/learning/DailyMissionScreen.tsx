import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { PronunciationButton } from '../../components/PronunciationButton';
import { api } from '../../lib/api';
import { createReviewEventId, enqueueReview, flushReviewQueue } from '../../lib/offline-review-queue';

type Step = 'listen' | 'recall' | 'review';
type MissionNavigation = { goBack: () => void; navigate: (route: 'QuizStart') => void };

export function DailyMissionScreen({ navigation }: { navigation: MissionNavigation }) {
  const query = useQuery({ queryKey: ['daily-mission', 'words'], queryFn: () => api.words({}) });
  const words = useMemo(() => (query.data?.items ?? []).slice(0, 5), [query.data?.items]);
  const [index, setIndex] = useState(0);
  const [step, setStep] = useState<Step>('listen');
  const [selectedAnswer, setSelectedAnswer] = useState<string>();
  const [correctAnswers, setCorrectAnswers] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const current = words[index];
  const options = useMemo(() => current ? [current.translation, ...words.filter(x => x.id !== current.id).slice(0, 3).map(x => x.translation)] : [], [current, words]);

  useEffect(() => { void flushReviewQueue(item => api.submitReview(item)); }, []);
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Günün görevi hazırlanıyor…</Text></View>;
  if (query.isError) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="alert" className="text-center text-red-700">Günün görevi yüklenemedi.</Text><Pressable accessibilityRole="button" accessibilityLabel="Geri dön" onPress={navigation.goBack} className="mt-6 rounded-2xl bg-blue-600 px-6 py-4"><Text className="font-bold text-white">Geri dön</Text></Pressable></View>;
  if (!current) return <View className="flex-1 items-center justify-center bg-background px-6"><Text accessibilityRole="header" className="text-center text-3xl font-bold text-foreground">Görev tamamlandı 🎉</Text><Text className="mt-3 text-center text-muted-foreground">Bugünkü kişisel seansını bitirdin. {correctAnswers} / {words.length} kelimeyi doğru hatırladın.</Text><Pressable accessibilityRole="button" accessibilityLabel="Quiz başlat" onPress={() => navigation.navigate('QuizStart')} className="mt-8 w-full items-center rounded-2xl bg-amber-500 py-4"><Text className="font-bold text-white">Mini quiz çöz</Text></Pressable><Pressable accessibilityRole="button" accessibilityLabel="Ana sayfaya dön" onPress={navigation.goBack} className="mt-4"><Text className="font-semibold text-blue-700">Ana sayfaya dön</Text></Pressable></View>;

  const chooseAnswer = (answer: string) => { setSelectedAnswer(answer); if (answer === current.translation) setCorrectAnswers(x => x + 1); };
  const submitReview = async (rating: 0 | 1 | 2 | 3) => {
    setSubmitting(true); setError(''); const item = { wordId: current.id, rating, clientEventId: createReviewEventId(), schemaVersion: 1 as const, retryCount: 0 };
    try { await api.submitReview(item); setIndex(x => x + 1); setStep('listen'); setSelectedAnswer(undefined); }
    catch (e) { const apiError = e as { status?: number; message?: string }; if (apiError.status === 0) { await enqueueReview(item); setIndex(x => x + 1); setStep('listen'); setSelectedAnswer(undefined); } else setError(apiError.message ?? 'Değerlendirme kaydedilemedi.'); }
    finally { setSubmitting(false); }
  };

  return <ScrollView className="flex-1 bg-background px-6 pt-16" contentContainerStyle={{ paddingBottom: 56 }}><View className="flex-row items-center justify-between"><Pressable accessibilityRole="button" accessibilityLabel="Görevden çık" onPress={navigation.goBack}><Text className="font-semibold text-blue-700">‹ Çık</Text></Pressable><Text className="text-sm font-bold text-blue-700">{index + 1} / {words.length}</Text></View><Text accessibilityRole="header" className="mt-8 text-3xl font-bold text-foreground">Günün görevi</Text><Text className="mt-2 text-muted-foreground">Dinle, hatırla, pekiştir.</Text><View className="mt-8 rounded-3xl bg-white p-7"><Text className="text-xs font-bold uppercase tracking-widest text-blue-700">{step === 'listen' ? '1 · Dinle' : step === 'recall' ? '2 · Hatırla' : '3 · Pekiştir'}</Text><Text accessibilityRole="header" className="mt-4 text-4xl font-bold text-foreground">{current.term}</Text><Text className="mt-2 text-sm text-muted-foreground">{current.pronunciation || 'İngilizce kelime'}</Text>{step === 'listen' ? <><View className="mt-6"><PronunciationButton text={current.term} label="Kelimeyi dinle" /></View><Pressable accessibilityRole="button" accessibilityLabel="Anlamı hatırlamaya geç" onPress={() => setStep('recall')} className="mt-8 items-center rounded-2xl bg-blue-600 py-4"><Text className="font-bold text-white">Anlamını hatırlıyorum</Text></Pressable></> : null}{step === 'recall' ? <><Text className="mt-7 text-base font-semibold text-foreground">Bu kelimenin Türkçe anlamı nedir?</Text><View className="mt-4 gap-3">{options.map((option) => <Pressable key={option} accessibilityRole="button" accessibilityLabel={`Cevap: ${option}`} accessibilityState={{ selected: selectedAnswer === option }} onPress={() => chooseAnswer(option)} className={`rounded-2xl border-2 p-4 ${selectedAnswer === option ? option === current.translation ? 'border-emerald-500 bg-emerald-50' : 'border-red-400 bg-red-50' : 'border-transparent bg-slate-50'}`}><Text className="font-semibold text-foreground">{option}</Text></Pressable>)}</View>{selectedAnswer ? <Pressable accessibilityRole="button" accessibilityLabel="Pekiştirme değerlendirmesine geç" onPress={() => setStep('review')} className="mt-6 items-center rounded-2xl bg-blue-600 py-4"><Text className="font-bold text-white">Devam et</Text></Pressable> : null}</> : null}{step === 'review' ? <><Text className="mt-7 text-base font-semibold text-foreground">Bu kelime senin için nasıldı?</Text><View className="mt-4 flex-row gap-2">{([['Tekrar', 0], ['Zor', 1], ['İyi', 2], ['Kolay', 3]] as const).map(([label, rating]) => <Pressable key={label} disabled={submitting} accessibilityRole="button" accessibilityLabel={`${label} olarak değerlendir`} onPress={() => void submitReview(rating)} className="flex-1 items-center rounded-2xl bg-blue-600 py-4"><Text className="font-bold text-white">{label}</Text></Pressable>)}</View></> : null}</View>{error ? <Text accessibilityRole="alert" className="mt-4 text-center text-red-700">{error}</Text> : null}</ScrollView>;
}
