import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function QuizQuestionScreen({ route, navigation }: { route: { params: { sessionId: string } }; navigation: { navigate: (route: 'QuizResult', params: { sessionId: string }) => void } }) {
  const query = useQuery({ queryKey: ['quiz', route.params.sessionId], queryFn: () => api.getQuiz(route.params.sessionId) });
  const [index, setIndex] = useState(0); const [selected, setSelected] = useState<string>(); const [answering, setAnswering] = useState(false); const [feedback, setFeedback] = useState<string>();
  const question = query.data?.questions[index];
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Quiz hazırlanıyor…</Text></View>;
  if (query.isError || !question) return <View className="flex-1 items-center justify-center bg-background px-6"><Text className="text-center text-red-700">Quiz yüklenemedi.</Text></View>;
  const answer = async (key: string) => { if (selected || answering) return; setSelected(key); setAnswering(true); try { const result = await api.answerQuiz(route.params.sessionId, question.id, key); setFeedback(result.isCorrect ? 'Doğru cevap!' : `Yanlış. Doğru cevap: ${result.correctOptionKey}`); } catch (e) { setSelected(undefined); setFeedback((e as { message?: string }).message ?? 'Cevap gönderilemedi.'); } finally { setAnswering(false); } };
  const next = () => { if (index >= (query.data?.questions.length ?? 1) - 1) navigation.navigate('QuizResult', { sessionId: route.params.sessionId }); else { setIndex(index + 1); setSelected(undefined); setFeedback(undefined); } };
  return <View className="flex-1 bg-background px-6 pt-16"><Text className="text-sm font-bold text-blue-700">SORU {index + 1} / {query.data?.questionCount}</Text><Text className="mt-8 text-2xl font-bold text-foreground">{question.prompt}</Text><View className="mt-8 gap-3">{question.options.map((option) => <Pressable key={option.key} disabled={Boolean(selected)} onPress={() => void answer(option.key)} className={`rounded-2xl border p-4 ${selected === option.key ? 'border-blue-600 bg-blue-50' : 'border-transparent bg-white'}`}><Text className="font-semibold text-foreground">{option.key}. {option.text}</Text></Pressable>)}</View>{feedback ? <Text className={`mt-6 font-bold ${feedback.startsWith('Doğru') ? 'text-green-700' : 'text-red-700'}`}>{feedback}</Text> : null}{selected ? <Pressable onPress={next} className="mt-8 items-center rounded-2xl bg-blue-600 py-4"><Text className="font-bold text-white">{index >= (query.data?.questions.length ?? 1) - 1 ? 'Sonucu gör' : 'Sonraki soru'}</Text></Pressable> : null}</View>;
}
