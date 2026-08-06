import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import {
  addProspectContact,
  fetchProspectById,
  removeProspectContact,
  sendTestMessage,
  updateProspect,
  updateProspectContact,
  type BusinessSize,
  type Prospect,
  type ProspectCategory,
  type ProspectContact,
  type ProspectContactChannel,
  type ProspectStatus,
  type RecurrencePotential,
  type TestMessageResult,
  type UpdateProspectRequest,
} from '../../api/prospects';
import {
  searchMessageResponses,
  searchMessages,
  type IntentClassification,
  type MessageDto,
  type MessageResponseDto,
  type MessageStatus,
} from '../../api/messages';

const DEFAULT_TEST_MESSAGE =
  'Hola {{business_name}}! Somos Hunter. Mirá nuestro catálogo acá: https://tu-tienda.com/catalogo';

// Mismo criterio que buildMapsLink en ProspectsListPage: lat/long si están cargados (más
// preciso), si no arma una búsqueda de texto con nombre + dirección + ciudad + provincia.
function buildMapsLink(prospect: Prospect): string | null {
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

const businessSizes: BusinessSize[] = ['Unknown', 'Micro', 'Small', 'Medium', 'Large'];
const recurrencePotentials: RecurrencePotential[] = ['Unknown', 'Low', 'Medium', 'High'];
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

function toFormState(prospect: Prospect): UpdateProspectRequest {
  return {
    businessName: prospect.businessName,
    contactName: prospect.contactName,
    category: prospect.category,
    businessSize: prospect.businessSize,
    recurrencePotential: prospect.recurrencePotential,
    address: prospect.address,
    city: prospect.city,
    province: prospect.province,
    country: prospect.country,
    postalCode: prospect.postalCode,
    website: prospect.website,
    status: prospect.status,
  };
}

interface EditProspectFormProps {
  prospect: Prospect;
  onCancel: () => void;
  onSaved: () => void;
}

function EditProspectForm({ prospect, onCancel, onSaved }: EditProspectFormProps) {
  const [form, setForm] = useState<UpdateProspectRequest>(() => toFormState(prospect));

  const mutation = useMutation({
    mutationFn: () => updateProspect(prospect.id, form),
    onSuccess: onSaved,
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    mutation.mutate();
  }

  function field<K extends keyof UpdateProspectRequest>(key: K, value: UpdateProspectRequest[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  const inputClass =
    'mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100';
  const labelClass = 'block text-sm font-medium text-slate-700 dark:text-slate-300';

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <div>
        <label className={labelClass}>Nombre del negocio</label>
        <input
          type="text"
          required
          value={form.businessName}
          onChange={(e) => field('businessName', e.target.value)}
          className={inputClass}
        />
      </div>

      <div>
        <label className={labelClass}>Contacto</label>
        <input
          type="text"
          value={form.contactName ?? ''}
          onChange={(e) => field('contactName', e.target.value || null)}
          className={inputClass}
        />
      </div>

      <div>
        <label className={labelClass}>Dirección</label>
        <input
          type="text"
          value={form.address ?? ''}
          onChange={(e) => field('address', e.target.value || null)}
          className={inputClass}
        />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <label className={labelClass}>Ciudad</label>
          <input
            type="text"
            value={form.city ?? ''}
            onChange={(e) => field('city', e.target.value || null)}
            className={inputClass}
          />
        </div>
        <div>
          <label className={labelClass}>Provincia</label>
          <input
            type="text"
            value={form.province ?? ''}
            onChange={(e) => field('province', e.target.value || null)}
            className={inputClass}
          />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <label className={labelClass}>País</label>
          <input
            type="text"
            value={form.country ?? ''}
            onChange={(e) => field('country', e.target.value || null)}
            className={inputClass}
          />
        </div>
        <div>
          <label className={labelClass}>Código postal</label>
          <input
            type="text"
            value={form.postalCode ?? ''}
            onChange={(e) => field('postalCode', e.target.value || null)}
            className={inputClass}
          />
        </div>
      </div>

      <div>
        <label className={labelClass}>Sitio web</label>
        <input
          type="text"
          value={form.website ?? ''}
          onChange={(e) => field('website', e.target.value || null)}
          className={inputClass}
        />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <label className={labelClass}>Categoría</label>
          <select
            value={form.category}
            onChange={(e) => field('category', e.target.value as ProspectCategory)}
            className={inputClass}
          >
            {categories.map((c) => (
              <option key={c} value={c}>
                {categoryLabels[c]}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className={labelClass}>Tamaño de negocio</label>
          <select
            value={form.businessSize}
            onChange={(e) => field('businessSize', e.target.value as BusinessSize)}
            className={inputClass}
          >
            {businessSizes.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <label className={labelClass}>Potencial de recurrencia</label>
          <select
            value={form.recurrencePotential}
            onChange={(e) => field('recurrencePotential', e.target.value as RecurrencePotential)}
            className={inputClass}
          >
            {recurrencePotentials.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className={labelClass}>Estado</label>
          <select
            value={form.status}
            onChange={(e) => field('status', e.target.value as ProspectStatus)}
            className={inputClass}
          >
            {statuses.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      </div>

      {mutation.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {mutation.error instanceof Error ? mutation.error.message : 'Ocurrió un error inesperado.'}
        </p>
      )}

      <div className="flex gap-2 pt-1">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          {mutation.isPending ? 'Guardando…' : 'Guardar'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={mutation.isPending}
          className="rounded-md border border-slate-300 dark:border-slate-700 px-3 py-1.5 text-sm font-medium text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}

const contactChannels: ProspectContactChannel[] = ['Phone', 'Whatsapp', 'Email', 'Instagram', 'Facebook'];

function buildWhatsAppLink(phone: string): string {
  const digits = phone.replace(/[^\d]/g, '');
  return `https://wa.me/${digits}`;
}

const contactInputClass =
  'rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-2 py-1 text-sm text-slate-900 dark:text-slate-100';

interface ContactRowProps {
  prospectId: number;
  contact: ProspectContact;
  canDelete: boolean;
}

function ContactRow({ prospectId, contact, canDelete }: ContactRowProps) {
  const queryClient = useQueryClient();
  const [isEditing, setIsEditing] = useState(false);
  const [channel, setChannel] = useState<ProspectContactChannel>(contact.channel);
  const [value, setValue] = useState(contact.value);
  const [isPrimary, setIsPrimary] = useState(contact.isPrimary);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['prospects', prospectId] });

  const updateMutation = useMutation({
    mutationFn: () => updateProspectContact(prospectId, contact.id, { channel, value, isPrimary }),
    onSuccess: () => {
      setIsEditing(false);
      invalidate();
    },
  });

  const removeMutation = useMutation({
    mutationFn: () => removeProspectContact(prospectId, contact.id),
    onSuccess: invalidate,
  });

  if (isEditing) {
    return (
      <li className="space-y-2 rounded-md border border-slate-200 dark:border-slate-800 p-2 text-sm">
        <div className="flex flex-wrap items-center gap-2">
          <select value={channel} onChange={(e) => setChannel(e.target.value as ProspectContactChannel)} className={contactInputClass}>
            {contactChannels.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
          <input value={value} onChange={(e) => setValue(e.target.value)} className={`flex-1 ${contactInputClass}`} />
          <label className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-300">
            <input type="checkbox" checked={isPrimary} onChange={(e) => setIsPrimary(e.target.checked)} />
            Principal
          </label>
        </div>
        {(updateMutation.isError || removeMutation.isError) && (
          <p className="text-xs text-red-600 dark:text-red-400">
            {(updateMutation.error ?? removeMutation.error) instanceof Error
              ? ((updateMutation.error ?? removeMutation.error) as Error).message
              : 'Ocurrió un error inesperado.'}
          </p>
        )}
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => updateMutation.mutate()}
            disabled={updateMutation.isPending || !value.trim()}
            className="rounded-md bg-indigo-600 px-2 py-1 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {updateMutation.isPending ? 'Guardando…' : 'Guardar'}
          </button>
          <button
            type="button"
            onClick={() => setIsEditing(false)}
            disabled={updateMutation.isPending}
            className="rounded-md border border-slate-300 dark:border-slate-700 px-2 py-1 text-xs text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800"
          >
            Cancelar
          </button>
          {canDelete && (
            <button
              type="button"
              onClick={() => removeMutation.mutate()}
              disabled={removeMutation.isPending}
              className="ml-auto rounded-md px-2 py-1 text-xs text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-500/10"
            >
              {removeMutation.isPending ? 'Eliminando…' : 'Eliminar'}
            </button>
          )}
        </div>
      </li>
    );
  }

  return (
    <li className="flex items-center justify-between text-sm">
      <span className="text-slate-600 dark:text-slate-300">
        {contact.channel}:{' '}
        {contact.channel === 'Whatsapp' ? (
          <a
            href={buildWhatsAppLink(contact.value)}
            target="_blank"
            rel="noreferrer"
            className="text-emerald-600 dark:text-emerald-400 hover:underline"
          >
            {contact.value}
          </a>
        ) : (
          contact.value
        )}
      </span>
      <span className="flex items-center gap-1">
        {contact.isPrimary && (
          <span className="rounded bg-indigo-50 dark:bg-indigo-500/10 px-1.5 py-0.5 text-xs text-indigo-700 dark:text-indigo-300">
            Principal
          </span>
        )}
        {contact.isVerified && (
          <span className="rounded bg-emerald-50 dark:bg-emerald-500/10 px-1.5 py-0.5 text-xs text-emerald-700 dark:text-emerald-300">
            Verificado
          </span>
        )}
        <button
          type="button"
          onClick={() => setIsEditing(true)}
          className="text-xs text-indigo-600 dark:text-indigo-400 hover:underline"
        >
          Editar
        </button>
      </span>
    </li>
  );
}

function AddContactForm({ prospectId, hasContacts }: { prospectId: number; hasContacts: boolean }) {
  const queryClient = useQueryClient();
  const [channel, setChannel] = useState<ProspectContactChannel>('Whatsapp');
  const [value, setValue] = useState('');
  const [isPrimary, setIsPrimary] = useState(!hasContacts);

  const mutation = useMutation({
    mutationFn: () => addProspectContact(prospectId, { channel, value, isPrimary }),
    onSuccess: () => {
      setValue('');
      setIsPrimary(false);
      queryClient.invalidateQueries({ queryKey: ['prospects', prospectId] });
    },
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    mutation.mutate();
  }

  return (
    <form onSubmit={handleSubmit} className="mt-3 space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <select value={channel} onChange={(e) => setChannel(e.target.value as ProspectContactChannel)} className={contactInputClass}>
          {contactChannels.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
        <input
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder="Nuevo contacto"
          className={`flex-1 ${contactInputClass}`}
        />
        <label className="flex items-center gap-1 text-xs text-slate-600 dark:text-slate-300">
          <input type="checkbox" checked={isPrimary} onChange={(e) => setIsPrimary(e.target.checked)} />
          Principal
        </label>
        <button
          type="submit"
          disabled={mutation.isPending || !value.trim()}
          className="rounded-md bg-indigo-600 px-2 py-1 text-xs font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          {mutation.isPending ? 'Agregando…' : 'Agregar'}
        </button>
      </div>
      {mutation.isError && (
        <p className="text-xs text-red-600 dark:text-red-400">
          {mutation.error instanceof Error ? mutation.error.message : 'Ocurrió un error inesperado.'}
        </p>
      )}
    </form>
  );
}

const conversationStatusLabels: Record<MessageStatus, string> = {
  Pending: 'Pendiente',
  Sent: 'Enviado',
  Delivered: 'Entregado',
  Read: 'Leído',
  Failed: 'Falló',
  Cancelled: 'Cancelado',
};

const conversationClassificationLabels: Record<IntentClassification, string> = {
  Interested: 'Interesado',
  NotInterested: 'No interesado',
  Question: 'Pregunta',
  Unclear: 'Sin clasificar',
  Stop: 'Baja (STOP)',
};

type ConversationEntry =
  | { kind: 'sent'; at: string; data: MessageDto }
  | { kind: 'received'; at: string; data: MessageResponseDto };

function ConversationSection({ prospectId }: { prospectId: number }) {
  const messagesQuery = useQuery({
    queryKey: ['messages', { prospectId }],
    queryFn: () => searchMessages({ prospectId, pageSize: 100 }),
    refetchInterval: 15000,
  });
  const responsesQuery = useQuery({
    queryKey: ['message-responses', { prospectId }],
    queryFn: () => searchMessageResponses({ prospectId, pageSize: 100 }),
    refetchInterval: 15000,
  });

  const isRefetching = messagesQuery.isFetching || responsesQuery.isFetching;

  function refreshNow() {
    messagesQuery.refetch();
    responsesQuery.refetch();
  }

  if (messagesQuery.isLoading || responsesQuery.isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando conversación...</p>;
  }

  const entries: ConversationEntry[] = [
    ...(messagesQuery.data?.items.map((m): ConversationEntry => ({ kind: 'sent', at: m.createdAt, data: m })) ?? []),
    ...(responsesQuery.data?.items.map((r): ConversationEntry => ({ kind: 'received', at: r.receivedAt, data: r })) ?? []),
  ].sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-end gap-2">
        <span className="text-xs text-slate-400">Se actualiza solo cada 15s</span>
        <button
          type="button"
          onClick={refreshNow}
          disabled={isRefetching}
          className="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:underline disabled:opacity-60"
        >
          {isRefetching ? 'Actualizando…' : 'Actualizar'}
        </button>
      </div>

      {entries.length === 0 ? (
        <p className="text-sm text-slate-400">Todavía no hay mensajes con este prospecto.</p>
      ) : (
        <ul className="space-y-2">
          {entries.map((entry) =>
            entry.kind === 'sent' ? (
          <li key={`sent-${entry.data.id}`} className="flex justify-end">
            <div className="max-w-md rounded-lg rounded-br-none bg-indigo-600 px-3 py-2 text-sm text-white">
              <p className="whitespace-pre-wrap">{entry.data.content}</p>
              <p className="mt-1 text-right text-xs text-indigo-200">
                {conversationStatusLabels[entry.data.status]}
                {entry.data.failureReason ? ` — ${entry.data.failureReason}` : ''} ·{' '}
                {new Date(entry.at).toLocaleString('es-AR')}
              </p>
            </div>
          </li>
        ) : (
          <li key={`recv-${entry.data.id}`} className="flex justify-start">
            <div className="max-w-md rounded-lg rounded-bl-none bg-slate-100 dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100">
              <p className="whitespace-pre-wrap">
                {entry.data.content || (entry.data.buttonPayload ? `[Botón] ${entry.data.buttonPayload}` : '—')}
              </p>
              <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                {conversationClassificationLabels[entry.data.classification]}
                {entry.data.buttonPayload ? ' · Botón' : ' · Texto libre'} · {new Date(entry.at).toLocaleString('es-AR')}
              </p>
            </div>
          </li>
            ),
          )}
        </ul>
      )}
    </div>
  );
}

export function ProspectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const prospectId = Number(id);
  const queryClient = useQueryClient();
  const [testMessage, setTestMessage] = useState(DEFAULT_TEST_MESSAGE);
  const [lastResult, setLastResult] = useState<TestMessageResult | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['prospects', prospectId],
    queryFn: () => fetchProspectById(prospectId),
    enabled: Number.isFinite(prospectId),
  });

  const testMessageMutation = useMutation({
    mutationFn: () => sendTestMessage(prospectId, testMessage),
    onSuccess: (result) => {
      setLastResult(result);
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: ['prospects', prospectId] });
      }
    },
  });

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Cargando prospecto...</p>;
  }

  if (isError || !data) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {error instanceof Error ? error.message : 'No se pudo cargar el prospecto.'}
      </p>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <Link to="/app/prospects" className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline">
          ← Volver a prospectos
        </Link>
        <h2 className="mt-2 text-lg font-semibold text-slate-900 dark:text-slate-100">{data.businessName}</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {categoryLabels[data.category]} · {data.status}
        </p>
      </div>

      <div className="grid gap-6 sm:grid-cols-2">
        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-medium text-slate-500 dark:text-slate-400">Datos generales</h3>
            {!isEditing && (
              <button
                onClick={() => setIsEditing(true)}
                className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline"
              >
                Editar
              </button>
            )}
          </div>

          {isEditing ? (
            <EditProspectForm
              prospect={data}
              onCancel={() => setIsEditing(false)}
              onSaved={() => {
                setIsEditing(false);
                queryClient.invalidateQueries({ queryKey: ['prospects', prospectId] });
              }}
            />
          ) : (
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Contacto</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.contactName ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Dirección</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.address ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Ciudad</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.city ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Provincia</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.province ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">País</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.country ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Código postal</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.postalCode ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Mapa</dt>
                <dd className="text-slate-900 dark:text-slate-100">
                  {(() => {
                    const mapsLink = buildMapsLink(data);
                    return mapsLink ? (
                      <a
                        href={mapsLink}
                        target="_blank"
                        rel="noreferrer"
                        className="text-indigo-600 dark:text-indigo-400 hover:underline"
                      >
                        📍 Ver mapa
                      </a>
                    ) : (
                      '—'
                    );
                  })()}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Tamaño de negocio</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.businessSize}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Potencial de recurrencia</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.recurrencePotential}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Sitio web</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.website ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Score comercial</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.commercialScore ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Prioridad operativa</dt>
                <dd className="text-slate-900 dark:text-slate-100">{data.operationalPriority ?? '—'}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Último contacto</dt>
                <dd className="text-slate-900 dark:text-slate-100">
                  {data.lastContactedAt ? new Date(data.lastContactedAt).toLocaleString('es-AR') : '—'}
                </dd>
              </div>
            </dl>
          )}
        </section>

        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Contactos</h3>
          {data.contacts.length === 0 && <p className="text-sm text-slate-400">Sin contactos registrados.</p>}
          <ul className="space-y-2">
            {data.contacts.map((c) => (
              <ContactRow key={c.id} prospectId={data.id} contact={c} canDelete={data.contacts.length > 1} />
            ))}
          </ul>
          <AddContactForm prospectId={data.id} hasContacts={data.contacts.length > 0} />

          <h3 className="mt-4 mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Tags</h3>
          {data.tags.length === 0 && <p className="text-sm text-slate-400">Sin tags.</p>}
          <div className="flex flex-wrap gap-1.5">
            {data.tags.map((tag) => (
              <span
                key={tag}
                className="rounded-full bg-slate-100 dark:bg-slate-800 px-2.5 py-0.5 text-xs text-slate-600 dark:text-slate-300"
              >
                {tag}
              </span>
            ))}
          </div>

          <h3 className="mt-4 mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Fuentes</h3>
          {data.sources.length === 0 && <p className="text-sm text-slate-400">Sin fuentes registradas.</p>}
          <ul className="space-y-1 text-sm text-slate-600 dark:text-slate-300">
            {data.sources.map((s) => (
              <li key={s.id}>
                {s.sourceType}
                {s.sourceUrl ? ` — ${s.sourceUrl}` : ''}
              </li>
            ))}
          </ul>
        </section>
      </div>

      <details className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4" open>
        <summary className="cursor-pointer text-sm font-medium text-slate-500 dark:text-slate-400">Conversación</summary>
        <div className="mt-3 max-h-96 overflow-y-auto pr-1">
          <ConversationSection prospectId={data.id} />
        </div>
      </details>

      {data.contacts.some((c) => c.channel === 'Whatsapp') && (
        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-1 text-sm font-medium text-slate-500 dark:text-slate-400">Mensaje de prueba (WhatsApp)</h3>
          <p className="mb-3 text-xs text-slate-400">
            Envía este mensaje directamente al contacto de WhatsApp de este prospecto, sin pasar por una campaña.
            Usalo solo con números propios/de prueba durante desarrollo.
          </p>
          <textarea
            value={testMessage}
            onChange={(e) => setTestMessage(e.target.value)}
            rows={3}
            className="w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100"
          />
          <div className="mt-3 flex items-center gap-3">
            <button
              onClick={() => testMessageMutation.mutate()}
              disabled={testMessageMutation.isPending || !testMessage.trim()}
              className="rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {testMessageMutation.isPending ? 'Enviando…' : 'Enviar mensaje de prueba'}
            </button>
            {lastResult && (
              <span className={`text-sm ${lastResult.success ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}>
                {lastResult.success
                  ? `Enviado (proveedor: ${lastResult.externalMessageId ?? 'sin id'})`
                  : `Falló: ${lastResult.error}`}
              </span>
            )}
          </div>
        </section>
      )}
    </div>
  );
}
