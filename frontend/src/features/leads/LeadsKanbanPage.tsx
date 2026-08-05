import { useState, type FormEvent, type ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import {
  markLeadLost,
  markLeadWon,
  searchLeads,
  setLeadInProgress,
  type Lead,
  type LeadListItem,
  type LeadStatus,
  type LostReason,
} from '../../api/leads';

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

// Transiciones que el backend realmente soporta (LeadService.cs): New/InProgress -> InProgress
// sin datos extra, y New/InProgress -> Won/Lost pidiendo monto o motivo. No hay forma de
// "reabrir" un lead Won/Lost, así que esas tarjetas no son arrastrables y esas columnas nunca
// son destino válido.
function isValidTransition(from: LeadStatus, to: LeadStatus): boolean {
  if (from === to) return false;
  if (from !== 'New' && from !== 'InProgress') return false;
  return to === 'InProgress' || to === 'Won' || to === 'Lost';
}

function LeadCard({
  lead,
  draggable,
  isDragging,
  onDragStart,
  onDragEnd,
  onMoveTo,
}: {
  lead: LeadListItem;
  draggable: boolean;
  isDragging: boolean;
  onDragStart: () => void;
  onDragEnd: () => void;
  onMoveTo: (status: LeadStatus) => void;
}) {
  // El drag & drop nativo de HTML5 no dispara con touch en celular, así que el selector "Mover
  // a…" es la forma real de cambiar de columna en mobile (y una alternativa más rápida en
  // desktop para quien no quiera arrastrar). Vive fuera del <Link> a propósito: un <select>
  // adentro de un <a> no es válido HTML y el navegador puede confundir el click.
  const moveOptions = columns.filter((c) => isValidTransition(lead.status, c.status));

  return (
    <div
      draggable={draggable}
      onDragStart={(e) => {
        if (!draggable) {
          e.preventDefault();
          return;
        }
        e.dataTransfer.effectAllowed = 'move';
        onDragStart();
      }}
      onDragEnd={onDragEnd}
      className={`rounded-md border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 hover:border-indigo-300 dark:hover:border-indigo-700 ${
        draggable ? 'cursor-grab active:cursor-grabbing' : ''
      } ${isDragging ? 'opacity-40' : ''}`}
    >
      <Link to={`/app/leads/${lead.id}`} className="block">
        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{lead.prospectBusinessName}</p>
        <span className={`mt-1 inline-block rounded px-1.5 py-0.5 text-xs ${priorityColors[lead.priority]}`}>
          {lead.priority}
        </span>
      </Link>
      {moveOptions.length > 0 && (
        <select
          value=""
          onChange={(e) => {
            const status = e.target.value as LeadStatus;
            if (status) onMoveTo(status);
            e.target.value = '';
          }}
          aria-label={`Mover "${lead.prospectBusinessName}" a otra columna`}
          className="mt-2 w-full rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-xs text-slate-600 dark:text-slate-300"
        >
          <option value="">Mover a…</option>
          {moveOptions.map((c) => (
            <option key={c.status} value={c.status}>
              {c.label}
            </option>
          ))}
        </select>
      )}
    </div>
  );
}

function DropPromptModal({ title, onCancel, children }: { title: string; onCancel: () => void; children: ReactNode }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4" onClick={onCancel}>
      <div
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-sm rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-lg"
      >
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{title}</h3>
        {children}
      </div>
    </div>
  );
}

