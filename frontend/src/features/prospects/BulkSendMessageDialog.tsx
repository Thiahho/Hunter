import { useMemo, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { addCampaignRecipients, createCampaign, processCampaignQueue, startCampaign } from '../../api/campaigns';
import { listTemplates } from '../../api/templates';
import type { MessagingChannel } from '../../api/messages';

const channels: MessagingChannel[] = ['Whatsapp', 'Email', 'Telegram', 'Sms'];

const channelLabels: Record<MessagingChannel, string> = {
  Whatsapp: 'WhatsApp',
  Email: 'Email',
  Telegram: 'Telegram',
  Sms: 'SMS',
};

interface BulkSendResult {
  added: number;
  alreadyInCampaign: number;
  withoutValidContact: number;
  suppressedAtAdd: number;
  sent: number;
  failed: number;
  suppressedAtSend: number;
}

const BATCH_SIZE = 50;

export function BulkSendMessageDialog({
  prospectIds,
  onClose,
  onSent,
}: {
  prospectIds: number[];
  onClose: () => void;
  onSent: () => void;
}) {
  const [channel, setChannel] = useState<MessagingChannel>('Whatsapp');
  const [templateId, setTemplateId] = useState<number | ''>('');
  const [result, setResult] = useState<BulkSendResult | null>(null);

  const templatesQuery = useQuery({ queryKey: ['templates'], queryFn: listTemplates });

  const availableTemplates = useMemo(
    () => (templatesQuery.data ?? []).filter((t) => t.channel === channel && t.isActive),
    [templatesQuery.data, channel],
  );

  const sendMutation = useMutation({
    mutationFn: async () => {
      if (!templateId) throw new Error('Elegí una plantilla.');

      const campaign = await createCampaign({
        name: `Envío masivo ${new Date().toLocaleString('es-AR')} (${prospectIds.length})`,
        channel,
        messageTemplateId: templateId,
      });

      const added = await addCampaignRecipients(campaign.id, prospectIds);
      await startCampaign(campaign.id);

      let sent = 0;
      let failed = 0;
      let suppressedAtSend = 0;
      for (;;) {
        const batch = await processCampaignQueue(campaign.id, BATCH_SIZE);
        sent += batch.sent;
        failed += batch.failed;
        suppressedAtSend += batch.suppressed;
        if (batch.processed === 0) break;
      }

      return {
        added: added.added,
        alreadyInCampaign: added.alreadyInCampaign,
        withoutValidContact: added.withoutValidContact,
        suppressedAtAdd: added.suppressed,
        sent,
        failed,
        suppressedAtSend,
      };
    },
    onSuccess: (r) => {
      setResult(r);
      onSent();
    },
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-md rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-lg"
      >
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Enviar mensaje masivo</h3>

        {result ? (
          <div className="mt-3 space-y-2 text-sm text-slate-600 dark:text-slate-300">
            <p>
              <strong className="text-slate-900 dark:text-slate-100">{result.sent}</strong> mensaje(s) enviado(s),{' '}
              <strong className="text-slate-900 dark:text-slate-100">{result.failed}</strong> fallido(s).
            </p>
            {(result.withoutValidContact > 0 || result.suppressedAtAdd > 0 || result.alreadyInCampaign > 0) && (
              <ul className="list-disc space-y-1 pl-5 text-xs text-slate-500 dark:text-slate-400">
                {result.withoutValidContact > 0 && (
                  <li>{result.withoutValidContact} sin contacto válido para el canal elegido.</li>
                )}
                {result.suppressedAtAdd > 0 && <li>{result.suppressedAtAdd} en lista de exclusión (suppression).</li>}
                {result.alreadyInCampaign > 0 && <li>{result.alreadyInCampaign} duplicado(s) en el envío.</li>}
              </ul>
            )}
            <div className="mt-4 flex justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500"
              >
                Cerrar
              </button>
            </div>
          </div>
        ) : (
          <>
            <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
              Se enviará a {prospectIds.length} prospecto(s) seleccionado(s).
            </p>

            <div className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-medium text-slate-500 dark:text-slate-400">Canal</label>
                <select
                  value={channel}
                  onChange={(e) => {
                    setChannel(e.target.value as MessagingChannel);
                    setTemplateId('');
                  }}
                  className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
                >
                  {channels.map((c) => (
                    <option key={c} value={c}>
                      {channelLabels[c]}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-500 dark:text-slate-400">Plantilla</label>
                <select
                  value={templateId}
                  onChange={(e) => setTemplateId(e.target.value ? Number(e.target.value) : '')}
                  disabled={templatesQuery.isLoading}
                  className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-1.5 text-sm text-slate-900 dark:text-slate-100"
                >
                  <option value="">Elegí una plantilla...</option>
                  {availableTemplates.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name} (v{t.version})
                    </option>
                  ))}
                </select>
                {templatesQuery.data && availableTemplates.length === 0 && (
                  <p className="mt-1 text-xs text-amber-600 dark:text-amber-400">
                    No hay plantillas activas para {channelLabels[channel]}. Creá una en Plantillas.
                  </p>
                )}
              </div>
            </div>

            {sendMutation.isError && (
              <p className="mt-3 text-sm text-red-600 dark:text-red-400">
                {sendMutation.error instanceof Error ? sendMutation.error.message : 'No se pudo enviar el mensaje.'}
              </p>
            )}

            <div className="mt-5 flex justify-end gap-2">
              <button
                type="button"
                onClick={onClose}
                disabled={sendMutation.isPending}
                className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={() => sendMutation.mutate()}
                disabled={!templateId || sendMutation.isPending}
                className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
              >
                {sendMutation.isPending ? 'Enviando…' : `Enviar a ${prospectIds.length}`}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
