import { apiClient } from './client';
import type { ApiResponse } from './types';
import type { ProspectCategory } from './prospects';

export type ScheduledAutomationStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export interface ScheduledProspectAutomationDto {
  id: number;
  localities: string[];
  categories: ProspectCategory[] | null;
  radiusKm: number;
  maxResults: number;
  campaignId: number;
  campaignName: string;
  scheduledAt: string;
  status: ScheduledAutomationStatus;
  runAt: string | null;
  resultSummary: string | null;
  createdAt: string;
}

export interface ScheduleProspectAutomationRequest {
  localities: string[];
  categories?: ProspectCategory[];
  radiusKm: number;
  maxResults: number;
  campaignId: number;
  scheduledAt: string;
}

export async function listProspectAutomations(): Promise<ScheduledProspectAutomationDto[]> {
  const response = await apiClient.get<ApiResponse<ScheduledProspectAutomationDto[]>>('/prospect-automations');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las automatizaciones programadas.');
  }
  return response.data.data;
}

export async function createProspectAutomation(
  request: ScheduleProspectAutomationRequest,
): Promise<ScheduledProspectAutomationDto> {
  const response = await apiClient.post<ApiResponse<ScheduledProspectAutomationDto>>('/prospect-automations', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo programar la automatización.');
  }
  return response.data.data;
}

export async function cancelProspectAutomation(id: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/prospect-automations/${id}/cancel`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo cancelar la automatización.');
  }
}
