import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';

export type MessagingChannel = 'Whatsapp' | 'Email' | 'Telegram' | 'Sms';
export type MessageStatus = 'Pending' | 'Sent' | 'Delivered' | 'Read' | 'Failed' | 'Cancelled';
export type IntentClassification = 'Interested' | 'NotInterested' | 'Question' | 'Unclear' | 'Stop' | 'AutomatedReply';

export interface MessageDto {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
  prospectPhone: string | null;
  campaignId: number | null;
  channel: MessagingChannel;
  content: string;
  status: MessageStatus;
  externalMessageId: string | null;
  sentAt: string | null;
  deliveredAt: string | null;
  readAt: string | null;
  failedAt: string | null;
  failureReason: string | null;
  cost: number | null;
  currency: string | null;
  createdAt: string;
}

export interface MessageResponseDto {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
  prospectPhone: string | null;
  campaignId: number | null;
  messageId: number | null;
  content: string;
  receivedAt: string;
  classification: IntentClassification;
  confidence: number;
  buttonPayload: string | null;
  processedAt: string | null;
}

export interface MessageQuery {
  search?: string;
  campaignId?: number;
  prospectId?: number;
  status?: MessageStatus;
  page?: number;
  pageSize?: number;
}

export interface MessageResponseQuery {
  search?: string;
  campaignId?: number;
  prospectId?: number;
  classification?: IntentClassification;
  page?: number;
  pageSize?: number;
}

export interface DailyProspectDto {
  prospectId: number;
  businessName: string;
  province: string | null;
  city: string | null;
  phone: string | null;
  createdAt: string;
  sent: boolean;
  attempts: number;
  lastAttemptAt: string | null;
  lastStatus: MessageStatus | null;
  retryTargetId: number | null;
  isCampaignRecipient: boolean;
  campaignId: number | null;
}

export interface DailyProspectQuery {
  search?: string;
  date?: string;
  province?: string;
  city?: string;
  sent?: boolean;
  page?: number;
  pageSize?: number;
}

export interface FailedContactsQuery {
  date?: string;
  province?: string;
  city?: string;
}

export async function searchMessages(query: MessageQuery): Promise<PagedResult<MessageDto>> {
  const response = await apiClient.get<ApiResponse<PagedResult<MessageDto>>>('/messages', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los mensajes.');
  }
  return response.data.data;
}

export async function searchMessageResponses(query: MessageResponseQuery): Promise<PagedResult<MessageResponseDto>> {
  const response = await apiClient.get<ApiResponse<PagedResult<MessageResponseDto>>>('/messages/responses', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las respuestas.');
  }
  return response.data.data;
}

export async function searchDailyProspects(query: DailyProspectQuery): Promise<PagedResult<DailyProspectDto>> {
  const response = await apiClient.get<ApiResponse<PagedResult<DailyProspectDto>>>('/messages/daily', { params: query });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los prospectos del día.');
  }
  return response.data.data;
}

export async function exportFailedContacts(query: FailedContactsQuery): Promise<void> {
  const response = await apiClient.get('/messages/export/failed', { params: query, responseType: 'blob' });
  const url = URL.createObjectURL(response.data as Blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `no-contactados-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export interface TestMessageResultDto {
  messageId: number;
  success: boolean;
  externalMessageId: string | null;
  error: string | null;
}

export async function retryMessage(id: number): Promise<TestMessageResultDto> {
  const response = await apiClient.post<ApiResponse<TestMessageResultDto>>(`/messages/${id}/retry`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo reintentar el envío.');
  }
  return response.data.data;
}

export async function deleteMessage(id: number): Promise<void> {
  const response = await apiClient.delete<ApiResponse<boolean>>(`/messages/${id}`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo borrar el mensaje.');
  }
}

export async function bulkDeleteMessages(ids: number[]): Promise<number> {
  const response = await apiClient.post<ApiResponse<number>>('/messages/bulk-delete', { ids });
  if (!response.data.success || response.data.data === null) {
    throw new Error(response.data.message ?? 'No se pudieron borrar los mensajes.');
  }
  return response.data.data;
}

export async function deleteMessageResponse(id: number): Promise<void> {
  const response = await apiClient.delete<ApiResponse<boolean>>(`/messages/responses/${id}`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo borrar la respuesta.');
  }
}

export async function bulkDeleteMessageResponses(ids: number[]): Promise<number> {
  const response = await apiClient.post<ApiResponse<number>>('/messages/responses/bulk-delete', { ids });
  if (!response.data.success || response.data.data === null) {
    throw new Error(response.data.message ?? 'No se pudieron borrar las respuestas.');
  }
  return response.data.data;
}
