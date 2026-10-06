import { create } from 'zustand';
import { api, AuthResponse, AuthUser, setAccessToken } from '../lib/api';
import { tokenStorage } from '../lib/token-storage';
import { clearReviewQueue } from '../lib/offline-review-queue';
import { queryClient } from '../lib/query-client';
type AuthState = { user: AuthUser | null; booting: boolean; signIn: (result: AuthResponse) => Promise<void>; signOut: () => Promise<void>; restore: () => Promise<void>; setUser: (user: AuthUser) => void };
export const useAuthStore = create<AuthState>((set) => ({ user: null, booting: true,
  signIn: async (result) => { setAccessToken(result.accessToken); await tokenStorage.saveRefreshToken(result.refreshToken); set({ user: result.user }); },
  // Drops the token first so in-flight refetches fail, then wipes per-account data (offline queue, query cache).
  signOut: async () => { setAccessToken(null); await tokenStorage.clear(); await clearReviewQueue(); set({ user: null }); queryClient.clear(); },
  // A failed refresh ends the stored session, so its queued reviews must not be sent with the next account's token.
  restore: async () => { try { const token = await tokenStorage.getRefreshToken(); if (!token) return; const result = await api.refresh(token); setAccessToken(result.accessToken); await tokenStorage.saveRefreshToken(result.refreshToken); set({ user: result.user }); } catch { await tokenStorage.clear(); await clearReviewQueue(); } finally { set({ booting: false }); } },
  setUser: (user) => set({ user }),
}));
