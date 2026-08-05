import { apiClient } from './client';
import type { ApiResponse } from './types';
import type { ProspectCategory } from './prospects';

export interface OpenStreetMapImportRequest {
  localities: string[];
  categories?: ProspectCategory[];
  radiusKm?: number;
  maxResults?: number;
  keywords?: string[];
}

export interface ImportPreviewDto {
  batchId: number;
  status: string;
  totalRecords: number;
  validRecords: number;
  duplicateRecords: number;
  invalidRecords: number;
}

export interface ImportRecordDto {
  id: number;
  rowNumber: number;
  status: 'Valid' | 'Duplicate' | 'Invalid' | 'Imported';
  businessName: string | null;
  category: string | null;
  phone: string | null;
  whatsapp: string | null;
  address: string | null;
  city: string | null;
  province: string | null;
  errorMessage: string | null;
}

export interface ImportConfirmResultDto {
  batchId: number;
  status: string;
  created: number;
}

export async function searchOpenStreetMap(request: OpenStreetMapImportRequest): Promise<ImportPreviewDto> {
  const response = await apiClient.post<ApiResponse<ImportPreviewDto>>('/imports/openstreetmap', request);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo buscar en OpenStreetMap.');
  }
  return response.data.data;
}

export async function getImportRecords(batchId: number): Promise<ImportRecordDto[]> {
  const response = await apiClient.get<ApiResponse<ImportRecordDto[]>>(`/imports/${batchId}/records`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener los resultados de la búsqueda.');
  }
  return response.data.data;
}

export async function confirmImport(batchId: number, selectedRecordIds?: number[]): Promise<ImportConfirmResultDto> {
  const response = await apiClient.post<ApiResponse<ImportConfirmResultDto>>(`/imports/${batchId}/confirm`, {
    selectedRecordIds,
  });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudo confirmar la importación.');
  }
  return response.data.data;
}

export async function cancelImport(batchId: number): Promise<void> {
  const response = await apiClient.post<ApiResponse<boolean>>(`/imports/${batchId}/cancel`);
  if (!response.data.success) {
    throw new Error(response.data.message ?? 'No se pudo cancelar la búsqueda.');
  }
}
