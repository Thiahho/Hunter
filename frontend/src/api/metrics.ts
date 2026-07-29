import { apiClient } from './client';
import type { ApiResponse } from './types';

export interface DashboardMetrics {
  prospectsFound: number;
  prospectsValid: number;
  prospectsContacted: number;
  messagesSent: number;
  responses: number;
  interested: number;
  leads: number;
  salesWon: number;
  salesLost: number;
  revenue: number;
  costTotal: number;
  responseRatePct: number | null;
  interestRatePct: number | null;
  leadConversionRatePct: number | null;
  salesConversionRatePct: number | null;
  costPerLead: number | null;
  costPerSale: number | null;
  averageTicket: number | null;
}

export async function fetchDashboardMetrics(): Promise<DashboardMetrics> {
  const response = await apiClient.get<ApiResponse<DashboardMetrics>>('/metrics/dashboard');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message ?? 'No se pudieron obtener las métricas del dashboard.');
  }
  return response.data.data;
}
