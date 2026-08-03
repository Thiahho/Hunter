import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

export interface CurrentUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  organizationId: number;
  roles: string[];
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: CurrentUser | null;
  setSession: (session: { accessToken: string; refreshToken: string; user: CurrentUser }) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setSession: ({ accessToken, refreshToken, user }) => set({ accessToken, refreshToken, user }),
      clearSession: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'hunter-auth',
      storage: createJSONStorage(() => sessionStorage),
    },
  ),
);
