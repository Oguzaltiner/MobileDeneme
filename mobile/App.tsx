import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StatusBar } from 'expo-status-bar';
import { Text, View } from 'react-native';
import { useEffect } from 'react';

import './global.css';
import { useAuthStore } from './src/store/auth-store';
import { LoginScreen } from './src/screens/auth/LoginScreen';
import { RegisterScreen } from './src/screens/auth/RegisterScreen';
import { OnboardingScreen } from './src/screens/onboarding/OnboardingScreen';
import { HomeScreen } from './src/screens/home/HomeScreen';
import { VocabularyScreen } from './src/screens/vocabulary/VocabularyScreen';
import { WordDetailScreen } from './src/screens/vocabulary/WordDetailScreen';
import { LearnScreen } from './src/screens/learning/LearnScreen';

export type RootStackParamList = { Login: undefined; Register: undefined; Onboarding: undefined; Home: undefined; Vocabulary: undefined; WordDetail: { id: string }; Learn: undefined };
const Stack = createNativeStackNavigator<RootStackParamList>();
const queryClient = new QueryClient();

export default function App() {
  const { user, booting, restore } = useAuthStore();
  useEffect(() => { void restore(); }, [restore]);
  if (booting) return <View className="flex-1 items-center justify-center bg-background"><Text>Yükleniyor…</Text></View>;
  return (
    <QueryClientProvider client={queryClient}>
      <NavigationContainer>
        <StatusBar style="auto" />
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          {!user ? <><Stack.Screen name="Login" component={LoginScreen} /><Stack.Screen name="Register" component={RegisterScreen} /></> : !user.onboardingCompleted ? <Stack.Screen name="Onboarding" component={OnboardingScreen} /> : <><Stack.Screen name="Home" component={HomeScreen} /><Stack.Screen name="Vocabulary" component={VocabularyScreen} /><Stack.Screen name="WordDetail" component={WordDetailScreen} /><Stack.Screen name="Learn" component={LearnScreen} /></>}
        </Stack.Navigator>
      </NavigationContainer>
    </QueryClientProvider>
  );
}
