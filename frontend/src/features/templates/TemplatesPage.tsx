import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listMetaTemplates,
  listTemplates,
  setTemplateActive,
  setTemplateCatalog,
  syncTemplateFromMeta,
  type MessageTemplateDto,
  type MetaWhatsAppTemplateDto,
} from '../../api/templates';
import { ConfirmDialog } from '../../components/ConfirmDialog';

const metaStatusBadgeClass: Record<string, string> = {
  APPROVED: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
  PENDING: 'bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300',
  REJECTED: 'bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-300',
};

function metaStatusClass(status: string): string {
  return metaStatusBadgeClass[status] ?? 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300';
}

function LocalTemplatesTable() {
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: ['templates'], queryFn: listTemplates });

  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) => setTemplateActive(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['templates'] }),
  });

  const catalogMutation = useMutation({
    mutationFn: (id: number) => setTemplateCatalog(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['templates'] }),
  });

  if (query.isLoading) return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>;
  if (query.isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {query.error instanceof Error ? query.error.message : 'No se pudieron cargar las plantillas.'}
      </p>
    );
  }

  const templates = query.data ?? [];

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
      <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
        <thead className="bg-slate-50 dark:bg-slate-900">
          <tr>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Nombre</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Canal</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Versión</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Contenido</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Catálogo</th>
            <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
          {templates.map((t: MessageTemplateDto) => (
            <tr key={t.id} className="hover:bg-slate-50 dark:hover:bg-slate-900">
              <td className="px-4 py-2 font-medium text-slate-900 dark:text-slate-100">{t.name}</td>
              <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{t.channel}</td>
              <td className="px-4 py-2 text-slate-600 dark:text-slate-300">v{t.version}</td>
              <td className="px-4 py-2 max-w-md truncate text-slate-600 dark:text-slate-300" title={t.content}>
                {t.content}
              </td>
              <td className="px-4 py-2">
                <span
                  className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                    t.isActive
                      ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
                      : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
                  }`}
                >
                  {t.isActive ? 'Activa' : 'Inactiva'}
                </span>
              </td>
              <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{t.isCatalogTemplate ? 'Sí' : '—'}</td>
              <td className="px-4 py-2 space-x-3">
                <button
                  type="button"
                  onClick={() => activeMutation.mutate({ id: t.id, isActive: !t.isActive })}
                  disabled={activeMutation.isPending}
                  className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-60"
                >
                  {t.isActive ? 'Desactivar' : 'Activar'}
                </button>
                {!t.isCatalogTemplate && (
                  <button
                    type="button"
                    onClick={() => catalogMutation.mutate(t.id)}
                    disabled={catalogMutation.isPending}
                    className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-60"
                  >
                    Marcar como catálogo
                  </button>
                )}
              </td>
            </tr>
          ))}
          {templates.length === 0 && (
            <tr>
              <td colSpan={7} className="px-4 py-6 text-center text-slate-400">
                Sin plantillas todavía.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function MetaCatalogSection() {
  const queryClient = useQueryClient();
  const [pendingSync, setPendingSync] = useState<MetaWhatsAppTemplateDto | null>(null);

  const query = useQuery({ queryKey: ['meta-templates'], queryFn: listMetaTemplates });

  const syncMutation = useMutation({
    mutationFn: (template: MetaWhatsAppTemplateDto) =>
      syncTemplateFromMeta({ name: template.name, language: template.language }),
    onSuccess: () => {
      setPendingSync(null);
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });

  return (
    <div className="space-y-3">
      <div>
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Plantillas aprobadas en Meta</h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          Catálogo de plantillas de WhatsApp aprobadas para la WABA configurada. Sincronizar una la crea (o
          reactiva con nueva versión) como plantilla local, desactivando cualquier otra plantilla de WhatsApp
          activa que no sea catálogo.
        </p>
      </div>

      {query.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando...</p>}
      {query.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {query.error instanceof Error ? query.error.message : 'No se pudieron cargar las plantillas de Meta.'}
        </p>
      )}

      {query.data && (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
            <thead className="bg-slate-50 dark:bg-slate-900">
              <tr>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Nombre</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Idioma</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado en Meta</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Cuerpo</th>
                <th className="px-4 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
              {query.data.map((t) => (
                <tr key={`${t.name}-${t.language}`} className="hover:bg-slate-50 dark:hover:bg-slate-900">
                  <td className="px-4 py-2 font-medium text-slate-900 dark:text-slate-100">{t.name}</td>
                  <td className="px-4 py-2 text-slate-600 dark:text-slate-300">{t.language}</td>
                  <td className="px-4 py-2">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${metaStatusClass(t.status)}`}>
                      {t.status}
                    </span>
                  </td>
                  <td className="px-4 py-2 max-w-md truncate text-slate-600 dark:text-slate-300" title={t.bodyText ?? ''}>
                    {t.bodyText ?? '—'}
                  </td>
                  <td className="px-4 py-2">
                    <button
                      type="button"
                      onClick={() => setPendingSync(t)}
                      disabled={t.status !== 'APPROVED' || syncMutation.isPending}
                      className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-40"
                    >
                      Usar esta plantilla
                    </button>
                  </td>
                </tr>
              ))}
              {query.data.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                    Sin plantillas aprobadas en Meta (o falta configurar WhatsAppCloudApi:WabaId).
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {syncMutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {syncMutation.error instanceof Error ? syncMutation.error.message : 'No se pudo sincronizar la plantilla.'}
        </p>
      )}

      {pendingSync && (
        <ConfirmDialog
          title="Sincronizar plantilla desde Meta"
          message={`¿Usar "${pendingSync.name}" (${pendingSync.language}) como la plantilla activa de WhatsApp? Se desactivará cualquier otra plantilla de WhatsApp activa que no sea catálogo.`}
          confirmLabel="Sincronizar"
          isPending={syncMutation.isPending}
          onConfirm={() => syncMutation.mutate(pendingSync)}
          onCancel={() => setPendingSync(null)}
        />
      )}
    </div>
  );
}

export function TemplatesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Plantillas de mensajes</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Plantillas locales usadas por las campañas y catálogo de plantillas de WhatsApp aprobadas en Meta.
        </p>
      </div>

      <LocalTemplatesTable />
      <MetaCatalogSection />
    </div>
  );
}
