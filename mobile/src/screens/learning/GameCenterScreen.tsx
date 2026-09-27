import { Pressable, ScrollView, Text, View } from 'react-native';

type GameRoute = 'QuizStart' | 'SentenceChallenge' | 'MatchingChallenge' | 'WritingChallenge' | 'ListeningLab';
const games: { title: string; description: string; icon: string; route: GameRoute }[] = [
  { title: 'Word Arena', description: '4 şıklı hızlı quiz ile kelime bilgini test et.', icon: '⚔️', route: 'QuizStart' },
  { title: 'Sentence Builder', description: 'Cümleyi tamamla ve İngilizce kelime sırasını öğren.', icon: '🧩', route: 'SentenceChallenge' },
  { title: 'Memory Match', description: 'İngilizce kelimeyi doğru anlamla eşleştir.', icon: '🔗', route: 'MatchingChallenge' },
  { title: 'Writing Sprint', description: 'İpucundan doğru kelimeyi yazarak hatırla.', icon: '✍️', route: 'WritingChallenge' },
  { title: 'Listening Rush', description: 'Duyduğunu hızlıca seç ve dinleme refleksini geliştir.', icon: '🎧', route: 'ListeningLab' }
];

export function GameCenterScreen({ navigation }: { navigation: { goBack: () => void; navigate: (route: GameRoute) => void } }) {
  return <ScrollView className="flex-1 bg-background px-5 pt-14" contentContainerStyle={{ paddingBottom: 48 }}><Pressable onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text className="mt-8 text-xs font-bold uppercase tracking-widest text-primary">OYUN MERKEZİ</Text><Text className="mt-3 text-3xl font-bold text-foreground">Öğrenmeyi oyuna çevir</Text><Text className="mt-3 leading-6 text-muted-foreground">Her oyun farklı bir beceriyi çalıştırır. Hız değil, doğru öğrenme serisi kazanır.</Text><View className="mt-7 gap-3">{games.map(game => <Pressable key={game.title} onPress={() => navigation.navigate(game.route)} className="flex-row items-center rounded-2xl border border-border bg-surface p-5"><Text className="mr-4 text-3xl">{game.icon}</Text><View className="flex-1"><Text className="text-lg font-bold text-foreground">{game.title}</Text><Text className="mt-1 leading-5 text-muted-foreground">{game.description}</Text></View><Text className="text-xl text-primary">→</Text></Pressable>)}</View></ScrollView>;
}
