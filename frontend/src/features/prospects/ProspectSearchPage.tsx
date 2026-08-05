import { useEffect, useState, type FormEvent, type KeyboardEvent } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import type { ProspectCategory } from '../../api/prospects';
import {
  cancelImport,
  confirmImport,
  getImportRecords,
  searchOpenStreetMap,
  type ImportConfirmResultDto,
  type ImportPreviewDto,
} from '../../api/imports';

// Único subconjunto de ProspectCategory con equivalente buscable en OpenStreetMap
// (OpenStreetMapCategories.Supported en el backend). Sin label compartido a propósito: sigue
// el mismo patrón que categoryLabels/statusLabels en ProspectsListPage (duplicado por página).
const categoryOptions: { value: ProspectCategory; label: string }[] = [
  { value: 'AutoPartsStore', label: 'Repuestería' },
  { value: 'Workshop', label: 'Taller' },
  { value: 'Lubricentro', label: 'Lubricentro' },
  { value: 'TireShop', label: 'Gomería' },
  { value: 'Reseller', label: 'Concesionaria / reventa' },
];

// Sinónimos en español para cada categoría: el dominio (ProspectCategory) es un enum cerrado
// con solo 5 rubros mapeables a OSM (OpenStreetMapCategories.Supported), así que la búsqueda
// "escrita" no agrega rubros nuevos, solo resuelve distintas formas de nombrar los mismos 5.
const categorySynonyms: { value: ProspectCategory; terms: string[] }[] = [
  { value: 'AutoPartsStore', terms: ['repuestería', 'repuesteria', 'repuestos', 'autopartes', 'auto partes'] },
  { value: 'Workshop', terms: ['taller', 'taller mecánico', 'taller mecanico', 'mecánica', 'mecanica'] },
  { value: 'Lubricentro', terms: ['lubricentro', 'cambio de aceite', 'lubricantes'] },
  { value: 'TireShop', terms: ['gomería', 'gomeria', 'neumáticos', 'neumaticos', 'cubiertas'] },
  { value: 'Reseller', terms: ['concesionaria', 'concesionaria / reventa', 'reventa', 'agencia de autos'] },
];

const categoryLabelByValue = new Map(categoryOptions.map((o) => [o.value, o.label]));

// "Unknown" es el único valor fuera de categoryOptions que OpenStreetMapClient.MapCategory
// puede devolver realmente (Distributor/Other no tienen tag de OSM, ver OpenStreetMapCategories
// .Supported en el backend), así que alcanza con este único fallback extra para la tabla de
// preview de la búsqueda.
function categoryPreviewLabel(category: string | null): string {
  if (!category) return '—';
  if (category === 'Unknown') return 'Sin clasificar';
  return categoryLabelByValue.get(category as ProspectCategory) ?? category;
}

function resolveCategoryFromText(text: string): ProspectCategory | null {
  const normalized = text
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '');
  if (!normalized) return null;

  for (const entry of categorySynonyms) {
    const matches = entry.terms.some((term) => {
      const normalizedTerm = term.normalize('NFD').replace(/\p{Diacritic}/gu, '');
      return normalizedTerm === normalized || normalizedTerm.includes(normalized) || normalized.includes(normalizedTerm);
    });
    if (matches) return entry.value;
  }
  return null;
}

const MAX_LOCALITIES = 5;
const RADIUS_OPTIONS_KM = [5, 10, 20, 50];

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100';

