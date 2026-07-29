import { apiClient } from './client';
import type { ApiResponse } from './types';
import type { CurrentUser } from '../store/authStore';

export interface AuthResult {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  user: CurrentUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterOrganizationRequest {
  organizationName: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerEmail: string;
  password: string;
}

export async function login(request: LoginRequest): Promise<AuthResult> {
  const response = await apiClient.post<ApiResponse<AuthResult>>('/auth/login', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo iniciar sesión.');
  }
  return response.data.data;
}

export async function registerOrganization(request: RegisterOrganizationRequest): Promise<AuthResult> {
  const response = await apiClient.post<ApiResponse<AuthResult>>('/auth/register', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear la organización.');
  }
  return response.data.data;
}

export async function fetchCurrentUser(): Promise<CurrentUser> {
  const response = await apiClient.get<ApiResponse<CurrentUser>>('/auth/me');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo obtener el usuario actual.');
  }
  return response.data.data;
}
