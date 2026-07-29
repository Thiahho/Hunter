import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { searchLeads, type Lead, type LeadListItem, type LeadStatus } from '../../api/leads';

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
  return (
    <Link
      to={`/app/leads/${lead.id}`}
      className="block rounded-md border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 hover:border-indigo-300 dark:hover:border-indigo-700"
    >
      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{lead.prospectBusinessName}</p>
      <span className={`mt-1 inline-block rounded px-1.5 py-0.5 text-xs ${priorityColors[lead.priority]}`}>
        {lead.priority}
      </span>
    </Link>
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
