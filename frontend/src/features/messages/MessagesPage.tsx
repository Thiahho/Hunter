import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import {
  bulkDeleteMessages,
  deleteMessage,
  searchMessageResponses,
  searchMessages,
  type IntentClassification,
  type MessageDto,
  type MessageStatus,
} from '../../api/messages';
import { ConfirmDialog } from '../../components/ConfirmDialog';

function formatCost(cost: number | null, currency: string | null): string {
  if (cost === null) return `0 ${currency ?? 'ARS'}`;
  return `${cost.toLocaleString('es-AR', { minimumFractionDigits: 2 })} ${currency ?? 'ARS'}`;
}

function sumCost(messages: MessageDto[]): { total: number; currency: string | null } {
  const currency = messages.find((m) => m.currency)?.currency ?? null;
  const total = messages.reduce((acc, m) => acc + (m.cost ?? 0), 0);
  return { total, currency };
}

const PAGE_SIZE = 30;

const statusLabels: Record<MessageStatus, string> = {
  Pending: 'Pendiente',
  Sent: 'Enviado',
  Delivered: 'Entregado',
  Read: 'Leído',
  Failed: 'Falló',
  Cancelled: 'Cancelado',
};

const statusOptions: MessageStatus[] = ['Pending', 'Sent', 'Delivered', 'Read', 'Failed', 'Cancelled'];

const classificationLabels: Record<IntentClassification, string> = {
  Interested: 'Interesado',
  NotInterested: 'No interesado',
  Question: 'Pregunta',
  Unclear: 'Sin clasificar',
  Stop: 'Baja (STOP)',
};

const classificationOptions: IntentClassification[] = ['Interested', 'NotInterested', 'Question', 'Unclear', 'Stop'];

const classificationBadgeClass: Record<IntentClassification, string> = {
  Interested: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
  Question: 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300',
  NotInterested: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Stop: 'bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-300',
  Unclear: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
};

const selectClass =
  'rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100';

