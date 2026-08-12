import { apiClient } from './client';
import type { ApiResponse, PagedResult } from './types';

export type ProspectCategory =
  | 'Unknown'
  | 'Distributor'
  | 'AutoPartsStore'
  | 'Workshop'
  | 'Lubricentro'
  | 'TireShop'
  | 'Reseller'
  | 'Other';

export type ProspectStatus =
  | 'New'
  | 'Validated'
  | 'Ready'
  | 'Contacted'
  | 'Responded'
  | 'NotInterested'
  | 'NoResponse'
  | 'Lead'
  | 'Customer'
  | 'Suppressed'
  | 'Invalid'
  | 'AutoReplyDetected';

export type BusinessSize = 'Unknown' | 'Micro' | 'Small' | 'Medium' | 'Large';
export type RecurrencePotential = 'Unknown' | 'Low' | 'Medium' | 'High';
export type OperationalPriority = 'PriorityD' | 'PriorityC' | 'PriorityB' | 'PriorityA';
export type ProspectContactChannel = 'Phone' | 'Whatsapp' | 'Email' | 'Instagram' | 'Facebook';
export type ProspectSourceType =
  | 'GooglePlaces'
  | 'OpenStreetMap'
  | 'Directory'
  | 'PublicWebsite'
  | 'CsvImport'
  | 'Manual'
  | 'ExternalApi'
  | 'Other';

export interface ProspectListItem {
  id: number;
  businessName: string;
  category: ProspectCategory;
  address: string | null;
  city: string | null;
  province: string | null;
  latitude: number | null;
  longitude: number | null;
  status: ProspectStatus;
  commercialScore: number | null;
  operationalPriority: OperationalPriority | null;
  primaryContactValue: string | null;
  createdAt: string;
}

export interface ProspectContact {
  id: number;
  channel: ProspectContactChannel;
  value: string;
  isPrimary: boolean;
  isVerified: boolean;
}

export interface ProspectSource {
  id: number;
  sourceType: ProspectSourceType;
  externalId: string | null;
  sourceUrl: string | null;
  collectedAt: string;
}

export interface Prospect {
  id: number;
  businessName: string;
  contactName: string | null;
  category: ProspectCategory;
  businessSize: BusinessSize;
  recurrencePotential: RecurrencePotential;
  address: string | null;
  city: string | null;
  province: string | null;
  country: string | null;
  postalCode: string | null;
  latitude: number | null;
  longitude: number | null;
  website: string | null;
  commercialScore: number | null;
  operationalPriority: OperationalPriority | null;
  status: ProspectStatus;
  createdAt: string;
  lastContactedAt: string | null;
  contacts: ProspectContact[];
  sources: ProspectSource[];
  tags: string[];
}

export interface ProspectQuery {
  search?: string;
  category?: ProspectCategory;
  city?: string;
  province?: string;
  status?: ProspectStatus;
  tag?: string;
  source?: ProspectSourceType;
  businessSize?: BusinessSize;
  page?: number;
  pageSize?: number;
}

export async function searchProspects(query: ProspectQuery): Promise<PagedResult<ProspectListItem>> {
  const response = await apiClient.get<ApiResponse<PagedResult<ProspectListItem>>>('/prospects', {
    params: query,
  });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los prospectos.');
  }
  return response.data.data;
}

export async function fetchProspectById(id: number): Promise<Prospect> {
  const response = await apiClient.get<ApiResponse<Prospect>>(`/prospects/${id}`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo obtener el prospecto.');
  }
  return response.data.data;
}

export interface CreateProspectRequest {
  businessName: string;
  contactName?: string | null;
  category: ProspectCategory;
  businessSize: BusinessSize;
  recurrencePotential: RecurrencePotential;
  address?: string | null;
  city?: string | null;
  province?: string | null;
  country?: string | null;
  postalCode?: string | null;
  website?: string | null;
  contacts: { channel: ProspectContactChannel; value: string; isPrimary: boolean }[];
  sourceType: ProspectSourceType;
}

export async function createProspect(request: CreateProspectRequest): Promise<Prospect> {
  const response = await apiClient.post<ApiResponse<Prospect>>('/prospects', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo crear el prospecto.');
  }
  return response.data.data;
}

export interface UpdateProspectRequest {
  businessName: string;
  contactName?: string | null;
  category: ProspectCategory;
  businessSize: BusinessSize;
  recurrencePotential: RecurrencePotential;
  address?: string | null;
  city?: string | null;
  province?: string | null;
  country?: string | null;
  postalCode?: string | null;
  website?: string | null;
  status: ProspectStatus;
}

export async function updateProspect(id: number, request: UpdateProspectRequest): Promise<Prospect> {
  const response = await apiClient.put<ApiResponse<Prospect>>(`/prospects/${id}`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo actualizar el prospecto.');
  }
  return response.data.data;
}

export async function deleteProspect(id: number): Promise<void> {
  const response = await apiClient.delete<ApiResponse<boolean>>(`/prospects/${id}`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo borrar el prospecto.');
  }
}

export interface AddProspectContactRequest {
  channel: ProspectContactChannel;
  value: string;
  isPrimary: boolean;
}

export interface UpdateProspectContactRequest {
  channel: ProspectContactChannel;
  value: string;
  isPrimary: boolean;
}

export async function addProspectContact(prospectId: number, request: AddProspectContactRequest): Promise<ProspectContact> {
  const response = await apiClient.post<ApiResponse<ProspectContact>>(`/prospects/${prospectId}/contacts`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo agregar el contacto.');
  }
  return response.data.data;
}

export async function updateProspectContact(
  prospectId: number,
  contactId: number,
  request: UpdateProspectContactRequest,
): Promise<ProspectContact> {
  const response = await apiClient.put<ApiResponse<ProspectContact>>(`/prospects/${prospectId}/contacts/${contactId}`, request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo actualizar el contacto.');
  }
  return response.data.data;
}

export async function removeProspectContact(prospectId: number, contactId: number): Promise<void> {
  const response = await apiClient.delete<ApiResponse<boolean>>(`/prospects/${prospectId}/contacts/${contactId}`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo eliminar el contacto.');
  }
}

export interface TestMessageResult {
  messageId: number;
  success: boolean;
  externalMessageId: string | null;
  error: string | null;
}

export async function sendTestMessage(prospectId: number, content: string): Promise<TestMessageResult> {
  const response = await apiClient.post<ApiResponse<TestMessageResult>>(`/prospects/${prospectId}/test-message`, {
    content,
  });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo enviar el mensaje de prueba.');
  }
  return response.data.data;
}

export async function sendTelegramAlert(prospectId: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/prospects/${prospectId}/telegram-alert`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo enviar la alerta a Telegram.');
  }
}
