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

type RootStackParamList = { Login: undefined; Register: undefined; Onboarding: undefined; Home: undefined };
const Stack = createNativeStackNavigator<RootStackParamList>();
const queryClient = new QueryClient();

function HomeScreen() {
  const signOut = useAuthStore((state) => state.signOut);
  return (
    <View className="flex-1 items-center justify-center bg-background px-6">
      <Text className="text-3xl font-bold text-foreground">English Learning</Text>
      <Text className="mt-3 text-center text-base text-muted-foreground">Öğrenmeye hazırsın.</Text>
      <Text onPress={() => void signOut()} className="mt-8 text-blue-700">Çıkış yap</Text>
    </View>
  );
}

export default function App() {
  const { user, booting, restore } = useAuthStore();
  useEffect(() => { void restore(); }, [restore]);
  if (booting) return <View className="flex-1 items-center justify-center bg-background"><Text>Yükleniyor…</Text></View>;
  return (
    <QueryClientProvider client={queryClient}>
      <NavigationContainer>
        <StatusBar style="auto" />
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          {!user ? <><Stack.Screen name="Login" component={LoginScreen} /><Stack.Screen name="Register" component={RegisterScreen} /></> : !user.onboardingCompleted ? <Stack.Screen name="Onboarding" component={OnboardingScreen} /> : <Stack.Screen name="Home" component={HomeScreen} />}
        </Stack.Navigator>
      </NavigationContainer>
    </QueryClientProvider>
  );
}