export function ProspectSearchPage() {
  const [selectedCategories, setSelectedCategories] = useState<ProspectCategory[]>([]);
  const [categoryInput, setCategoryInput] = useState('');
  const [categoryError, setCategoryError] = useState<string | null>(null);
  const [localityInput, setLocalityInput] = useState('');
  const [localities, setLocalities] = useState<string[]>([]);
  const [radiusKm, setRadiusKm] = useState(10);
  const [maxResults, setMaxResults] = useState(50);

  const [preview, setPreview] = useState<ImportPreviewDto | null>(null);
  const [batchId, setBatchId] = useState<number | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [lastResult, setLastResult] = useState<ImportConfirmResultDto | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);

  const searchMutation = useMutation({
    mutationFn: searchOpenStreetMap,
    onSuccess: (result) => {
      setLastResult(null);
      setPreview(result);
      setBatchId(result.batchId);
    },
  });

  // Overpass (el servidor público de OSM) puede tardar bastante bajo carga (ver
  // OpenStreetMapClient.PostWithRetryAsync — reintenta con backoff en 429/504), así que un
  // simple "Buscando…" no alcanza para saber si sigue vivo. El contador de segundos es la señal
  // más simple de "esto sigue corriendo, no se colgó".
  useEffect(() => {
    if (!searchMutation.isPending) {
      setElapsedSeconds(0);
      return;
    }
    const interval = setInterval(() => setElapsedSeconds((s) => s + 1), 1000);
    return () => clearInterval(interval);
  }, [searchMutation.isPending]);

  const recordsQuery = useQuery({
    queryKey: ['import-records', batchId],
    queryFn: () => getImportRecords(batchId!),
    enabled: batchId !== null,
  });

  useEffect(() => {
    if (recordsQuery.data) {
      setSelectedIds(new Set(recordsQuery.data.filter((r) => r.status === 'Valid').map((r) => r.id)));
    }
  }, [recordsQuery.data]);

  const confirmMutation = useMutation({
    mutationFn: () => confirmImport(batchId!, [...selectedIds]),
    onSuccess: (result) => {
      setLastResult(result);
      resetSearch();
    },
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelImport(batchId!),
    onSuccess: () => resetSearch(),
  });

  function resetSearch() {
    setBatchId(null);
    setPreview(null);
    setSelectedIds(new Set());
  }

  function addCategory() {
    const trimmed = categoryInput.trim();
    if (!trimmed) return;

    const resolved = resolveCategoryFromText(trimmed);
    if (!resolved) {
      setCategoryError(`No se reconoce el rubro "${trimmed}". Probá con: repuestería, taller, lubricentro, gomería o concesionaria.`);
      return;
    }

    setCategoryError(null);
    setSelectedCategories((prev) => (prev.includes(resolved) ? prev : [...prev, resolved]));
    setCategoryInput('');
  }

  function handleCategoryKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault();
      addCategory();
    }
  }

  function removeCategory(category: ProspectCategory) {
    setSelectedCategories((prev) => prev.filter((c) => c !== category));
  }

  function addLocality() {
    const trimmed = localityInput.trim();
    if (trimmed && !localities.includes(trimmed) && localities.length < MAX_LOCALITIES) {
      setLocalities((prev) => [...prev, trimmed]);
    }
    setLocalityInput('');
  }

  function handleLocalityKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault();
      addLocality();
    }
  }

  function removeLocality(name: string) {
    setLocalities((prev) => prev.filter((l) => l !== name));
  }

  function toggleRecord(id: number) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (localities.length === 0) return;

    searchMutation.mutate({
      localities,
      categories: selectedCategories.length > 0 ? selectedCategories : undefined,
      radiusKm,
      maxResults,
    });
  }

  const records = recordsQuery.data ?? [];
  const selectableRecords = records.filter((r) => r.status === 'Valid');

  return (
    <div className="max-w-4xl space-y-4">
      <div>
        <Link to="/app/prospects" className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline">
          ← Volver a prospectos
        </Link>
        <h2 className="mt-2 text-lg font-semibold text-slate-900 dark:text-slate-100">Buscar prospectos (OpenStreetMap)</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Buscá negocios por rubro y zona. Revisá los resultados y elegí cuáles importar como prospectos.
        </p>
      </div>

      {lastResult && (
        <p className="rounded-md border border-emerald-200 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-900/20 px-3 py-2 text-sm text-emerald-700 dark:text-emerald-300">
          Se importaron {lastResult.created} prospecto(s).
        </p>
      )}

      <form
        onSubmit={handleSubmit}
        className="space-y-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5"
      >
        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Rubro</label>
          <div className="mt-1 flex gap-2">
            <input
              type="text"
              value={categoryInput}
              onChange={(e) => {
                setCategoryInput(e.target.value);
                if (categoryError) setCategoryError(null);
              }}
              onKeyDown={handleCategoryKeyDown}
              list="category-suggestions"
              placeholder="ej. gomería, taller, lubricentro"
              className={inputClass}
            />
            <datalist id="category-suggestions">
              {categoryOptions.map((option) => (
                <option key={option.value} value={option.label} />
              ))}
            </datalist>
            <button
              type="button"
              onClick={addCategory}
              className="mt-1 shrink-0 rounded-md border border-slate-300 dark:border-slate-700 px-3 py-2 text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
            >
              Agregar
            </button>
          </div>
          {categoryError && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{categoryError}</p>}
          {selectedCategories.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-2">
              {selectedCategories.map((category) => (
                <span
                  key={category}
                  className="flex items-center gap-1 rounded-full bg-indigo-50 dark:bg-indigo-500/10 px-3 py-1 text-xs font-medium text-indigo-700 dark:text-indigo-300"
                >
                  {categoryLabelByValue.get(category) ?? category}
                  <button
                    type="button"
                    onClick={() => removeCategory(category)}
                    className="text-indigo-500 hover:text-indigo-800 dark:hover:text-indigo-100"
                    aria-label={`Quitar ${categoryLabelByValue.get(category) ?? category}`}
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
          <p className="mt-1 text-xs text-slate-400">Sin selección = se buscan todos los rubros.</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">
            Zonas o localidades (máx. {MAX_LOCALITIES})
          </label>
          <div className="mt-1 flex gap-2">
            <input
              type="text"
              value={localityInput}
              onChange={(e) => setLocalityInput(e.target.value)}
              onKeyDown={handleLocalityKeyDown}
              disabled={localities.length >= MAX_LOCALITIES}
              placeholder="ej. Moreno"
              className={inputClass}
            />
            <button
              type="button"
              onClick={addLocality}
              disabled={localities.length >= MAX_LOCALITIES}
              className="mt-1 shrink-0 rounded-md border border-slate-300 dark:border-slate-700 px-3 py-2 text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40"
            >
              Agregar
            </button>
          </div>
          {localities.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-2">
              {localities.map((locality) => (
                <span
                  key={locality}
                  className="flex items-center gap-1 rounded-full bg-indigo-50 dark:bg-indigo-500/10 px-3 py-1 text-xs font-medium text-indigo-700 dark:text-indigo-300"
                >
                  {locality}
                  <button
                    type="button"
                    onClick={() => removeLocality(locality)}
                    className="text-indigo-500 hover:text-indigo-800 dark:hover:text-indigo-100"
                    aria-label={`Quitar ${locality}`}
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
        </div>

        <div className="flex flex-wrap items-end gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Radio de búsqueda</label>
            <select
              value={radiusKm}
              onChange={(e) => setRadiusKm(Number(e.target.value))}
              className={`${inputClass} w-32`}
            >
              {RADIUS_OPTIONS_KM.map((km) => (
                <option key={km} value={km}>
                  {km} km
                </option>
              ))}
            </select>
            <p className="mt-1 text-xs text-slate-400">Alrededor de cada localidad.</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Máximo de resultados</label>
            <input
              type="number"
              min={1}
              max={300}
              value={maxResults}
              onChange={(e) => setMaxResults(Number(e.target.value))}
              className={`${inputClass} w-28`}
            />
          </div>
        </div>

        {searchMutation.isError && (
          <p className="text-sm text-red-600 dark:text-red-400">
            {searchMutation.error instanceof Error ? searchMutation.error.message : 'Ocurrió un error inesperado.'}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={searchMutation.isPending || localities.length === 0}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {searchMutation.isPending ? `Buscando… (${elapsedSeconds}s)` : 'Buscar'}
          </button>
          {searchMutation.isPending && (
            <p className="text-xs text-slate-400">
              Puede tardar hasta 1 minuto según la carga del servidor público de OpenStreetMap. Sigue en curso, no
              cierres esta pestaña.
            </p>
          )}
        </div>
      </form>

      {recordsQuery.isLoading && <p className="text-sm text-slate-500 dark:text-slate-400">Cargando resultados...</p>}

      {recordsQuery.isError && (
        <p className="text-sm text-red-600 dark:text-red-400">
          {recordsQuery.error instanceof Error ? recordsQuery.error.message : 'No se pudieron cargar los resultados.'}
        </p>
      )}

      {preview && recordsQuery.data && (
        <div className="space-y-3 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5">
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {preview.validRecords} válidos · {preview.duplicateRecords} duplicados · {preview.invalidRecords} inválidos
          </p>

          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400"></th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Nombre</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Rubro</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Teléfono</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">WhatsApp</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Dirección</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Ciudad</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {records.map((record) => (
                  <tr
                    key={record.id}
                    className={record.status !== 'Valid' ? 'opacity-50' : 'hover:bg-slate-50 dark:hover:bg-slate-900'}
                  >
                    <td className="px-3 py-2">
                      {record.status === 'Valid' && (
                        <input
                          type="checkbox"
                          checked={selectedIds.has(record.id)}
                          onChange={() => toggleRecord(record.id)}
                          className="rounded border-slate-300 dark:border-slate-700"
                        />
                      )}
                    </td>
                    <td className="px-3 py-2 text-slate-900 dark:text-slate-100">
                      {record.businessName ?? '—'}
                      {record.status !== 'Valid' && (
                        <span className="ml-2 text-xs text-slate-400">
                          ({record.status === 'Duplicate' ? 'duplicado' : 'inválido'}
                          {record.errorMessage ? `: ${record.errorMessage}` : ''})
                        </span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{categoryPreviewLabel(record.category)}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{record.phone ?? '—'}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{record.whatsapp ?? '—'}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{record.address ?? '—'}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{record.city ?? '—'}</td>
                  </tr>
                ))}
                {records.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-slate-400">
                      Sin resultados.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {confirmMutation.isError && (
            <p className="text-sm text-red-600 dark:text-red-400">
              {confirmMutation.error instanceof Error ? confirmMutation.error.message : 'No se pudo confirmar la importación.'}
            </p>
          )}

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => confirmMutation.mutate()}
              disabled={confirmMutation.isPending || selectableRecords.length === 0 || selectedIds.size === 0}
              className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
            >
              {confirmMutation.isPending ? 'Confirmando…' : `Confirmar (${selectedIds.size} seleccionados)`}
            </button>
            <button
              type="button"
              onClick={() => cancelMutation.mutate()}
              disabled={cancelMutation.isPending}
              className="rounded-md border border-slate-300 dark:border-slate-700 px-4 py-2 text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-60"
            >
              Cancelar búsqueda
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
