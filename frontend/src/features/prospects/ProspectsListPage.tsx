import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { searchProspects, type ProspectCategory, type ProspectStatus } from '../../api/prospects';

const categories: ProspectCategory[] = [
  'Unknown',
  'Distributor',
  'AutoPartsStore',
  'Workshop',
  'Lubricentro',
  'TireShop',
  'Reseller',
  'Other',
];

const statuses: ProspectStatus[] = [
  'New',
  'Validated',
  'Ready',
  'Contacted',
  'Responded',
  'NotInterested',
  'NoResponse',
  'Lead',
  'Customer',
  'Suppressed',
  'Invalid',
];

const statusLabels: Record<ProspectStatus, string> = {
  New: 'Nuevo',
  Validated: 'Validado',
  Ready: 'Listo',
  Contacted: 'Contactado',
  Responded: 'Respondió',
  NotInterested: 'No interesado',
  NoResponse: 'Sin respuesta',
  Lead: 'Lead',
  Customer: 'Cliente',
  Suppressed: 'Suprimido',
  Invalid: 'Inválido',
};

const PAGE_SIZE = 20;

export function ProspectsListPage() {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState<ProspectCategory | ''>('');
  const [status, setStatus] = useState<ProspectStatus | ''>('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['prospects', { search, category, status, page }],
    queryFn: () =>
      searchProspects({
        search: search || undefined,
        category: category || undefined,
        status: status || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Prospectos</h2>
        <Link
          to="/app/prospects/new"
          className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500"
        >
          Nuevo prospecto
        </Link>
      </div>

      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Buscar por nombre..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
        />
        <select
          value={category}
          onChange={(e) => {
            setCategory(e.target.value as ProspectCategory | '');
            setPage(1);
          }}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
        >
          <option value="">Todas las categorías</option>
          {categories.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as ProspectStatus | '');
            setPage(1);
          }}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
        >
          <option value="">Todos los estados</option>
          {statuses.map((s) => (
            <option key={s} value={s}>
              {statusLabels[s]}
            </option>
          ))}
        </select>
      </div>

      {isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando prospectos...</p>}

      {isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {error instanceof Error ? error.message : 'Ocurrió un error al cargar los prospectos.'}
        </p>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Negocio</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Categoría</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Ciudad</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Contacto</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {data.items.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${p.id}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {p.businessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.category}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.city ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{statusLabels[p.status]}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.primaryContactValue ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                      No se encontraron prospectos.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm text-slate-500 dark:text-slate-400">
            <span>
              Página {data.page} de {Math.max(data.totalPages, 1)} · {data.totalItems} prospectos
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={data.page <= 1}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
              >
                Anterior
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={data.page >= data.totalPages}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
