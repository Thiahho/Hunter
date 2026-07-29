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
  | 'Invalid';

export type BusinessSize = 'Unknown' | 'Micro' | 'Small' | 'Medium' | 'Large';
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
  city: string | null;
  province: string | null;
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
  recurrencePotential: string;
  address: string | null;
  city: string | null;
  province: string | null;
  country: string | null;
  postalCode: string | null;
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
