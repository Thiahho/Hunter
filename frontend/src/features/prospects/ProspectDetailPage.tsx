import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import { fetchProspectById } from '../../api/prospects';

export function ProspectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const prospectId = Number(id);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['prospects', prospectId],
    queryFn: () => fetchProspectById(prospectId),
    enabled: Number.isFinite(prospectId),
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
          {data.category} · {data.status}
        </p>
      </div>

      <div className="grid gap-6 sm:grid-cols-2">
        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Datos generales</h3>
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
              <dt className="text-slate-500 dark:text-slate-400">Tamaño de negocio</dt>
              <dd className="text-slate-900 dark:text-slate-100">{data.businessSize}</dd>
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
        </section>

        <section className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <h3 className="mb-3 text-sm font-medium text-slate-500 dark:text-slate-400">Contactos</h3>
          {data.contacts.length === 0 && <p className="text-sm text-slate-400">Sin contactos registrados.</p>}
          <ul className="space-y-2">
            {data.contacts.map((c) => (
              <li key={c.id} className="flex items-center justify-between text-sm">
                <span className="text-slate-600 dark:text-slate-300">
                  {c.channel}: {c.value}
                </span>
                <span className="flex gap-1">
                  {c.isPrimary && (
                    <span className="rounded bg-indigo-50 dark:bg-indigo-500/10 px-1.5 py-0.5 text-xs text-indigo-700 dark:text-indigo-300">
                      Principal
                    </span>
                  )}
                  {c.isVerified && (
                    <span className="rounded bg-emerald-50 dark:bg-emerald-500/10 px-1.5 py-0.5 text-xs text-emerald-700 dark:text-emerald-300">
                      Verificado
                    </span>
                  )}
                </span>
              </li>
            ))}
          </ul>

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
    </div>
  );
}
