import * as SecureStore from 'expo-secure-store';
const REFRESH_TOKEN_KEY = 'english-learning.refresh-token';
export const tokenStorage = { getRefreshToken: () => SecureStore.getItemAsync(REFRESH_TOKEN_KEY), saveRefreshToken: (token: string) => SecureStore.setItemAsync(REFRESH_TOKEN_KEY, token), clear: () => SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY) };
