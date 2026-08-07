import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';
import type { MessagingChannel } from './messages';

export type CampaignStatus = 'Draft' | 'Ready' | 'Running' | 'Paused' | 'Completed' | 'Cancelled';

export interface CampaignListItem {
  id: number;
  name: string;
  status: CampaignStatus;
  channel: MessagingChannel;
  messageTemplateName: string;
  messagesPerMinute: number;
  recipientsCount: number;
  sentCount: number;
  createdAt: string;
}

export interface CampaignQuery {
  status?: CampaignStatus;
  page?: number;
  pageSize?: number;
}

export interface CampaignDto {
  id: number;
  name: string;
  description: string | null;
  status: CampaignStatus;
  channel: MessagingChannel;
  messageTemplateId: number;
  messageTemplateName: string;
  maxMessages: number;
  messagesPerMinute: number;
  messagesPerHour: number;
  messagesPerDay: number;
  startDate: string | null;
  endDate: string | null;
  recipientsCount: number;
  sentCount: number;
  respondedCount: number;
  createdAt: string;
}

export interface CreateCampaignRequest {
  name: string;
  description?: string;
  channel: MessagingChannel;
  messageTemplateId: number;
  maxMessages?: number;
  messagesPerMinute?: number;
  messagesPerHour?: number;
  messagesPerDay?: number;
}

export interface AddRecipientsResultDto {
  added: number;
  alreadyInCampaign: number;
  withoutValidContact: number;
  suppressed: number;
}

export interface ProcessQueueResultDto {
  processed: number;
  sent: number;
  failed: number;
  suppressed: number;
}

export async function searchCampaigns(query: CampaignQuery = {}): Promise<PagedResult<CampaignListItem>> {
  const response = await apiClient.get<ApiResponse<PagedResult<CampaignListItem>>>('/campaigns', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las campañas.');
  }
  return response.data.data;
}

export async function createCampaign(request: CreateCampaignRequest): Promise<CampaignDto> {
  const response = await apiClient.post<ApiResponse<CampaignDto>>('/campaigns', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear la campaña.');
  }
  return response.data.data;
}

export async function addCampaignRecipients(campaignId: number, prospectIds: number[]): Promise<AddRecipientsResultDto> {
  const response = await apiClient.post<ApiResponse<AddRecipientsResultDto>>(`/campaigns/${campaignId}/recipients`, {
    prospectIds,
  });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron agregar los destinatarios.');
  }
  return response.data.data;
}

export async function startCampaign(campaignId: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/campaigns/${campaignId}/start`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo iniciar la campaña.');
  }
}

export async function processCampaignQueue(campaignId: number, batchSize = 50): Promise<ProcessQueueResultDto> {
  const response = await apiClient.post<ApiResponse<ProcessQueueResultDto>>(
    `/campaigns/${campaignId}/process-queue`,
    null,
    { params: { batchSize } },
  );
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo procesar la cola de envío.');
  }
  return response.data.data;
}
