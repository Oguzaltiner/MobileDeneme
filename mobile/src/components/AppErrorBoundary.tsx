import React from 'react';
import { Pressable, Text, View } from 'react-native';

type Props = { children: React.ReactNode };
type State = { hasError: boolean };

export class AppErrorBoundary extends React.Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error) {
    // Keep a local diagnostic trail without exposing stack traces to users.
    console.error('[AppErrorBoundary]', error.message);
  }

  render() {
    if (!this.state.hasError) return this.props.children;
    return (
      <View className="flex-1 items-center justify-center bg-background px-6">
        <Text accessibilityRole="header" className="text-center text-2xl font-bold text-foreground">Bir şeyler ters gitti.</Text>
        <Text className="mt-3 text-center leading-6 text-muted-foreground">Uygulamayı yenileyip tekrar deneyebilirsin.</Text>
        <Pressable accessibilityRole="button" accessibilityLabel="Uygulamayı yenile" onPress={() => this.setState({ hasError: false })} className="mt-7 rounded-2xl bg-primary px-6 py-4">
          <Text className="font-bold text-white">Tekrar dene</Text>
        </Pressable>
      </View>
    );
  }
}
