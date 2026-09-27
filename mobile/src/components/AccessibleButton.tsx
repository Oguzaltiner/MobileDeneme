import { Pressable, PressableProps, Text } from 'react-native';

type Props = PressableProps & { label: string; children: string };
export function AccessibleButton({ label, children, accessibilityRole = 'button', ...props }: Props) {
  return <Pressable {...props} accessibilityRole={accessibilityRole} accessibilityLabel={label} accessibilityHint="Etkinleştirmek için dokun" accessible><Text allowFontScaling maxFontSizeMultiplier={1.4}>{children}</Text></Pressable>;
}
