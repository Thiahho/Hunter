import { apiClient } from './client';
import type { ApiResponse } from './types';

export type ScheduledMessageStatus = 'Pending' | 'Running' | 'Sent' | 'Failed' | 'Cancelled';

export interface ScheduledMessageDto {
  id: number;
  prospectId: number;
  messageTemplateId: number;
  messageTemplateName: string;
  scheduledAt: string;
  status: ScheduledMessageStatus;
  runAt: string | null;
  messageId: number | null;
  failureReason: string | null;
  source: string;
  createdAt: string;
}

export interface ScheduleMessageRequest {
  messageTemplateId: number;
  scheduledAt: string;
}

export async function listScheduledMessages(prospectId: number): Promise<ScheduledMessageDto[]> {
  const response = await apiClient.get<ApiResponse<ScheduledMessageDto[]>>(`/prospects/${prospectId}/scheduled-messages`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los mensajes programados.');
  }
  return response.data.data;
}

export async function scheduleMessage(prospectId: number, request: ScheduleMessageRequest): Promise<ScheduledMessageDto> {
  const response = await apiClient.post<ApiResponse<ScheduledMessageDto>>(`/prospects/${prospectId}/scheduled-messages`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo programar el mensaje.');
  }
  return response.data.data;
}

export async function cancelScheduledMessage(id: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/prospects/scheduled-messages/${id}/cancel`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo cancelar el mensaje programado.');
  }
}
