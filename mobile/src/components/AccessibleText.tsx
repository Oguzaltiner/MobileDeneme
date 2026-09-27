import { Text, TextProps } from 'react-native';

export function AccessibleText({ maxFontSizeMultiplier = 1.4, allowFontScaling = true, ...props }: TextProps) {
  return <Text {...props} allowFontScaling={allowFontScaling} maxFontSizeMultiplier={maxFontSizeMultiplier} />;
}
