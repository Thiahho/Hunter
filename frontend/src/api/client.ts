import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';
import type { ApiResponse } from './types';

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5226/api/v1';

export const apiClient = axios.create({ baseURL });

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, user, setSession, clearSession } = useAuthStore.getState();
  if (!refreshToken || !user) return null;

  try {
    const response = await axios.post<ApiResponse<{
      accessToken: string;
      expiresAt: string;
      refreshToken: string;
      user: typeof user;
    }>>(`${baseURL}/auth/refresh`, { refreshToken });

    const data = response.data.data;
    if (!data) {
      clearSession();
      return null;
    }

    setSession({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
    return data.accessToken;
  } catch {
    clearSession();
    return null;
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

    if (error.response?.status !== 401 || !originalRequest || originalRequest._retry) {
      throw error;
    }

    originalRequest._retry = true;

    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null;
    });

    const newToken = await refreshPromise;
    if (!newToken) {
      throw error;
    }

    originalRequest.headers.Authorization = `Bearer ${newToken}`;
    return apiClient(originalRequest);
  },
);
