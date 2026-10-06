import { Text, View } from 'react-native';
import type { MissionStep, MissionStepKey } from '../../../lib/api';
import { stepLabel } from './queries';

/** Per-step progress (done/required). Capped steps (daily free quota reached) count as finished but are marked. */
export function MissionStepper({ steps, currentKey, capped }: { steps: MissionStep[]; currentKey?: MissionStepKey; capped: Partial<Record<MissionStepKey, boolean>> }) {
  return <View className="mt-6 flex-row gap-2">
    {steps.map((step) => {
      const label = stepLabel(step);
      const isCapped = Boolean(capped[step.key]) && !step.completed;
      const isCurrent = step.key === currentKey;
      const ratio = step.completed || isCapped ? 1 : step.required > 0 ? Math.min(1, step.done / step.required) : 0;
      const status = step.completed ? 'tamamlandı' : isCapped ? 'günlük sınır nedeniyle sınırlı' : `${step.done} / ${step.required} tamamlandı`;
      return <View key={step.key} accessible accessibilityLabel={`${label}: ${status}${isCurrent ? ', şu anki adım' : ''}`} className="flex-1">
        <View className="h-1.5 overflow-hidden rounded-full bg-slate-200"><View className={`h-1.5 rounded-full ${step.completed ? 'bg-emerald-500' : isCapped ? 'bg-amber-400' : 'bg-primary'}`} style={{ width: `${ratio * 100}%` }} /></View>
        <Text numberOfLines={2} maxFontSizeMultiplier={1.6} className={`mt-2 text-[11px] font-bold leading-4 ${isCurrent ? 'text-primary' : 'text-foreground'}`}>{label}</Text>
        <Text numberOfLines={2} maxFontSizeMultiplier={1.6} className={`text-[11px] font-medium ${step.completed ? 'text-emerald-700' : isCapped ? 'text-amber-700' : 'text-muted-foreground'}`}>{step.completed ? '✓ Tamam' : isCapped ? 'Sınırlı' : `${step.done}/${step.required}`}</Text>
      </View>;
    })}
  </View>;
}
