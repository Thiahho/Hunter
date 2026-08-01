import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { fetchDashboardMetrics } from '../../api/metrics';
import { searchLeads, type LeadListItem, type LeadStatus } from '../../api/leads';

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

const statusLabels: Record<LeadStatus, string> = {
  New: 'Nuevo',
  InProgress: 'En progreso',
  Won: 'Ganado',
  Lost: 'Perdido',
};

const statusColors: Record<LeadStatus, string> = {
  New: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  InProgress: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
  Won: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
  Lost: 'bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-300',
};

function formatAddress(lead: LeadListItem): string {
  const parts = [lead.prospectAddress, lead.prospectCity, lead.prospectProvince].filter(Boolean);
  return parts.length > 0 ? parts.join(', ') : '—';
}

function googleMapsUrl(lead: LeadListItem): string | null {
  if (lead.prospectLatitude !== null && lead.prospectLongitude !== null) {
    return `https://www.google.com/maps/search/?api=1&query=${lead.prospectLatitude},${lead.prospectLongitude}`;
  }
  const parts = [lead.prospectAddress, lead.prospectCity, lead.prospectProvince, lead.prospectCountry].filter(Boolean);
  if (parts.length === 0) return null;
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(parts.join(', '))}`;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('es-AR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

function BotContactsSection() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['leads', 'bot-registry'],
    queryFn: () => searchLeads({ pageSize: 100 }),
  });

  return (
    <section>
      <h3 className="mb-2 text-sm font-medium text-slate-500 dark:text-slate-400">
        Clientes contactados por el bot
      </h3>

      {isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando registro...</p>}

      {isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {error instanceof Error ? error.message : 'Ocurrió un error al cargar el registro.'}
        </p>
      )}

      {data && (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800">
            <thead className="bg-slate-50 dark:bg-slate-900/50">
              <tr>
                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400">Empresa</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400">Dirección</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400">Estado</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400">Fecha</th>
                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400">Mapa</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 bg-white dark:divide-slate-800 dark:bg-slate-900">
              {data.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-slate-500 dark:text-slate-400">
                    Todavía no hay clientes que hayan respondido al bot.
                  </td>
                </tr>
              )}
              {data.items.map((lead) => {
                const mapsUrl = googleMapsUrl(lead);
                return (
                  <tr key={lead.id}>
                    <td className="px-4 py-2 text-sm">
                      <Link
                        to={`/app/leads/${lead.id}`}
                        className="font-medium text-slate-900 hover:text-indigo-600 dark:text-slate-100 dark:hover:text-indigo-400"
                      >
                        {lead.prospectBusinessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-sm text-slate-600 dark:text-slate-300">{formatAddress(lead)}</td>
                    <td className="px-4 py-2 text-sm">
                      <span className={`inline-block rounded px-1.5 py-0.5 text-xs ${statusColors[lead.status]}`}>
                        {statusLabels[lead.status]}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sm text-slate-600 dark:text-slate-300">{formatDate(lead.createdAt)}</td>
                    <td className="px-4 py-2 text-sm">
                      {mapsUrl ? (
                        <a
                          href={mapsUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-indigo-600 hover:underline dark:text-indigo-400"
                        >
                          Ver en Maps
                        </a>
                      ) : (
                        <span className="text-slate-400 dark:text-slate-600">Sin dirección</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
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

      <BotContactsSection />
    </div>
  );
}
