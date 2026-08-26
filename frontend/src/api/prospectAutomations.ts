import { apiClient } from './client';
import type { ApiResponse } from './types';
import type { ProspectCategory } from './prospects';

export type ScheduledAutomationStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
export type ProspectAutomationSource = 'OpenStreetMap' | 'Apify';

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
  keywords: string[] | null;
  source: ProspectAutomationSource;
}

export interface ScheduleProspectAutomationRequest {
  localities: string[];
  categories?: ProspectCategory[];
  radiusKm: number;
  maxResults: number;
  scheduledAt: string;
  keywords?: string[];
  source?: ProspectAutomationSource;
}

export interface CreateDailyProspectingPlanRequest {
  localities: string[];
  startAt: string;
  intervalMinutes?: number;
  radiusKm?: number;
  includeApify?: boolean;
}

export interface DailyProspectingPlanDto {
  automations: ScheduledProspectAutomationDto[];
  estimatedCeiling: number;
  localitiesCovered: number;
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

export async function createDailyProspectingPlan(request: CreateDailyProspectingPlanRequest): Promise<DailyProspectingPlanDto> {
  const response = await apiClient.post<ApiResponse<DailyProspectingPlanDto>>('/prospect-automations/daily-plan', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear el plan diario.');
  }
  return response.data.data;
}
