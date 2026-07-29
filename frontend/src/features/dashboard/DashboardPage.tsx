import { useQuery } from '@tanstack/react-query';
import { fetchDashboardMetrics } from '../../api/metrics';

function formatNumber(value: number): string {
  return new Intl.NumberFormat('es-AR').format(value);
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(value);
}

function formatPct(value: number | null): string {
  if (value === null) return '—';
  return `${new Intl.NumberFormat('es-AR', { maximumFractionDigits: 1 }).format(value)}%`;
}

interface KpiCardProps {
  label: string;
  value: string;
}

function KpiCard({ label, value }: KpiCardProps) {
  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
      <p className="text-xs font-medium text-slate-500 dark:text-slate-400">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-slate-900 dark:text-slate-100">{value}</p>
    </div>
  );
}

export function DashboardPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['metrics', 'dashboard'],
    queryFn: fetchDashboardMetrics,
  });

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando métricas...</p>;
  }

  if (isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {error instanceof Error ? error.message : 'Ocurrió un error al cargar el dashboard.'}
      </p>
    );
  }

  const metrics = data!;

  return (
    <div className="space-y-6">
      <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Dashboard</h2>

      <section>
        <h3 className="mb-2 text-sm font-medium text-slate-500 dark:text-slate-400">Embudo</h3>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          <KpiCard label="Prospectos encontrados" value={formatNumber(metrics.prospectsFound)} />
          <KpiCard label="Prospectos válidos" value={formatNumber(metrics.prospectsValid)} />
          <KpiCard label="Prospectos contactados" value={formatNumber(metrics.prospectsContacted)} />
          <KpiCard label="Mensajes enviados" value={formatNumber(metrics.messagesSent)} />
          <KpiCard label="Respuestas" value={formatNumber(metrics.responses)} />
          <KpiCard label="Interesados" value={formatNumber(metrics.interested)} />
          <KpiCard label="Leads" value={formatNumber(metrics.leads)} />
          <KpiCard label="Ventas ganadas" value={formatNumber(metrics.salesWon)} />
          <KpiCard label="Ventas perdidas" value={formatNumber(metrics.salesLost)} />
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-medium text-slate-500 dark:text-slate-400">Tasas de conversión</h3>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <KpiCard label="Tasa de respuesta" value={formatPct(metrics.responseRatePct)} />
          <KpiCard label="Tasa de interés" value={formatPct(metrics.interestRatePct)} />
          <KpiCard label="Conversión a lead" value={formatPct(metrics.leadConversionRatePct)} />
          <KpiCard label="Conversión a venta" value={formatPct(metrics.salesConversionRatePct)} />
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-medium text-slate-500 dark:text-slate-400">Finanzas</h3>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <KpiCard label="Ingresos" value={formatCurrency(metrics.revenue)} />
          <KpiCard label="Costo total" value={formatCurrency(metrics.costTotal)} />
          <KpiCard label="Costo por lead" value={metrics.costPerLead !== null ? formatCurrency(metrics.costPerLead) : '—'} />
          <KpiCard label="Costo por venta" value={metrics.costPerSale !== null ? formatCurrency(metrics.costPerSale) : '—'} />
          <KpiCard label="Ticket promedio" value={metrics.averageTicket !== null ? formatCurrency(metrics.averageTicket) : '—'} />
        </div>
      </section>
    </div>
  );
}
