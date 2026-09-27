import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { PronunciationButton } from '../../components/PronunciationButton';

const scenarios = [
  { key: 'airport', title: 'Havaalanında', description: 'Check-in ve yön sorma ifadeleri.', phrases: [['Where is gate twelve?', 'On iki numaralı kapı nerede?'], ['I have a reservation.', 'Rezervasyonum var.']] },
  { key: 'restaurant', title: 'Restoranda', description: 'Sipariş verirken kullanacağın kısa cümleler.', phrases: [['Could I see the menu?', 'Menüyü görebilir miyim?'], ['I would like some water.', 'Biraz su istiyorum.']] },
  { key: 'meeting', title: 'İş görüşmesinde', description: 'Kendini tanıt ve fikrini net ifade et.', phrases: [['Let me explain my experience.', 'Deneyimimi açıklayayım.'], ['I agree with your proposal.', 'Teklifinize katılıyorum.']] }
];

export function ConversationPracticeScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const [selected, setSelected] = useState(scenarios[0]);
  return <ScrollView className="flex-1 bg-background px-5 pt-14" contentContainerStyle={{ paddingBottom: 56 }}><Pressable onPress={navigation.goBack}><Text className="font-semibold text-primary">‹ Geri</Text></Pressable><Text accessibilityRole="header" className="mt-8 text-4xl font-bold text-foreground">Konuşma pratiği</Text><Text className="mt-2 text-base leading-6 text-muted-foreground">Gerçek hayattan kısa senaryoları dinle, tekrar et ve güven kazan.</Text><View className="mt-7 gap-3">{scenarios.map(scenario => <Pressable key={scenario.key} accessibilityRole="button" accessibilityState={{ selected: selected.key === scenario.key }} onPress={() => setSelected(scenario)} className={`rounded-2xl border p-4 ${selected.key === scenario.key ? 'border-primary bg-primary-soft' : 'border-border bg-surface'}`}><Text className="font-bold text-foreground">{scenario.title}</Text><Text className="mt-1 text-sm text-muted-foreground">{scenario.description}</Text></Pressable>)}</View><View className="mt-7 rounded-3xl bg-surface p-6"><Text className="text-xs font-bold uppercase tracking-widest text-primary">DİYALOG KARTLARI</Text>{selected.phrases.map(([english, turkish]) => <View key={english} className="mt-5 border-b border-border pb-5"><Text className="text-xl font-bold text-foreground">{english}</Text><Text className="mt-1 text-sm text-muted-foreground">{turkish}</Text><View className="mt-3"><PronunciationButton text={english} label={`${english} cümlesini dinle`} /></View></View>)}</View></ScrollView>;
}
