import { useState, type FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { createProspect, type ProspectCategory } from '../../api/prospects';

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

export function ProspectCreatePage() {
  const navigate = useNavigate();
  const [businessName, setBusinessName] = useState('');
  const [category, setCategory] = useState<ProspectCategory>('Unknown');
  const [whatsapp, setWhatsapp] = useState('');
  const [city, setCity] = useState('');

  const mutation = useMutation({
    mutationFn: createProspect,
    onSuccess: (prospect) => {
      navigate(`/app/prospects/${prospect.id}`, { replace: true });
    },
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    mutation.mutate({
      businessName,
      category,
      businessSize: 'Unknown',
      recurrencePotential: 'Unknown',
      city: city || null,
      contacts: [{ channel: 'Whatsapp', value: whatsapp, isPrimary: true }],
      sourceType: 'Manual',
    });
  }

  return (
    <div className="max-w-lg space-y-4">
      <div>
        <Link to="/app/prospects" className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline">
          ← Volver a prospectos
        </Link>
        <h2 className="mt-2 text-lg font-semibold text-slate-900 dark:text-slate-100">Nuevo prospecto (manual)</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Útil para cargar un número propio y probar el envío de mensajes sin usar prospectos reales.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5">
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre del negocio</label>
          <input
            type="text"
            required
            value={businessName}
            onChange={(e) => setBusinessName(e.target.value)}
            className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100"
            placeholder="ej. Prueba personal"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Número de WhatsApp</label>
          <input
            type="text"
            required
            value={whatsapp}
            onChange={(e) => setWhatsapp(e.target.value)}
            className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100"
            placeholder="ej. 011 15 1234-5678"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Categoría</label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value as ProspectCategory)}
            className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100"
          >
            {categories.map((c) => (
              <option key={c} value={c}>
                {categoryLabels[c]}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Ciudad (opcional)</label>
          <input
            type="text"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            className="mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100"
          />
        </div>

        {mutation.isError && (
          <p className="text-sm text-red-600 dark:text-red-400">
            {mutation.error instanceof Error ? mutation.error.message : 'Ocurrió un error inesperado.'}
          </p>
        )}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          {mutation.isPending ? 'Creando…' : 'Crear prospecto'}
        </button>
      </form>
    </div>
  );
}
