import { apiClient } from './client';
import type { ApiResponse } from './types';
import type { MessagingChannel } from './messages';

export interface MessageTemplateDto {
  id: number;
  name: string;
  content: string;
  channel: MessagingChannel;
  version: number;
  isActive: boolean;
  isCatalogTemplate: boolean;
}

export interface CreateMessageTemplateRequest {
  name: string;
  content: string;
  channel: MessagingChannel;
}

export interface UpdateMessageTemplateRequest {
  name: string;
  content: string;
}

// Reflejo de una plantilla tal como está aprobada en Meta Business Manager (Graph API) — no
// existe en la base de Hunter hasta que se sincroniza con syncTemplateFromMeta.
export interface MetaWhatsAppTemplateDto {
  name: string;
  language: string;
  status: string;
  bodyText: string | null;
}

export interface SyncMessageTemplateFromMetaRequest {
  name: string;
  language: string;
}

export async function listTemplates(): Promise<MessageTemplateDto[]> {
  const response = await apiClient.get<ApiResponse<MessageTemplateDto[]>>('/templates');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las plantillas.');
  }
  return response.data.data;
}

export async function createTemplate(request: CreateMessageTemplateRequest): Promise<MessageTemplateDto> {
  const response = await apiClient.post<ApiResponse<MessageTemplateDto>>('/templates', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear la plantilla.');
  }
  return response.data.data;
}

export async function updateTemplate(id: number, request: UpdateMessageTemplateRequest): Promise<MessageTemplateDto> {
  const response = await apiClient.put<ApiResponse<MessageTemplateDto>>(`/templates/${id}`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo actualizar la plantilla.');
  }
  return response.data.data;
}

export async function setTemplateActive(id: number, isActive: boolean): Promise<void> {
  const response = await apiClient.patch<ApiResponse<boolean>>(`/templates/${id}/status`, isActive);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo cambiar el estado de la plantilla.');
  }
}

export async function setTemplateCatalog(id: number): Promise<void> {
  const response = await apiClient.patch<ApiResponse<boolean>>(`/templates/${id}/catalog`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo marcar la plantilla como catálogo.');
  }
}

export async function listMetaTemplates(): Promise<MetaWhatsAppTemplateDto[]> {
  const response = await apiClient.get<ApiResponse<MetaWhatsAppTemplateDto[]>>('/templates/meta');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las plantillas aprobadas en Meta.');
  }
  return response.data.data;
}

export async function syncTemplateFromMeta(request: SyncMessageTemplateFromMetaRequest): Promise<MessageTemplateDto> {
  const response = await apiClient.post<ApiResponse<MessageTemplateDto>>('/templates/meta/sync', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo sincronizar la plantilla desde Meta.');
  }
  return response.data.data;
}
