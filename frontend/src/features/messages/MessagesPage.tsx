import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import {
  bulkDeleteMessageResponses,
  bulkDeleteMessages,
  deleteMessage,
  deleteMessageResponse,
  exportFailedContacts,
  retryMessage,
  searchDailyProspects,
  searchMessageResponses,
  searchMessages,
  type DailyProspectDto,
  type IntentClassification,
  type MessageDto,
  type MessageResponseDto,
  type MessageStatus,
} from '../../api/messages';
import {
  deleteRecipient,
  processCampaignQueue,
  retryRecipients,
  searchCampaignRecipients,
  type CampaignRecipientDto,
  type CampaignRecipientStatus,
} from '../../api/campaigns';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { LiveRefreshLabel } from '../../components/LiveRefreshLabel';

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
  AutomatedReply: 'Respuesta automática',
};

const classificationOptions: IntentClassification[] = [
  'Interested',
  'NotInterested',
  'Question',
  'Unclear',
  'Stop',
  'AutomatedReply',
];

const classificationBadgeClass: Record<IntentClassification, string> = {
  Interested: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
  Question: 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300',
  NotInterested: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Stop: 'bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-300',
  Unclear: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
  AutomatedReply: 'bg-sky-50 text-sky-700 dark:bg-sky-500/10 dark:text-sky-300',
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
    refetchInterval: 15000,
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
      <div className="flex items-center gap-2">
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
        <button
          type="button"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
        >
          {query.isFetching ? 'Actualizando…' : 'Actualizar'}
        </button>
        <LiveRefreshLabel intervalSeconds={15} lastUpdatedAt={query.dataUpdatedAt} />
      </div>

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

const recipientStatusLabels: Record<CampaignRecipientStatus, string> = {
  Pending: 'Pendiente',
  Queued: 'En cola',
  Sent: 'Enviado',
  Delivered: 'Entregado',
  Responded: 'Respondió',
  Interested: 'Interesado',
  NotInterested: 'No interesado',
  Stopped: 'Excluido (opt-out)',
  Skipped: 'Sin contacto',
  Failed: 'Falló',
  Completed: 'Completado',
};

// Solo los estados que significan "no se envió (todavía)": Sent/Delivered/etc. ya tienen su
// lugar en la pestaña "Enviados". Stopped (opt-out) y Skipped (sin contacto) no tienen botón de
// reintentar acá porque no es algo que un reintento resuelva por sí solo (ver CampaignService.RetryRecipientsAsync).
const notSentStatusOptions: CampaignRecipientStatus[] = ['Failed', 'Pending', 'Queued', 'Skipped', 'Stopped'];

type PendingRecipientDeletion =
  | { kind: 'single'; recipient: CampaignRecipientDto }
  | { kind: 'bulk'; recipients: CampaignRecipientDto[] };

type RetryTarget = { id: number; campaignId: number | null; isCampaignRecipient: boolean };

// Reintentar un CampaignRecipient real reprocesa la cola de su Campaña; reintentar un envío
// individual (isCampaignRecipient=false) reenvía ese Message puntual vía TestMessageService.
async function retryOne(r: RetryTarget): Promise<void> {
  if (r.isCampaignRecipient) {
    await retryRecipients([r.id]);
    if (r.campaignId !== null) {
      for (;;) {
        const batch = await processCampaignQueue(r.campaignId);
        if (batch.processed === 0) break;
      }
    }
  } else {
    await retryMessage(r.id);
  }
}

async function deleteOne(r: CampaignRecipientDto): Promise<void> {
  if (r.isCampaignRecipient) {
    await deleteRecipient(r.id);
  } else {
    await deleteMessage(r.id);
  }
}

function NotSentTab() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<CampaignRecipientStatus>('Failed');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [pendingDeletion, setPendingDeletion] = useState<PendingRecipientDeletion | null>(null);

  const query = useQuery({
    queryKey: ['campaign-recipients', { status, page }],
    queryFn: () => searchCampaignRecipients({ status, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
    refetchInterval: 15000,
  });

  function changePage(next: number) {
    setPage(next);
    setSelectedIds(new Set());
  }

  const retryMutation = useMutation({
    mutationFn: async (targets: CampaignRecipientDto[]) => {
      for (const target of targets.filter((r) => r.status === 'Failed')) {
        await retryOne(target);
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      queryClient.invalidateQueries({ queryKey: ['campaign-recipients'] });
      queryClient.invalidateQueries({ queryKey: ['messages'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (target: PendingRecipientDeletion) => {
      const targets = target.kind === 'single' ? [target.recipient] : target.recipients;
      for (const r of targets) {
        await deleteOne(r);
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      setPendingDeletion(null);
      queryClient.invalidateQueries({ queryKey: ['campaign-recipients'] });
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
      const allSelected = query.data.items.length > 0 && query.data.items.every((r) => prev.has(r.id));
      return allSelected ? new Set() : new Set(query.data.items.map((r) => r.id));
    });
  }

  const allOnPageSelected =
    !!query.data && query.data.items.length > 0 && query.data.items.every((r) => selectedIds.has(r.id));
  const selectedRecipients = query.data?.items.filter((r) => selectedIds.has(r.id)) ?? [];
  const selectedRetryableCount = selectedRecipients.filter((r) => r.status === 'Failed').length;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as CampaignRecipientStatus);
            changePage(1);
          }}
          className={selectClass}
        >
          {notSentStatusOptions.map((s) => (
            <option key={s} value={s}>
              {recipientStatusLabels[s]}
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
        >
          {query.isFetching ? 'Actualizando…' : 'Actualizar'}
        </button>
        <LiveRefreshLabel intervalSeconds={15} lastUpdatedAt={query.dataUpdatedAt} />
      </div>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar los destinatarios.'}
        </p>
      )}
      {retryMutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {retryMutation.error instanceof Error ? retryMutation.error.message : 'No se pudo reintentar el envío.'}
        </p>
      )}
      {deleteMutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {deleteMutation.error instanceof Error ? deleteMutation.error.message : 'No se pudo borrar.'}
        </p>
      )}

      {query.data && (
        <>
          {selectedIds.size > 0 && (
            <div className="flex items-center justify-between rounded-md border border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/10 px-4 py-2 text-sm">
              <span className="text-indigo-700 dark:text-indigo-300">{selectedIds.size} seleccionado(s)</span>
              <div className="flex items-center gap-2">
                {selectedRetryableCount > 0 && (
                  <button
                    type="button"
                    onClick={() => retryMutation.mutate(selectedRecipients)}
                    disabled={retryMutation.isPending}
                    className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
                  >
                    {retryMutation.isPending ? 'Reintentando…' : `Reintentar seleccionados (${selectedRetryableCount})`}
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => setPendingDeletion({ kind: 'bulk', recipients: selectedRecipients })}
                  className="rounded-md bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-500"
                >
                  Eliminar seleccionados
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
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Destinatario</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Campaña</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Intentos</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Último intento</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((r) => (
                  <tr key={r.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(r.id)}
                        onChange={() => toggleSelected(r.id)}
                        aria-label={`Seleccionar destinatario ${r.prospectBusinessName}`}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    </td>
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${r.prospectId}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {r.prospectBusinessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {r.campaignName ?? <span title="Envío individual, no pertenece a una campaña">Envío individual</span>}
                    </td>
                    <td className="px-4 py-2">
                      <span className="text-slate-600 dark:text-slate-300">{recipientStatusLabels[r.status]}</span>
                      {r.status === 'Failed' && r.lastMessageFailureReason && (
                        <p className="text-xs text-red-500 dark:text-red-400" title={r.lastMessageFailureReason}>
                          {r.lastMessageFailureReason}
                        </p>
                      )}
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{r.attempts}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {r.lastAttemptAt ? new Date(r.lastAttemptAt).toLocaleString('es-AR') : '—'}
                    </td>
                    <td className="px-4 py-2">
                      <div className="flex items-center gap-3">
                        {r.status === 'Failed' && (
                          <button
                            type="button"
                            onClick={() => retryMutation.mutate([r])}
                            disabled={retryMutation.isPending}
                            className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-60"
                          >
                            Reintentar
                          </button>
                        )}
                        <button
                          type="button"
                          onClick={() => setPendingDeletion({ kind: 'single', recipient: r })}
                          disabled={deleteMutation.isPending}
                          className="text-xs font-medium text-red-600 dark:text-red-400 hover:underline disabled:opacity-60"
                        >
                          Eliminar
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-slate-400">
                      Sin destinatarios en este estado.
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
          title={pendingDeletion.kind === 'single' ? 'Eliminar destinatario' : 'Eliminar seleccionados'}
          message={
            pendingDeletion.kind === 'single'
              ? `¿Eliminar a "${pendingDeletion.recipient.prospectBusinessName}" de esta lista? Esta acción no se puede deshacer.`
              : `¿Eliminar ${pendingDeletion.recipients.length} destinatario(s) seleccionado(s)? Esta acción no se puede deshacer.`
          }
          confirmLabel="Eliminar"
          danger
          isPending={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(pendingDeletion)}
          onCancel={() => setPendingDeletion(null)}
        />
      )}
    </div>
  );
}

const dailyInputClass =
  'rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100 placeholder:text-slate-400';

function DailyProspectsTab() {
  const queryClient = useQueryClient();
  const [province, setProvince] = useState('');
  const [city, setCity] = useState('');
  const [sentFilter, setSentFilter] = useState<'' | 'true' | 'false'>('');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());

  const query = useQuery({
    queryKey: ['daily-prospects', { province, city, sentFilter, page }],
    queryFn: () =>
      searchDailyProspects({
        province: province.trim() || undefined,
        city: city.trim() || undefined,
        sent: sentFilter === '' ? undefined : sentFilter === 'true',
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
    refetchInterval: 15000,
  });

  const exportMutation = useMutation({
    mutationFn: () => exportFailedContacts({ province: province.trim() || undefined, city: city.trim() || undefined }),
  });

  const retryMutation = useMutation({
    mutationFn: async (targets: DailyProspectDto[]) => {
      for (const target of targets.filter((p) => p.lastStatus === 'Failed' && p.retryTargetId !== null)) {
        await retryOne({ id: target.retryTargetId!, campaignId: target.campaignId, isCampaignRecipient: target.isCampaignRecipient });
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      queryClient.invalidateQueries({ queryKey: ['daily-prospects'] });
      queryClient.invalidateQueries({ queryKey: ['messages'] });
      queryClient.invalidateQueries({ queryKey: ['campaign-recipients'] });
    },
  });

  function changePage(next: number) {
    setPage(next);
    setSelectedIds(new Set());
  }

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
      const allSelected = query.data.items.length > 0 && query.data.items.every((p) => prev.has(p.prospectId));
      return allSelected ? new Set() : new Set(query.data.items.map((p) => p.prospectId));
    });
  }

  const allOnPageSelected =
    !!query.data && query.data.items.length > 0 && query.data.items.every((p) => selectedIds.has(p.prospectId));
  const selectedProspects = query.data?.items.filter((p) => selectedIds.has(p.prospectId)) ?? [];
  const selectedRetryableCount = selectedProspects.filter((p) => p.lastStatus === 'Failed' && p.retryTargetId !== null).length;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <input
          type="text"
          value={province}
          onChange={(e) => {
            setProvince(e.target.value);
            changePage(1);
          }}
          placeholder="Provincia"
          className={dailyInputClass}
        />
        <input
          type="text"
          value={city}
          onChange={(e) => {
            setCity(e.target.value);
            changePage(1);
          }}
          placeholder="Localidad"
          className={dailyInputClass}
        />
        <select
          value={sentFilter}
          onChange={(e) => {
            setSentFilter(e.target.value as '' | 'true' | 'false');
            changePage(1);
          }}
          className={selectClass}
        >
          <option value="">Todos</option>
          <option value="true">Ya se envió</option>
          <option value="false">Todavía no</option>
        </select>
        <button
          type="button"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
        >
          {query.isFetching ? 'Actualizando…' : 'Actualizar'}
        </button>
        <button
          type="button"
          onClick={() => exportMutation.mutate()}
          disabled={exportMutation.isPending}
          className="ml-auto rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          {exportMutation.isPending ? 'Exportando…' : 'Exportar no contactados'}
        </button>
        <LiveRefreshLabel intervalSeconds={15} lastUpdatedAt={query.dataUpdatedAt} />
      </div>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar los prospectos del día.'}
        </p>
      )}
      {exportMutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {exportMutation.error instanceof Error ? exportMutation.error.message : 'No se pudo exportar la lista.'}
        </p>
      )}
      {retryMutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {retryMutation.error instanceof Error ? retryMutation.error.message : 'No se pudo reintentar el envío.'}
        </p>
      )}

      {query.data && (
        <>
          {selectedIds.size > 0 && (
            <div className="flex items-center justify-between rounded-md border border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/10 px-4 py-2 text-sm">
              <span className="text-indigo-700 dark:text-indigo-300">{selectedIds.size} seleccionado(s)</span>
              {selectedRetryableCount > 0 && (
                <button
                  type="button"
                  onClick={() => retryMutation.mutate(selectedProspects)}
                  disabled={retryMutation.isPending}
                  className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
                >
                  {retryMutation.isPending ? 'Reintentando…' : `Reintentar seleccionados (${selectedRetryableCount})`}
                </button>
              )}
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
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Prospecto</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Provincia</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Localidad</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Enviado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Intentos</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Último intento</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((p) => (
                  <tr key={p.prospectId} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(p.prospectId)}
                        onChange={() => toggleSelected(p.prospectId)}
                        aria-label={`Seleccionar prospecto ${p.businessName}`}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    </td>
                    <td className="px-4 py-2">
                      <Link
                        to={`/app/prospects/${p.prospectId}`}
                        className="font-medium text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        {p.businessName}
                      </Link>
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.province ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.city ?? '—'}</td>
                    <td className="px-4 py-2">
                      {p.sent ? (
                        <span className="rounded-full bg-emerald-50 dark:bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:text-emerald-300">
                          Enviado
                        </span>
                      ) : (
                        <span className="rounded-full bg-slate-100 dark:bg-slate-800 px-2 py-0.5 text-xs font-medium text-slate-600 dark:text-slate-300">
                          Pendiente
                        </span>
                      )}
                      {p.lastStatus === 'Failed' && (
                        <span className="ml-1 rounded-full bg-red-50 dark:bg-red-500/10 px-2 py-0.5 text-xs font-medium text-red-600 dark:text-red-400">
                          Falló
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{p.attempts}</td>
                    <td className="px-4 py-2 text-slate-600 dark:text-slate-300">
                      {p.lastAttemptAt ? new Date(p.lastAttemptAt).toLocaleString('es-AR') : '—'}
                    </td>
                    <td className="px-4 py-2">
                      {p.lastStatus === 'Failed' && p.retryTargetId !== null && (
                        <button
                          type="button"
                          onClick={() => retryMutation.mutate([p])}
                          disabled={retryMutation.isPending}
                          className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-60"
                        >
                          Reintentar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-4 py-6 text-center text-slate-400">
                      Sin prospectos nuevos hoy con estos filtros.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pager page={query.data.page} totalPages={query.data.totalPages} onChange={changePage} />
        </>
      )}
    </div>
  );
}

type PendingResponseDeletion =
  | { kind: 'single'; response: MessageResponseDto }
  | { kind: 'bulk'; responses: MessageResponseDto[] };

function ResponsesTab() {
  const queryClient = useQueryClient();
  const [classification, setClassification] = useState<IntentClassification | ''>('');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [pendingDeletion, setPendingDeletion] = useState<PendingResponseDeletion | null>(null);

  const query = useQuery({
    queryKey: ['message-responses', { classification, page }],
    queryFn: () => searchMessageResponses({ classification: classification || undefined, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
    refetchInterval: 15000,
  });

  function changePage(next: number) {
    setPage(next);
    setSelectedIds(new Set());
  }

  const deleteMutation = useMutation({
    mutationFn: async (target: PendingResponseDeletion) => {
      if (target.kind === 'single') {
        await deleteMessageResponse(target.response.id);
      } else {
        await bulkDeleteMessageResponses(target.responses.map((r) => r.id));
      }
    },
    onSuccess: () => {
      setSelectedIds(new Set());
      setPendingDeletion(null);
      queryClient.invalidateQueries({ queryKey: ['message-responses'] });
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
      const allSelected = query.data.items.length > 0 && query.data.items.every((r) => prev.has(r.id));
      return allSelected ? new Set() : new Set(query.data.items.map((r) => r.id));
    });
  }

  const allOnPageSelected =
    !!query.data && query.data.items.length > 0 && query.data.items.every((r) => selectedIds.has(r.id));
  const selectedResponses = query.data?.items.filter((r) => selectedIds.has(r.id)) ?? [];

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <select
          value={classification}
          onChange={(e) => {
            setClassification(e.target.value as IntentClassification | '');
            changePage(1);
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
        <button
          type="button"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
        >
          {query.isFetching ? 'Actualizando…' : 'Actualizar'}
        </button>
        <LiveRefreshLabel intervalSeconds={15} lastUpdatedAt={query.dataUpdatedAt} />
      </div>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar las respuestas.'}
        </p>
      )}

      {query.data && (
        <>
          {selectedIds.size > 0 && (
            <div className="flex items-center justify-between rounded-md border border-indigo-200 dark:border-indigo-800 bg-indigo-50 dark:bg-indigo-500/10 px-4 py-2 text-sm">
              <span className="text-indigo-700 dark:text-indigo-300">{selectedIds.size} seleccionado(s)</span>
              <button
                type="button"
                onClick={() => setPendingDeletion({ kind: 'bulk', responses: selectedResponses })}
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
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Prospecto</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Respuesta</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Clasificación</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Origen</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Recibido</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((r) => (
                  <tr key={r.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(r.id)}
                        onChange={() => toggleSelected(r.id)}
                        aria-label={`Seleccionar respuesta de ${r.prospectBusinessName}`}
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    </td>
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
                    <td className="px-4 py-2">
                      <button
                        type="button"
                        onClick={() => setPendingDeletion({ kind: 'single', response: r })}
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
                      Sin respuestas todavía.
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
          title={pendingDeletion.kind === 'single' ? 'Borrar respuesta' : 'Borrar respuestas seleccionadas'}
          message={
            pendingDeletion.kind === 'single'
              ? `¿Borrar la respuesta de "${pendingDeletion.response.prospectBusinessName}"? Esta acción no se puede deshacer.`
              : `¿Borrar ${pendingDeletion.responses.length} respuesta(s) seleccionada(s)? Esta acción no se puede deshacer.`
          }
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

export function MessagesPage() {
  const [tab, setTab] = useState<'sent' | 'responses' | 'not-sent' | 'daily'>('sent');

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
          A quién se le envió cada mensaje y su estado, quiénes todavía no recibieron el suyo (con opción de
          reintentar), las respuestas recibidas (texto libre o botón "Estoy interesado") con la clasificación que
          les asignó el sistema, y los prospectos que se sumaron hoy con su estado de contacto.
        </p>
      </div>

      <div className="flex gap-2">
        <button className={tabClass(tab === 'sent')} onClick={() => setTab('sent')}>
          Enviados
        </button>
        <button className={tabClass(tab === 'not-sent')} onClick={() => setTab('not-sent')}>
          No enviados
        </button>
        <button className={tabClass(tab === 'responses')} onClick={() => setTab('responses')}>
          Respuestas
        </button>
        <button className={tabClass(tab === 'daily')} onClick={() => setTab('daily')}>
          Del día
        </button>
      </div>

      {tab === 'sent' && <SentMessagesTab />}
      {tab === 'not-sent' && <NotSentTab />}
      {tab === 'responses' && <ResponsesTab />}
      {tab === 'daily' && <DailyProspectsTab />}
    </div>
  );
}