function Pager({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  return (
    <div className="flex items-center justify-end gap-2 text-sm text-slate-500 dark:text-slate-400">
      <span>
        Página {page} de {Math.max(totalPages, 1)}
      </span>
      <button
        onClick={() => onChange(Math.max(1, page - 1))}
        disabled={page <= 1}
        className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
      >
        Anterior
      </button>
      <button
        onClick={() => onChange(page + 1)}
        disabled={page >= totalPages}
        className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1 disabled:opacity-40"
      >
        Siguiente
      </button>
    </div>
  );
}

type PendingMessageDeletion = { kind: 'single'; message: MessageDto } | { kind: 'bulk'; messages: MessageDto[] };

function SentMessagesTab() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<MessageStatus | ''>('');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [pendingDeletion, setPendingDeletion] = useState<PendingMessageDeletion | null>(null);

  const query = useQuery({
    queryKey: ['messages', { status, page }],
    queryFn: () => searchMessages({ status: status || undefined, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  });

  function changePage(next: number) {
    setPage(next);
    setSelectedIds(new Set());
  }

  const deleteMutation = useMutation({
    mutationFn: async (target: PendingMessageDeletion) => {
      if (target.kind === 'single') {
        await deleteMessage(target.message.id);
      } else {
        await bulkDeleteMessages(target.messages.map((m) => m.id));
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      setPendingDeletion(null);
      queryClient.invalidateQueries({ queryKey: ['messages'] });
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
    if (!query.data) return;
    setSelectedIds((prev) => {
      const allSelected = query.data.items.length > 0 && query.data.items.every((m) => prev.has(m.id));
      return allSelected ? new Set() : new Set(query.data.items.map((m) => m.id));
    });
  }

  const allOnPageSelected =
    !!query.data && query.data.items.length > 0 && query.data.items.every((m) => selectedIds.has(m.id));
  const selectedMessages = query.data?.items.filter((m) => selectedIds.has(m.id)) ?? [];

  return (
    <div className="space-y-3">
      <select
        value={status}
        onChange={(e) => {
          setStatus(e.target.value as MessageStatus | '');
          changePage(1);
        }}
        className={selectClass}
      >
        <option value="">Todos los estados</option>
        {statusOptions.map((s) => (
          <option key={s} value={s}>
            {statusLabels[s]}
          </option>
        ))}
      </select>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar los mensajes.'}
        </p>
      )}

      {query.data && (
        <>
          {selectedIds.size > 0 && (
            <div className="flex items-center justify-between rounded-md border border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/10 px-4 py-2 text-sm">
              <span className="text-indigo-700 dark:text-indigo-300">{selectedIds.size} seleccionado(s)</span>
              <button
                type="button"
                onClick={() => setPendingDeletion({ kind: 'bulk', messages: selectedMessages })}
                className="rounded-md bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-500"
              >
                Borrar seleccionados
              </button>
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
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Destinatario</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Canal</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Mensaje</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Enviado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((m) => (
                  <tr key={m.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(m.id)}
                        onChange={() => toggleSelected(m.id)}
                        aria-label={`Seleccionar mensaje a ${m.prospectBusinessName}`}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    </td>
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${m.prospectId}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {m.prospectBusinessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{m.channel}</td>
                    <td className="px-4 py-2 max-w-md truncate text-slate-600 dark:text-slate-300" title={m.content}>
                      {m.content}
                    </td>
                    <td className="px-4 py-2">
                      <span className="text-slate-600 dark:text-slate-300">{statusLabels[m.status]}</span>
                      {m.status === 'Failed' && m.failureReason && (
                        <p className="text-xs text-red-500 dark:text-red-400" title={m.failureReason}>
                          {m.failureReason}
                        </p>
                      )}
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {m.sentAt ? new Date(m.sentAt).toLocaleString('es-AR') : '—'}
                    </td>
                    <td className="px-4 py-2">
                      <button
                        type="button"
                        onClick={() => setPendingDeletion({ kind: 'single', message: m })}
                        disabled={deleteMutation.isPending}
                        className="text-xs font-medium text-red-600 dark:text-red-400 hover:underline disabled:opacity-60"
                      >
                        Borrar
                      </button>
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-slate-400">
                      Sin mensajes.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pager page={query.data.page} totalPages={query.data.totalPages} onChange={changePage} />
        </>
      )}

      {pendingDeletion && (
        <ConfirmDialog
          title={pendingDeletion.kind === 'single' ? 'Borrar mensaje' : 'Borrar mensajes seleccionados'}
          message={(() => {
            if (pendingDeletion.kind === 'single') {
              const { total, currency } = sumCost([pendingDeletion.message]);
              return `¿Borrar el mensaje a "${pendingDeletion.message.prospectBusinessName}"? Costo: ${formatCost(total, currency)}. Esta acción no se puede deshacer (no afecta el historial de Costos, que es independiente).`;
            }
            const { total, currency } = sumCost(pendingDeletion.messages);
            return `¿Borrar ${pendingDeletion.messages.length} mensaje(s) seleccionado(s)? Costo total: ${formatCost(total, currency)}. Esta acción no se puede deshacer (no afecta el historial de Costos, que es independiente).`;
          })()}
          confirmLabel="Borrar"
          danger
          isPending={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(pendingDeletion)}
          onCancel={() => setPendingDeletion(null)}
        />
      )}
    </div>
  );
}

function ResponsesTab() {
  const [classification, setClassification] = useState<IntentClassification | ''>('');
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ['message-responses', { classification, page }],
    queryFn: () => searchMessageResponses({ classification: classification || undefined, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  });

  return (
    <div className="space-y-3">
      <select
        value={classification}
        onChange={(e) => {
          setClassification(e.target.value as IntentClassification | '');
          setPage(1);
        }}
        className={selectClass}
      >
        <option value="">Todas las clasificaciones</option>
        {classificationOptions.map((c) => (
          <option key={c} value={c}>
            {classificationLabels[c]}
          </option>
        ))}
      </select>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar las respuestas.'}
        </p>
      )}

      {query.data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Prospecto</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Respuesta</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Clasificación</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Origen</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Recibido</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((r) => (
                  <tr key={r.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${r.prospectId}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {r.prospectBusinessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 max-w-md truncate text-slate-600 dark:text-slate-300" title={r.content}>
                      {r.content || '—'}
                    </td>
                    <td className="px-4 py-2">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${classificationBadgeClass[r.classification]}`}>
                        {classificationLabels[r.classification]}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {r.buttonPayload ? `Botón: ${r.buttonPayload}` : 'Texto libre'}
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {new Date(r.receivedAt).toLocaleString('es-AR')}
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                      Sin respuestas todavía.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pager page={query.data.page} totalPages={query.data.totalPages} onChange={setPage} />
        </>
      )}
    </div>
  );
}

export function MessagesPage() {
  const [tab, setTab] = useState<'sent' | 'responses'>('sent');

  const tabClass = (active: boolean) =>
    `rounded-md px-3 py-1.5 text-sm font-medium ${
      active
        ? 'bg-indigo-600 text-white'
        : 'border border-slate-300 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
    }`;

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Mensajes</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          A quién se le envió cada mensaje y su estado, y las respuestas recibidas (texto libre o botón "Estoy
          interesado") con la clasificación que les asignó el sistema.
        </p>
      </div>

      <div className="flex gap-2">
        <button className={tabClass(tab === 'sent')} onClick={() => setTab('sent')}>
          Enviados
        </button>
        <button className={tabClass(tab === 'responses')} onClick={() => setTab('responses')}>
          Respuestas
        </button>
      </div>

      {tab === 'sent' ? <SentMessagesTab /> : <ResponsesTab />}
    </div>
  );
}
