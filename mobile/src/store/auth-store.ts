import { create } from 'zustand';
import { api, AuthResponse, AuthUser, setAccessToken } from '../lib/api';
import { tokenStorage } from '../lib/token-storage';
type AuthState = { user: AuthUser | null; booting: boolean; signIn: (result: AuthResponse) => Promise<void>; signOut: () => Promise<void>; restore: () => Promise<void>; setUser: (user: AuthUser) => void };
export const useAuthStore = create<AuthState>((set) => ({ user: null, booting: true,
  signIn: async (result) => { setAccessToken(result.accessToken); await tokenStorage.saveRefreshToken(result.refreshToken); set({ user: result.user }); },
  signOut: async () => { setAccessToken(null); await tokenStorage.clear(); set({ user: null }); },
  restore: async () => { try { const token = await tokenStorage.getRefreshToken(); if (!token) return; const result = await api.refresh(token); setAccessToken(result.accessToken); await tokenStorage.saveRefreshToken(result.refreshToken); set({ user: result.user }); } catch { await tokenStorage.clear(); } finally { set({ booting: false }); } },
  setUser: (user) => set({ user }),
}));
