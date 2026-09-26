import { useQuery } from '@tanstack/react-query';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { api } from '../../lib/api';

export function WordDetailScreen({ route, navigation }: { route: { params: { id: string } }; navigation: { goBack: () => void } }) {
  const query = useQuery({ queryKey: ['vocabulary', 'word', route.params.id], queryFn: () => api.word(route.params.id) });
  const word = query.data;
  if (query.isLoading) return <View className="flex-1 items-center justify-center bg-background"><Text>Yükleniyor…</Text></View>;
  if (query.isError || !word) return <View className="flex-1 items-center justify-center bg-background px-6"><Pressable onPress={navigation.goBack}><Text className="mb-5 font-semibold text-blue-700">‹ Geri</Text></Pressable><Text className="text-center text-red-700">Kelime detayı yüklenemedi.</Text></View>;
  return <ScrollView className="flex-1 bg-background px-6 pt-16"><Pressable onPress={navigation.goBack} className="mb-6"><Text className="font-semibold text-blue-700">‹ Geri</Text></Pressable><Text className="text-4xl font-bold text-foreground">{word.term}</Text>{word.pronunciation ? <Text className="mt-2 text-blue-700">{word.pronunciation}</Text> : null}<Text className="mt-6 text-2xl font-semibold text-foreground">{word.translation}</Text>{word.definition ? <Text className="mt-5 text-base leading-6 text-muted-foreground">{word.definition}</Text> : null}<View className="mt-8 rounded-2xl bg-white p-5"><Text className="font-bold text-foreground">Örnek cümle</Text>{word.exampleSentence ? <Text className="mt-3 leading-6 text-muted-foreground">{word.exampleSentence}</Text> : <Text className="mt-3 text-muted-foreground">Henüz örnek cümle eklenmemiş.</Text>}</View></ScrollView>;
}