export function LeadsKanbanPage() {
  const queryClient = useQueryClient();
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['leads', 'kanban'],
    queryFn: () => searchLeads({ pageSize: 200 }),
  });

  const [draggedLead, setDraggedLead] = useState<LeadListItem | null>(null);
  const [wonTarget, setWonTarget] = useState<LeadListItem | null>(null);
  const [lostTarget, setLostTarget] = useState<LeadListItem | null>(null);
  const [wonAmount, setWonAmount] = useState('');
  const [lostReason, setLostReason] = useState<LostReason>('Price');

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['leads'] });
  }

  const inProgressMutation = useMutation({
    mutationFn: (id: number) => setLeadInProgress(id),
    onSuccess: invalidate,
  });

  const wonMutation = useMutation({
    mutationFn: () => markLeadWon(wonTarget!.id, { amount: Number(wonAmount), currency: 'ARS' }),
    onSuccess: () => {
      setWonTarget(null);
      setWonAmount('');
      invalidate();
    },
  });

  const lostMutation = useMutation({
    mutationFn: () => markLeadLost(lostTarget!.id, { lostReason }),
    onSuccess: () => {
      setLostTarget(null);
      invalidate();
    },
  });

  function handleTargetChosen(lead: LeadListItem, targetStatus: LeadStatus) {
    if (!isValidTransition(lead.status, targetStatus)) return;

    if (targetStatus === 'InProgress') {
      inProgressMutation.mutate(lead.id);
    } else if (targetStatus === 'Won') {
      setWonTarget(lead);
    } else if (targetStatus === 'Lost') {
      setLostTarget(lead);
    }
  }

  function handleDrop(targetStatus: LeadStatus) {
    const lead = draggedLead;
    setDraggedLead(null);
    if (lead) handleTargetChosen(lead, targetStatus);
  }

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
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Leads</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Arrastrá una tarjeta a otra columna para cambiar su estado, o usá el selector "Mover a…" de cada tarjeta
          (necesario en mobile, donde no hay drag & drop). Ganado y Perdido son finales: no se pueden reabrir desde
          acá.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {columns.map((col) => {
          const items = data.items.filter((l) => l.status === col.status);
          const canDropHere = !!draggedLead && isValidTransition(draggedLead.status, col.status);

          return (
            <div
              key={col.status}
              onDragOver={(e) => {
                if (canDropHere) e.preventDefault();
              }}
              onDrop={(e) => {
                e.preventDefault();
                handleDrop(col.status);
              }}
              className={`rounded-lg bg-slate-100 dark:bg-slate-900/50 p-3 transition-colors ${
                canDropHere ? 'ring-2 ring-indigo-400 dark:ring-indigo-600' : ''
              }`}
            >
              <h3 className="mb-3 flex items-center justify-between text-sm font-medium text-slate-600 dark:text-slate-300">
                {col.label}
                <span className="rounded-full bg-slate-200 dark:bg-slate-800 px-2 py-0.5 text-xs">{items.length}</span>
              </h3>
              <div className="space-y-2">
                {items.map((lead) => (
                  <LeadCard
                    key={lead.id}
                    lead={lead}
                    draggable={lead.status === 'New' || lead.status === 'InProgress'}
                    isDragging={draggedLead?.id === lead.id}
                    onDragStart={() => setDraggedLead(lead)}
                    onDragEnd={() => setDraggedLead(null)}
                    onMoveTo={(status) => handleTargetChosen(lead, status)}
                  />
                ))}
                {items.length === 0 && <p className="text-xs text-slate-400">Sin leads.</p>}
              </div>
            </div>
          );
        })}
      </div>

      {wonTarget && (
        <DropPromptModal title={`Marcar "${wonTarget.prospectBusinessName}" como ganado`} onCancel={() => setWonTarget(null)}>
          <form
            onSubmit={(e: FormEvent) => {
              e.preventDefault();
              wonMutation.mutate();
            }}
            className="mt-3 space-y-3"
          >
            <div>
              <label className="block text-xs text-slate-500 dark:text-slate-400">Monto (ARS)</label>
              <input
                type="number"
                required
                min="0"
                step="0.01"
                autoFocus
                value={wonAmount}
                onChange={(e) => setWonAmount(e.target.value)}
                className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 px-2 py-1.5 text-sm text-slate-900 dark:text-slate-100"
              />
            </div>
            {wonMutation.isError && (
              <p className="text-xs text-red-600 dark:text-red-400">
                {wonMutation.error instanceof Error ? wonMutation.error.message : 'Error al confirmar.'}
              </p>
            )}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setWonTarget(null)}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={wonMutation.isPending}
                className="rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-500 disabled:opacity-60"
              >
                {wonMutation.isPending ? 'Confirmando…' : 'Confirmar'}
              </button>
            </div>
          </form>
        </DropPromptModal>
      )}

      {lostTarget && (
        <DropPromptModal title={`Marcar "${lostTarget.prospectBusinessName}" como perdido`} onCancel={() => setLostTarget(null)}>
          <form
            onSubmit={(e: FormEvent) => {
              e.preventDefault();
              lostMutation.mutate();
            }}
            className="mt-3 space-y-3"
          >
            <div>
              <label className="block text-xs text-slate-500 dark:text-slate-400">Motivo</label>
              <select
                autoFocus
                value={lostReason}
                onChange={(e) => setLostReason(e.target.value as LostReason)}
                className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 px-2 py-1.5 text-sm text-slate-900 dark:text-slate-100"
              >
                {lostReasons.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>
            {lostMutation.isError && (
              <p className="text-xs text-red-600 dark:text-red-400">
                {lostMutation.error instanceof Error ? lostMutation.error.message : 'Error al confirmar.'}
              </p>
            )}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setLostTarget(null)}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={lostMutation.isPending}
                className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-500 disabled:opacity-60"
              >
                {lostMutation.isPending ? 'Confirmando…' : 'Confirmar'}
              </button>
            </div>
          </form>
        </DropPromptModal>
      )}
    </div>
  );
}
