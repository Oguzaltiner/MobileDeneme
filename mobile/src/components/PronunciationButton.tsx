import { useEffect, useState } from 'react';
import { Pressable, Text } from 'react-native';
import * as Speech from 'expo-speech';

export function PronunciationButton({ text, label = 'Telaffuzunu dinle' }: { text: string; label?: string }) {
  const [speaking, setSpeaking] = useState(false);
  useEffect(() => () => { Speech.stop(); }, []);
  const play = () => {
    if (!text.trim()) return;
    if (speaking) { Speech.stop(); setSpeaking(false); return; }
    setSpeaking(true);
    Speech.speak(text, { language: 'en-US', rate: 0.82, onDone: () => setSpeaking(false), onStopped: () => setSpeaking(false), onError: () => setSpeaking(false) });
  };
  return <Pressable onPress={play} accessibilityRole="button" accessibilityLabel={speaking ? 'Telaffuzu durdur' : label} accessibilityHint="Kelimenin İngilizce telaffuzunu seslendirir" className="self-start rounded-full bg-blue-100 px-4 py-2">
    <Text className="font-semibold text-blue-700">{speaking ? '■ Durdur' : '🔊 Dinle'}</Text>
  </Pressable>;
}
