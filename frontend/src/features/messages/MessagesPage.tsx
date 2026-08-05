import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import {
  searchMessageResponses,
  searchMessages,
  type IntentClassification,
  type MessageStatus,
} from '../../api/messages';

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

function SentMessagesTab() {
  const [status, setStatus] = useState<MessageStatus | ''>('');
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ['messages', { status, page }],
    queryFn: () => searchMessages({ status: status || undefined, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  });

  return (
    <div className="space-y-3">
      <select
        value={status}
        onChange={(e) => {
          setStatus(e.target.value as MessageStatus | '');
          setPage(1);
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
          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Destinatario</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Canal</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Mensaje</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Enviado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {query.data.items.map((m) => (
                  <tr key={m.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
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
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                      Sin mensajes.
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
