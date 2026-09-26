import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StatusBar } from 'expo-status-bar';
import { Text, View } from 'react-native';

import './global.css';
import { apiConfig } from './src/lib/api';

type RootStackParamList = { Home: undefined };
const Stack = createNativeStackNavigator<RootStackParamList>();
const queryClient = new QueryClient();

function HomeScreen() {
  return (
    <View className="flex-1 items-center justify-center bg-background px-6">
      <Text className="text-3xl font-bold text-foreground">English Learning</Text>
      <Text className="mt-3 text-center text-base text-muted-foreground">
        Mobile foundation is ready. API: {apiConfig.baseUrl}
      </Text>
    </View>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <NavigationContainer>
        <StatusBar style="auto" />
        <Stack.Navigator>
          <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Home' }} />
        </Stack.Navigator>
      </NavigationContainer>
    </QueryClientProvider>
  );
}
