import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';

export type MessagingChannel = 'Whatsapp' | 'Email' | 'Telegram' | 'Sms';
export type MessageStatus = 'Pending' | 'Sent' | 'Delivered' | 'Read' | 'Failed' | 'Cancelled';
export type IntentClassification = 'Interested' | 'NotInterested' | 'Question' | 'Unclear' | 'Stop';

export interface MessageDto {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
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
  createdAt: string;
}

export interface MessageResponseDto {
  id: number;
  prospectId: number;
  prospectBusinessName: string;
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
  campaignId?: number;
  prospectId?: number;
  status?: MessageStatus;
  page?: number;
  pageSize?: number;
}

export interface MessageResponseQuery {
  campaignId?: number;
  prospectId?: number;
  classification?: IntentClassification;
  page?: number;
  pageSize?: number;
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
