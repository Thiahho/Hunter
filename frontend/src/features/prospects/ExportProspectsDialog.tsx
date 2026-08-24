import { useMemo, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { exportProspectsToExcel } from '../../api/prospects';
import { listTemplates } from '../../api/templates';

export function ExportProspectsDialog({ prospectIds, onClose }: { prospectIds: number[]; onClose: () => void }) {
  const [selectedTemplateIds, setSelectedTemplateIds] = useState<Set<number>>(new Set());

  const templatesQuery = useQuery({ queryKey: ['templates'], queryFn: listTemplates });

  const availableTemplates = useMemo(
    () => (templatesQuery.data ?? []).filter((t) => t.channel === 'Whatsapp' && t.isActive),
    [templatesQuery.data],
  );

  function toggleTemplate(id: number) {
    setSelectedTemplateIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  const exportMutation = useMutation({
    mutationFn: () => exportProspectsToExcel(prospectIds, [...selectedTemplateIds]),
    onSuccess: onClose,
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-md rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-lg"
      >
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Exportar a Excel</h3>
        <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
          Se exportarán {prospectIds.length} prospecto(s) seleccionado(s), con un link de WhatsApp
          pre-cargado por cada plantilla que elijas.
        </p>

        <div className="mt-4 space-y-2">
          <label className="block text-xs font-medium text-slate-500 dark:text-slate-400">
            Plantillas de WhatsApp a incluir (opcional)
          </label>
          {templatesQuery.isLoading && <p className="text-xs text-slate-400">Cargando plantillas...</p>}
          {templatesQuery.data && availableTemplates.length === 0 && (
            <p className="text-xs text-amber-600 dark:text-amber-400">
              No hay plantillas activas de WhatsApp. Podés exportar igual, solo con los datos del prospecto.
            </p>
          )}
          <div className="max-h-48 space-y-1 overflow-y-auto rounded-md border border-slate-200 dark:border-slate-800 p-2">
            {availableTemplates.map((t) => (
              <label key={t.id} className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
                <input
                  type="checkbox"
                  checked={selectedTemplateIds.has(t.id)}
                  onChange={() => toggleTemplate(t.id)}
                  className="rounded border-slate-300 dark:border-slate-700"
                />
                {t.name}
              </label>
            ))}
          </div>
        </div>

        {exportMutation.isError && (
          <p className="mt-3 text-sm text-red-600 dark:text-red-400">
            {exportMutation.error instanceof Error ? exportMutation.error.message : 'No se pudo exportar.'}
          </p>
        )}

        <div className="mt-5 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            disabled={exportMutation.isPending}
            className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={() => exportMutation.mutate()}
            disabled={exportMutation.isPending}
            className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {exportMutation.isPending ? 'Exportando…' : `Exportar ${prospectIds.length}`}
          </button>
        </div>
      </div>
    </div>
  );
}
