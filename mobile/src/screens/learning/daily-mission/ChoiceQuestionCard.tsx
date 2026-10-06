import { useEffect } from 'react';
import { Platform, Pressable, Text, View } from 'react-native';
import { PronunciationButton } from '../../../components/PronunciationButton';
import type { MissionQuestionStepKey, QuizOption } from '../../../lib/api';
import { announce } from './MissionBanners';

/**
 * Server verdict for one answered question; the correct option is only known after answering.
 * `selectedKey` is the server's stored first answer when it can be inferred, otherwise undefined.
 */
export type AnswerFeedback = { stepKey: MissionQuestionStepKey; questionId: string; selectedKey?: string; correctOptionKey: string; isCorrect: boolean; explanation?: string | null; term?: string | null; translation?: string | null };

type Props = {
  stepKey: MissionQuestionStepKey;
  eyebrow: string;
  heading: string;
  /** Shown as the question text. For listening this is the question, never the transcript. */
  prompt?: string;
  /** Spoken via TTS only (recall: `speakText`, listening: hidden transcript). Never rendered or put in labels. */
  speakText?: string | null;
  speakLabel: string;
  options: QuizOption[];
  pendingKey?: string;
  feedback?: AnswerFeedback;
  isLast: boolean;
  onSelect: (optionKey: string) => void;
  onNext: () => void;
};

const SILENT_MODE_HINT = Platform.OS === 'ios' ? 'Ses gelmiyorsa telefonunun sessiz modunu kapat.' : 'Ses gelmiyorsa medya ses seviyesini kontrol et.';

export function ChoiceQuestionCard({ stepKey, eyebrow, heading, prompt, speakText, speakLabel, options, pendingKey, feedback, isLast, onSelect, onNext }: Props) {
  const locked = Boolean(feedback) || Boolean(pendingKey);
  const isListening = stepKey === 'listening';
  const correctText = feedback ? options.find((option) => option.key === feedback.correctOptionKey)?.text : undefined;
  const verdict = feedback ? (feedback.isCorrect ? 'Doğru!' : `Yanlış. Doğru cevap: ${feedback.correctOptionKey}${correctText ? `, ${correctText}` : ''}`) : '';
  useEffect(() => { if (verdict) announce(`${verdict}${feedback?.explanation ? `. ${feedback.explanation}` : ''}`); }, [verdict, feedback?.explanation]);
  return <View className="mt-6 rounded-3xl border border-border bg-surface p-6">
    <Text className="text-xs font-bold uppercase tracking-widest text-primary">{eyebrow}</Text>
    <Text accessibilityRole="header" className="mt-3 text-2xl font-bold leading-8 text-foreground">{heading}</Text>
    {speakText ? <View className={isListening ? 'mt-5 items-center rounded-2xl bg-primary-soft p-5' : 'mt-4'}>
      {isListening ? <Text className="mb-3 text-center text-sm leading-5 text-muted-foreground">Metin ekranda gösterilmez. İstediğin kadar tekrar dinleyebilirsin.</Text> : null}
      <PronunciationButton text={speakText} label={speakLabel} />
      {isListening ? <Text className="mt-3 text-center text-xs leading-4 text-muted-foreground">{SILENT_MODE_HINT}</Text> : null}
    </View> : null}
    {prompt ? <Text className={`mt-5 text-lg font-semibold leading-7 text-foreground ${isListening ? '' : 'rounded-2xl bg-background p-4'}`}>{prompt}</Text> : null}
    <View className="mt-5 gap-3">
      {options.map((option) => {
        const isSelected = feedback ? feedback.selectedKey === option.key : pendingKey === option.key;
        const isCorrectOption = feedback?.correctOptionKey === option.key;
        const tone = feedback
          ? isCorrectOption ? 'border-emerald-500 bg-emerald-50' : isSelected ? 'border-red-400 bg-red-50' : 'border-transparent bg-background opacity-60'
          : isSelected ? 'border-primary bg-primary-soft' : 'border-transparent bg-background';
        const suffix = feedback ? isCorrectOption ? ', doğru cevap' : isSelected ? ', senin cevabın, yanlış' : '' : '';
        return <Pressable key={option.key} disabled={locked} accessibilityRole="button" accessibilityLabel={`Şık ${option.key}: ${option.text}${suffix}`} accessibilityState={{ selected: isSelected, disabled: locked, busy: pendingKey === option.key }} onPress={() => onSelect(option.key)} className={`flex-row items-center rounded-2xl border-2 p-4 ${tone}`}>
          <View className={`mr-3 h-8 w-8 items-center justify-center rounded-full ${feedback && isCorrectOption ? 'bg-emerald-500' : feedback && isSelected ? 'bg-red-500' : 'bg-white'}`}><Text className={`font-bold ${feedback && (isCorrectOption || isSelected) ? 'text-white' : 'text-primary'}`}>{feedback && isCorrectOption ? '✓' : feedback && isSelected ? '✗' : option.key}</Text></View>
          <Text className="flex-1 font-semibold text-foreground">{option.text}</Text>
        </Pressable>;
      })}
    </View>
    {pendingKey && !feedback ? <Text accessibilityLiveRegion="polite" className="mt-4 text-center text-sm text-muted-foreground">Cevabın kontrol ediliyor…</Text> : null}
    {feedback ? <View accessibilityLiveRegion="polite" className={`mt-5 rounded-2xl p-4 ${feedback.isCorrect ? 'bg-emerald-50' : 'bg-red-50'}`}>
      <Text className={`text-base font-bold ${feedback.isCorrect ? 'text-emerald-700' : 'text-red-700'}`}>{feedback.isCorrect ? 'Doğru! 🎯' : `Yanlış. Doğru cevap: ${feedback.correctOptionKey}${correctText ? ` · ${correctText}` : ''}`}</Text>
      {!feedback.selectedKey && !feedback.isCorrect ? <Text className="mt-1 text-xs text-red-700">İlk cevabın kaydedildi; sonraki seçimler sonucu değiştirmez.</Text> : null}
      {feedback.explanation ? <Text className="mt-2 text-sm leading-5 text-foreground">{feedback.explanation}</Text> : null}
      {feedback.term ? <View className="mt-3 flex-row items-center justify-between rounded-xl bg-white/70 p-3">
        <View className="flex-1 pr-3"><Text className="font-bold text-foreground">{feedback.term}</Text>{feedback.translation ? <Text className="text-sm text-muted-foreground">{feedback.translation}</Text> : null}</View>
        <PronunciationButton text={feedback.term} label="Kelimeyi dinle" />
      </View> : null}
    </View> : null}
    {feedback ? <Pressable accessibilityRole="button" accessibilityLabel={isLast ? 'Adımı bitir ve devam et' : 'Sonraki soruya geç'} onPress={onNext} className="mt-5 items-center rounded-2xl bg-primary py-4"><Text className="font-bold text-white">{isLast ? 'Devam et' : 'Sonraki soru'}</Text></Pressable> : null}
  </View>;
}
