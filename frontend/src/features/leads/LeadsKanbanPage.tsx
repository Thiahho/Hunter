import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { markLeadLost, searchLeads, type Lead, type LeadListItem, type LeadStatus } from '../../api/leads';

const columns: { status: LeadStatus; label: string }[] = [
  { status: 'New', label: 'Nuevo' },
  { status: 'InProgress', label: 'En progreso' },
  { status: 'Won', label: 'Ganado' },
  { status: 'Lost', label: 'Perdido' },
];

const priorityColors: Record<Lead['priority'], string> = {
  Low: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Medium: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
  High: 'bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-300',
};

function LeadCard({ lead }: { lead: LeadListItem }) {
  const queryClient = useQueryClient();

  const closeMutation = useMutation({
    mutationFn: () => markLeadLost(lead.id, { lostReason: 'Other', notes: 'Cerrado rápido desde el Kanban' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['leads'] }),
  });

  const canClose = lead.status === 'New' || lead.status === 'InProgress';

  return (
    <div className="rounded-md border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 hover:border-indigo-300 dark:hover:border-indigo-700">
      <Link to={`/app/leads/${lead.id}`} className="block">
        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{lead.prospectBusinessName}</p>
        <span className={`mt-1 inline-block rounded px-1.5 py-0.5 text-xs ${priorityColors[lead.priority]}`}>
          {lead.priority}
        </span>
      </Link>
      {canClose && (
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault();
            closeMutation.mutate();
          }}
          disabled={closeMutation.isPending}
          title="Marca el lead como perdido (motivo genérico), para liberar la notificación de handoff en el próximo mensaje del prospecto"
          className="mt-2 w-full rounded-md border border-slate-200 dark:border-slate-700 px-2 py-1 text-xs font-medium text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
        >
          {closeMutation.isPending ? 'Cerrando…' : 'Cerrar (rápido)'}
        </button>
      )}
      {closeMutation.isError && (
        <p className="mt-1 text-xs text-red-600 dark:text-red-400">
          {closeMutation.error instanceof Error ? closeMutation.error.message : 'No se pudo cerrar.'}
        </p>
      )}
    </div>
  );
}

export function LeadsKanbanPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['leads', 'kanban'],
    queryFn: () => searchLeads({ pageSize: 200 }),
  });

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando leads...</p>;
  }

  if (isError || !data) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {error instanceof Error ? error.message : 'Ocurrió un error al cargar los leads.'}
      </p>
    );
  }

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Leads</h2>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {columns.map((col) => {
          const items = data.items.filter((l) => l.status === col.status);
          return (
            <div key={col.status} className="rounded-lg bg-slate-100 dark:bg-slate-900/50 p-3">
              <h3 className="mb-3 flex items-center justify-between text-sm font-medium text-slate-600 dark:text-slate-300">
                {col.label}
                <span className="rounded-full bg-slate-200 dark:bg-slate-800 px-2 py-0.5 text-xs">{items.length}</span>
              </h3>
              <div className="space-y-2">
                {items.map((lead) => (
                  <LeadCard key={lead.id} lead={lead} />
                ))}
                {items.length === 0 && <p className="text-xs text-slate-400">Sin leads.</p>}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
