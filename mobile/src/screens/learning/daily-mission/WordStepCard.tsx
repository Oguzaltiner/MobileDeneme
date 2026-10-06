import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { PronunciationButton } from '../../../components/PronunciationButton';
import type { MissionWordStepKey, VocabularyWord } from '../../../lib/api';

export type ReviewRating = 0 | 1 | 2 | 3;
const RATINGS: { rating: ReviewRating; label: string; hint: string; className: string; textClassName: string }[] = [
  { rating: 0, label: 'Tekrar', hint: 'Hatırlamadım', className: 'bg-red-50 border border-red-100', textClassName: 'text-red-700' },
  { rating: 1, label: 'Zor', hint: 'Zorlandım', className: 'bg-amber-50 border border-amber-200', textClassName: 'text-amber-700' },
  { rating: 2, label: 'İyi', hint: 'Hatırladım', className: 'bg-primary', textClassName: 'text-white' },
  { rating: 3, label: 'Kolay', hint: 'Çok kolaydı', className: 'bg-emerald-600', textClassName: 'text-white' },
];

/**
 * Review / new-word card: term and pronunciation first, meaning after reveal, then a rating.
 * Render with `key={word.id}` so the reveal state resets for every word.
 */
export function WordStepCard({ word, stepKey, position, total, submitting, onRate }: { word: VocabularyWord; stepKey: MissionWordStepKey; position: number; total: number; submitting: boolean; onRate: (rating: ReviewRating) => void }) {
  const [revealed, setRevealed] = useState(false);
  const isNew = stepKey === 'new-words';
  return <View className="mt-6 rounded-3xl border border-border bg-surface p-6">
    <View className="flex-row items-center justify-between">
      <Text className="text-xs font-bold uppercase tracking-widest text-primary">{isNew ? 'Yeni kelime' : 'Tekrar'} · {position} / {total}</Text>
      <Text className="rounded-full bg-primary-soft px-3 py-1 text-xs font-bold text-primary">{word.level}</Text>
    </View>
    <Text accessibilityRole="header" className="mt-5 text-4xl font-bold text-foreground">{word.term}</Text>
    <Text className="mt-1 text-sm text-muted-foreground">{[word.pronunciation, word.partOfSpeech].filter(Boolean).join(' · ') || 'İngilizce kelime'}</Text>
    <View className="mt-4"><PronunciationButton text={word.term} label={`${word.term} kelimesini dinle`} /></View>
    {!revealed ? <>
      <Text className="mt-6 text-base leading-6 text-muted-foreground">{isNew ? 'Önce kelimeyi dinle ve tahmin et, sonra anlamına bak.' : 'Anlamını hatırlamaya çalış, sonra kontrol et.'}</Text>
      <Pressable accessibilityRole="button" accessibilityLabel="Anlamını göster" onPress={() => setRevealed(true)} className="mt-5 items-center rounded-2xl bg-primary py-4"><Text className="font-bold text-white">Anlamını göster</Text></Pressable>
    </> : <>
      <View className="mt-6 rounded-2xl bg-primary-soft p-4">
        <Text className="text-xs font-bold uppercase tracking-widest text-primary">Türkçe</Text>
        <Text className="mt-1 text-2xl font-bold text-foreground">{word.translation}</Text>
        {word.definition ? <Text className="mt-2 text-sm leading-5 text-muted-foreground">{word.definition}</Text> : null}
      </View>
      {word.exampleSentence ? <View className="mt-3 rounded-2xl border border-border p-4">
        <Text className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Örnek cümle</Text>
        <Text className="mt-1 text-base italic leading-6 text-foreground">“{word.exampleSentence}”</Text>
        <View className="mt-3"><PronunciationButton text={word.exampleSentence} label="Örnek cümleyi dinle" /></View>
      </View> : null}
      <Text accessibilityRole="header" className="mt-6 text-sm font-semibold text-foreground">{isNew ? 'Bu kelime senin için nasıl?' : 'Ne kadar iyi hatırladın?'}</Text>
      <View className="mt-3 flex-row gap-2">
        {RATINGS.map((item) => <Pressable key={item.rating} disabled={submitting} accessibilityRole="button" accessibilityLabel={`${item.label}: ${item.hint}`} accessibilityState={{ disabled: submitting, busy: submitting }} onPress={() => onRate(item.rating)} className={`flex-1 items-center rounded-2xl py-4 ${item.className} ${submitting ? 'opacity-50' : ''}`}><Text className={`font-bold ${item.textClassName}`}>{item.label}</Text></Pressable>)}
      </View>
    </>}
  </View>;
}
