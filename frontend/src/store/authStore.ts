import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

export interface CurrentUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  telegramChatId: string | null;
  organizationId: number;
  roles: string[];
  telegramConnected: boolean;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: CurrentUser | null;
  setSession: (session: { accessToken: string; refreshToken: string; user: CurrentUser }) => void;
  updateUser: (user: CurrentUser) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setSession: ({ accessToken, refreshToken, user }) => set({ accessToken, refreshToken, user }),
      updateUser: (user) => set({ user }),
      clearSession: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'hunter-auth',
      storage: createJSONStorage(() => sessionStorage),
    },
  ),
);
