import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';
import type { MessagingChannel } from './messages';

export type CampaignStatus = 'Draft' | 'Ready' | 'Running' | 'Paused' | 'Completed' | 'Cancelled';

export interface CampaignListItem {
  id: number;
  name: string;
  status: CampaignStatus;
  channel: MessagingChannel;
  recipientsCount: number;
  sentCount: number;
  createdAt: string;
}

export interface CampaignQuery {
  status?: CampaignStatus;
  page?: number;
  pageSize?: number;
}

export async function searchCampaigns(query: CampaignQuery = {}): Promise<PagedResult<CampaignListItem>> {
  const response = await apiClient.get<ApiResponse<PagedResult<CampaignListItem>>>('/campaigns', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las campañas.');
  }
  return response.data.data;
}
