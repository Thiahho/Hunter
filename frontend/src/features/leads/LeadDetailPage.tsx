import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import {
  fetchLeadById,
  markLeadLost,
  markLeadWon,
  setLeadInProgress,
  type LostReason,
} from '../../api/leads';
import { fetchProspectById } from '../../api/prospects';

const lostReasons: LostReason[] = [
  'Price',
  'NoStock',
  'ExistingSupplier',
  'NoResponse',
  'NotInterested',
  'Logistics',
  'CommercialConditions',
  'Other',
];

function buildWhatsAppLink(phone: string): string {
  const digits = phone.replace(/[^\d]/g, '');
  return `https://wa.me/${digits}`;
}

export function LeadDetailPage() {
  const { id } = useParams<{ id: string }>();
  const leadId = Number(id);
  const queryClient = useQueryClient();

  const [showWonForm, setShowWonForm] = useState(false);
  const [showLostForm, setShowLostForm] = useState(false);
  const [wonAmount, setWonAmount] = useState('');
  const [lostReason, setLostReason] = useState<LostReason>('Price');

  const leadQuery = useQuery({
    queryKey: ['leads', leadId],
    queryFn: () => fetchLeadById(leadId),
    enabled: Number.isFinite(leadId),
  });

  const prospectQuery = useQuery({
    queryKey: ['prospects', leadQuery.data?.prospectId],
    queryFn: () => fetchProspectById(leadQuery.data!.prospectId),
    enabled: !!leadQuery.data,
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['leads'] });
  }

  const inProgressMutation = useMutation({
    mutationFn: () => setLeadInProgress(leadId),
    onSuccess: invalidate,
  });

  const wonMutation = useMutation({
    mutationFn: () => markLeadWon(leadId, { amount: Number(wonAmount), currency: 'ARS' }),
    onSuccess: () => {
      setShowWonForm(false);
      invalidate();
    },
  });

  const lostMutation = useMutation({
    mutationFn: () => markLeadLost(leadId, { lostReason }),
    onSuccess: () => {
      setShowLostForm(false);
      invalidate();
    },
  });

  if (leadQuery.isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando lead...</p>;
  }

  if (leadQuery.isError || !leadQuery.data) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {leadQuery.error instanceof Error ? leadQuery.error.message : 'No se pudo cargar el lead.'}
      </p>
    );
  }

  const lead = leadQuery.data;
  const whatsappContact = prospectQuery.data?.contacts.find(
    (c) => c.channel === 'Whatsapp' || c.channel === 'Phone',
  );

  return (
    <div className="space-y-6">
      <div>
        <Link to="/app/leads" className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline">
          ← Volver a leads
        </Link>
        <h2 className="mt-2 text-lg font-semibold text-slate-900 dark:text-slate-100">{lead.prospectBusinessName}</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {lead.status} · Prioridad {lead.priority}
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {whatsappContact && (
          <a
            href={buildWhatsAppLink(whatsappContact.value)}
            target="_blank"
            rel="noreferrer"
            className="rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
          >
            Abrir WhatsApp
          </a>
        )}
        {lead.status === 'New' && (
          <button
            onClick={() => inProgressMutation.mutate()}
            disabled={inProgressMutation.isPending}
            className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            Marcar en progreso
          </button>
        )}
        {(lead.status === 'New' || lead.status === 'InProgress') && (
          <>
            <button
              onClick={() => setShowWonForm((v) => !v)}
              className="rounded-md border border-emerald-300 dark:border-emerald-700 px-3 py-1.5 text-sm font-medium text-emerald-700 dark:text-emerald-300 hover:bg-emerald-50 dark:hover:bg-emerald-500/10"
            >
              Marcar ganado
            </button>
            <button
              onClick={() => setShowLostForm((v) => !v)}
              className="rounded-md border border-red-300 dark:border-red-700 px-3 py-1.5 text-sm font-medium text-red-700 dark:text-red-300 hover:bg-red-50 dark:hover:bg-red-500/10"
            >
              Marcar perdido
            </button>
          </>
        )}
      </div>

      {showWonForm && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            wonMutation.mutate();
          }}
          className="flex items-end gap-2 rounded-md border border-slate-200 dark:border-slate-800 p-3"
        >
          <div>
            <label className="block text-xs text-slate-500 dark:text-slate-400">Monto (ARS)</label>
            <input
              type="number"
              required
              min="0"
              step="0.01"
              value={wonAmount}
              onChange={(e) => setWonAmount(e.target.value)}
              className="mt-1 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-2 py-1 text-sm"
            />
          </div>
          <button
            type="submit"
            disabled={wonMutation.isPending}
            className="rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
          >
            Confirmar
          </button>
          {wonMutation.isError && (
            <p className="text-sm text-red-600 dark:text-red-400">
              {wonMutation.error instanceof Error ? wonMutation.error.message : 'Error al confirmar.'}
            </p>
          )}
        </form>
      )}

      {showLostForm && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            lostMutation.mutate();
          }}
          className="flex items-end gap-2 rounded-md border border-slate-200 dark:border-slate-800 p-3"
        >
          <div>
            <label className="block text-xs text-slate-500 dark:text-slate-400">Motivo</label>
            <select
              value={lostReason}
              onChange={(e) => setLostReason(e.target.value as LostReason)}
              className="mt-1 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-2 py-1 text-sm"
            >
              {lostReasons.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          </div>
          <button
            type="submit"
            disabled={lostMutation.isPending}
            className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-700"
          >
            Confirmar
          </button>
          {lostMutation.isError && (
            <p className="text-sm text-red-600 dark:text-red-400">
              {lostMutation.error instanceof Error ? lostMutation.error.message : 'Error al confirmar.'}
            </p>
          )}
        </form>
      )}

      <div className="grid gap-6 sm:grid-cols-2">
        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Actividades</h3>
          {lead.activities.length === 0 && <p className="text-sm text-slate-400">Sin actividades registradas.</p>}
          <ul className="space-y-2">
            {lead.activities.map((a) => (
              <li key={a.id} className="text-sm">
                <span className="font-medium text-slate-700 dark:text-slate-200">{a.type}</span>
                <span className="text-slate-500 dark:text-slate-400"> — {a.description}</span>
                <p className="text-xs text-slate-400">{new Date(a.createdAt).toLocaleString('es-AR')}</p>
              </li>
            ))}
          </ul>
        </section>

        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Seguimientos</h3>
          {lead.followUps.length === 0 && <p className="text-sm text-slate-400">Sin seguimientos programados.</p>}
          <ul className="space-y-2">
            {lead.followUps.map((f) => (
              <li key={f.id} className="text-sm">
                <span className="font-medium text-slate-700 dark:text-slate-200">
                  {new Date(f.scheduledAt).toLocaleString('es-AR')}
                </span>
                <span className="text-slate-500 dark:text-slate-400"> — {f.status}</span>
                {f.notes && <p className="text-xs text-slate-400">{f.notes}</p>}
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
}
