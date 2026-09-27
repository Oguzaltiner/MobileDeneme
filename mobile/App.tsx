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
import { DailyMissionScreen } from './src/screens/learning/DailyMissionScreen';
import { QuizStartScreen } from './src/screens/quiz/QuizStartScreen';
import { QuizQuestionScreen } from './src/screens/quiz/QuizQuestionScreen';
import { QuizResultScreen } from './src/screens/quiz/QuizResultScreen';
import { PremiumScreen } from './src/screens/premium/PremiumScreen';
import { SentenceChallengeScreen } from './src/screens/learning/SentenceChallengeScreen';
import { LeaderboardScreen } from './src/screens/home/LeaderboardScreen';
import { LearningPathsScreen } from './src/screens/home/LearningPathsScreen';
import { WritingChallengeScreen } from './src/screens/learning/WritingChallengeScreen';
import { MatchingChallengeScreen } from './src/screens/learning/MatchingChallengeScreen';

export type RootStackParamList = { Login: undefined; Register: undefined; Onboarding: undefined; Home: undefined; Vocabulary: undefined; WordDetail: { id: string }; Learn: undefined; DailyMission: undefined; SentenceChallenge: undefined; WritingChallenge: undefined; MatchingChallenge: undefined; Leaderboard: undefined; LearningPaths: undefined; QuizStart: undefined; QuizQuestion: { sessionId: string }; QuizResult: { sessionId: string }; Premium: undefined };
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
          {!user ? <><Stack.Screen name="Login" component={LoginScreen} /><Stack.Screen name="Register" component={RegisterScreen} /></> : !user.onboardingCompleted ? <Stack.Screen name="Onboarding" component={OnboardingScreen} /> : <><Stack.Screen name="Home" component={HomeScreen} /><Stack.Screen name="Vocabulary" component={VocabularyScreen} /><Stack.Screen name="WordDetail" component={WordDetailScreen} /><Stack.Screen name="Learn" component={LearnScreen} /><Stack.Screen name="DailyMission" component={DailyMissionScreen} /><Stack.Screen name="SentenceChallenge" component={SentenceChallengeScreen} /><Stack.Screen name="WritingChallenge" component={WritingChallengeScreen} /><Stack.Screen name="MatchingChallenge" component={MatchingChallengeScreen} /><Stack.Screen name="Leaderboard" component={LeaderboardScreen} /><Stack.Screen name="LearningPaths" component={LearningPathsScreen} /><Stack.Screen name="QuizStart" component={QuizStartScreen} /><Stack.Screen name="QuizQuestion" component={QuizQuestionScreen} /><Stack.Screen name="QuizResult" component={QuizResultScreen} /><Stack.Screen name="Premium" component={PremiumScreen} /></>}
        </Stack.Navigator>
      </NavigationContainer>
    </QueryClientProvider>
  );
}
