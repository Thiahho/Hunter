import { apiClient } from './client';
import type { ApiResponse } from './types';

export type UserArea = 'Unassigned' | 'Administracion' | 'Ventas';
export type InvitableRole = 'ADMIN' | 'MANAGER' | 'SELLER';

export interface UserDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  phone: string | null;
  telegramChatId: string | null;
  area: UserArea;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: InvitableRole;
  phone?: string;
  area?: UserArea;
}

export interface UpdateUserRequest {
  email?: string | null;
  phone?: string | null;
  telegramChatId?: string | null;
  area?: UserArea;
  isActive?: boolean;
}

export async function listUsers(): Promise<UserDto[]> {
  const response = await apiClient.get<ApiResponse<UserDto[]>>('/users');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los usuarios.');
  }
  return response.data.data;
}

export async function createUser(request: CreateUserRequest): Promise<UserDto> {
  const response = await apiClient.post<ApiResponse<UserDto>>('/users', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear el usuario.');
  }
  return response.data.data;
}

export async function updateUser(id: number, request: UpdateUserRequest): Promise<UserDto> {
  const response = await apiClient.patch<ApiResponse<UserDto>>(`/users/${id}`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo actualizar el usuario.');
  }
  return response.data.data;
}
