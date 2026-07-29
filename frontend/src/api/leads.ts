import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';

export type LeadStatus = 'New' | 'InProgress' | 'Won' | 'Lost';
export type LeadPriority = 'Low' | 'Medium' | 'High';
export type LostReason =
  | 'Price'
  | 'NoStock'
  | 'ExistingSupplier'
  | 'NoResponse'
  | 'NotInterested'
  | 'Logistics'
  | 'CommercialConditions'
  | 'Other';
export type LeadActivityType = 'Contact' | 'Call' | 'Whatsapp' | 'Quote' | 'Note' | 'FollowUp';
export type FollowUpStatus = 'Pending' | 'Completed' | 'Cancelled';

export interface LeadListItem {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
  status: LeadStatus;
  priority: LeadPriority;
  assignedToUserId: number | null;
  createdAt: string;
}

export interface LeadActivity {
  id: number;
  type: LeadActivityType;
  description: string;
  userId: number;
  createdAt: string;
}

export interface FollowUp {
  id: number;
  scheduledAt: string;
  status: FollowUpStatus;
  notes: string | null;
  completedAt: string | null;
}

export interface Lead {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
  campaignId: number | null;
  assignedToUserId: number | null;
  status: LeadStatus;
  priority: LeadPriority;
  lostReason: LostReason | null;
  createdAt: string;
  firstResponseAt: string | null;
  lastActivityAt: string | null;
  closedAt: string | null;
  activities: LeadActivity[];
  followUps: FollowUp[];
}

export interface LeadQuery {
  status?: LeadStatus;
  priority?: LeadPriority;
  assignedUserId?: number;
  campaignId?: number;
  page?: number;
  pageSize?: number;
}

export interface MarkWonRequest {
  amount: number;
  currency: string;
  margin?: number | null;
  productCategory?: string | null;
}

export interface MarkLostRequest {
  lostReason: LostReason;
  notes?: string | null;
}

export async function searchLeads(query: LeadQuery): Promise<PagedResult<LeadListItem>> {
  const response = await apiClient.get<ApiResponse<PagedResult<LeadListItem>>>('/leads', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los leads.');
  }
  return response.data.data;
}

export async function fetchLeadById(id: number): Promise<Lead> {
  const response = await apiClient.get<ApiResponse<Lead>>(`/leads/${id}`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo obtener el lead.');
  }
  return response.data.data;
}

export async function setLeadInProgress(id: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/leads/${id}/in-progress`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo actualizar el lead.');
  }
}

export async function markLeadWon(id: number, request: MarkWonRequest): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/leads/${id}/won`, request);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo marcar el lead como ganado.');
  }
}

export async function markLeadLost(id: number, request: MarkLostRequest): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/leads/${id}/lost`, request);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo marcar el lead como perdido.');
  }
}
