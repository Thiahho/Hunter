import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { deleteProspect, searchProspects, type ProspectCategory, type ProspectListItem, type ProspectStatus } from '../../api/prospects';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { BulkSendMessageDialog } from './BulkSendMessageDialog';

function buildMapsLink(prospect: ProspectListItem): string | null {
  // typeof en vez de "!== null": si el backend todavía no manda estos campos (build vieja,
  // etc.) llegan como undefined, no como null, y "undefined !== null" da true igual — eso
  // generaba el link roto a "maps?query=undefined,undefined".
  if (typeof prospect.latitude === 'number' && typeof prospect.longitude === 'number') {
    return `https://www.google.com/maps/search/?api=1&query=${prospect.latitude},${prospect.longitude}`;
  }

  const parts = [prospect.address, prospect.city, prospect.province].filter(Boolean);
  if (parts.length === 0) return null;

  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${prospect.businessName}, ${parts.join(', ')}`)}`;
}

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

const categoryLabels: Record<ProspectCategory, string> = {
  Unknown: 'Sin clasificar',
  Distributor: 'Mayorista/Distribuidor',
  AutoPartsStore: 'Casa de repuestos',
  Workshop: 'Taller',
  Lubricentro: 'Lubricentro',
  TireShop: 'Gomería',
  Reseller: 'Revendedor',
  Other: 'Otro',
};

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
  'AutoReplyDetected',
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
  AutoReplyDetected: 'Bot detectado — reintentando',
};

const PAGE_SIZE = 20;

type PendingDeletion = { kind: 'single'; prospect: ProspectListItem } | { kind: 'bulk'; ids: number[] };

export function ProspectsListPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState<ProspectCategory | ''>('');
  const [status, setStatus] = useState<ProspectStatus | ''>('');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [pendingDeletion, setPendingDeletion] = useState<PendingDeletion | null>(null);
  const [showBulkSend, setShowBulkSend] = useState(false);

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

  function changePage(next: number) {
    setPage(next);
    setSelectedIds(new Set());
  }

  const deleteMutation = useMutation({
    mutationFn: async (target: PendingDeletion) => {
      if (target.kind === 'single') {
        await deleteProspect(target.prospect.id);
      } else {
        await Promise.all(target.ids.map((id) => deleteProspect(id)));
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      setPendingDeletion(null);
      queryClient.invalidateQueries({ queryKey: ['prospects'] });
    },
  });

  function toggleSelected(id: number) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function toggleSelectAllOnPage() {
    if (!data) return;
    setSelectedIds((prev) => {
      const allSelected = data.items.length > 0 && data.items.every((p) => prev.has(p.id));
      return allSelected ? new Set() : new Set(data.items.map((p) => p.id));
    });
  }

  const allOnPageSelected = !!data && data.items.length > 0 && data.items.every((p) => selectedIds.has(p.id));

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
            changePage(1);
          }}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
        />
        <select
          value={category}
          onChange={(e) => {
            setCategory(e.target.value as ProspectCategory | '');
            changePage(1);
          }}
          className="rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
        >
          <option value="">Todas las categorías</option>
          {categories.map((c) => (
            <option key={c} value={c}>
              {categoryLabels[c]}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as ProspectStatus | '');
            changePage(1);
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
          {selectedIds.size > 0 && (
            <div className="flex items-center justify-between rounded-md border border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/10 px-4 py-2 text-sm">
              <span className="text-indigo-700 dark:text-indigo-300">{selectedIds.size} seleccionado(s)</span>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setShowBulkSend(true)}
                  className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500"
                >
                  Enviar mensaje
                </button>
                <button
                  type="button"
                  onClick={() => setPendingDeletion({ kind: 'bulk', ids: [...selectedIds] })}
                  className="rounded-md bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-500"
                >
                  Borrar seleccionados
                </button>
              </div>
            </div>
          )}

          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-4 py-2 text-left">
                    <input
                      type="checkbox"
                      checked={allOnPageSelected}
                      onChange={toggleSelectAllOnPage}
                      aria-label="Seleccionar todos los de esta página"
                      className="rounded border-slate-300 dark:border-slate-700"
                    />
                  </th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Negocio</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Categoría</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Ciudad</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Contacto</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Mapa</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {data.items.map((p) => {
                  const mapsLink = buildMapsLink(p);
                  return (
                  <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(p.id)}
                        onChange={() => toggleSelected(p.id)}
                        aria-label={`Seleccionar ${p.businessName}`}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    </td>
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${p.id}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {p.businessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{categoryLabels[p.category]}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.city ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{statusLabels[p.status]}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.primaryContactValue ?? '—'}</td>
                    <td className="px-4 py-2">
                      {mapsLink ? (
                        <a
                          href={mapsLink}
                          target="_blank"
                          rel="noreferrer"
                          className="text-indigo-600 dark:text-indigo-400 hover:underline"
                        >
                          📍 Ver mapa
                        </a>
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-2">
                      <div className="flex gap-2">
                        <Link
                          to={`/app/prospects/${p.id}`}
                          className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                        >
                          Editar
                        </Link>
                        <button
                          type="button"
                          onClick={() => setPendingDeletion({ kind: 'single', prospect: p })}
                          disabled={deleteMutation.isPending}
                          className="text-xs font-medium text-red-600 dark:text-red-400 hover:underline disabled:opacity-60"
                        >
                          Borrar
                        </button>
                      </div>
                    </td>
                  </tr>
                  );
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-4 py-6 text-center text-slate-400">
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
                onClick={() => changePage(Math.max(1, page - 1))}
                disabled={data.page <= 1}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
              >
                Anterior
              </button>
              <button
                onClick={() => changePage(page + 1)}
                disabled={data.page >= data.totalPages}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}

      {pendingDeletion && (
        <ConfirmDialog
          title={pendingDeletion.kind === 'single' ? 'Borrar prospecto' : 'Borrar prospectos seleccionados'}
          message={
            pendingDeletion.kind === 'single'
              ? `¿Borrar "${pendingDeletion.prospect.businessName}"? Esta acción no se puede deshacer.`
              : `¿Borrar ${pendingDeletion.ids.length} prospecto(s) seleccionado(s)? Esta acción no se puede deshacer.`
          }
          confirmLabel="Borrar"
          danger
          isPending={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(pendingDeletion)}
          onCancel={() => setPendingDeletion(null)}
        />
      )}

      {showBulkSend && (
        <BulkSendMessageDialog
          prospectIds={[...selectedIds]}
          onClose={() => setShowBulkSend(false)}
          onSent={() => {
            setSelectedIds(new Set());
            queryClient.invalidateQueries({ queryKey: ['prospects'] });
          }}
        />
      )}
    </div>
  );
}
