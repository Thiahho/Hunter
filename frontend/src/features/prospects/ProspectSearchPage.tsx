import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import type { ProspectCategory } from '../../api/prospects';
import {
  cancelImport,
  confirmImport,
  getImportRecords,
  searchApify,
  searchOpenStreetMap,
  type ImportConfirmResultDto,
  type ImportPreviewDto,
} from '../../api/imports';
import { searchCampaigns } from '../../api/campaigns';
import {
  cancelProspectAutomation,
  createProspectAutomation,
  listProspectAutomations,
  type ScheduledAutomationStatus,
} from '../../api/prospectAutomations';

// Único subconjunto de ProspectCategory con equivalente buscable en OpenStreetMap
// (OpenStreetMapCategories.Supported en el backend). Sin label compartido a propósito: sigue
// el mismo patrón que categoryLabels/statusLabels en ProspectsListPage (duplicado por página).
const categoryOptions: { value: ProspectCategory; label: string }[] = [
  { value: 'AutoPartsStore', label: 'Casa de repuestos' },
  { value: 'Workshop', label: 'Taller' },
  { value: 'Lubricentro', label: 'Lubricentro' },
  { value: 'TireShop', label: 'Gomería' },
  { value: 'Reseller', label: 'Concesionaria / reventa' },
];

// Búsqueda libre de rubro (texto arbitrario, resuelto por sinónimo o mandado tal cual como
// keyword): deshabilitada a pedido — el objetivo pasa a ser específicamente "Casa de repuestos" y
// "Mayorista (Suspensión/Tren delantero)", no cualquier rubro. El input libre, los sinónimos y el
// datalist quedan en el código (más abajo, detrás de este flag) por si se reactiva más adelante.
const FREE_TEXT_RUBRO_ENABLED = false;

// Sin tag propio en OSM ni en ProspectCategory: se busca como término libre (name~regex en OSM,
// texto plano en Apify) en vez de una categoría — mismo mecanismo que ya usa la búsqueda libre,
// solo que ahora se ofrece como una opción fija en vez de un input de texto abierto.
const WHOLESALE_SUSPENSION_KEYWORD = 'mayorista suspensión tren delantero';

// Localidades reales alcanzan a OSM/Apify igual que una provincia entera (Apify busca "{rubro} en
// {zona}, Argentina" sin distinguir si "zona" es una ciudad o una provincia completa), así que
// elegir una provincia acá solo agrega su nombre a la misma lista de "localities" — sin cambios de
// contrato ni de backend. Solo se ofrece con Apify: en modo administrativo de OSM, un área del
// tamaño de una provincia entera pisa el timeout de Overpass (ver DefaultKeywordRadiusKm en
// ImportService, mismo problema que ya resolvimos para partidos grandes, pero una provincia es
// varios órdenes de magnitud más grande).
const ARGENTINE_PROVINCES = [
  'Buenos Aires',
  'Catamarca',
  'Chaco',
  'Chubut',
  'Ciudad Autónoma de Buenos Aires',
  'Córdoba',
  'Corrientes',
  'Entre Ríos',
  'Formosa',
  'Jujuy',
  'La Pampa',
  'La Rioja',
  'Mendoza',
  'Misiones',
  'Neuquén',
  'Río Negro',
  'Salta',
  'San Juan',
  'San Luis',
  'Santa Cruz',
  'Santa Fe',
  'Santiago del Estero',
  'Tierra del Fuego',
  'Tucumán',
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

const automationStatusLabels: Record<ScheduledAutomationStatus, string> = {
  Pending: 'Programada',
  Running: 'Ejecutando',
  Completed: 'Completada',
  Failed: 'Falló',
  Cancelled: 'Cancelada',
};

const automationStatusClass: Record<ScheduledAutomationStatus, string> = {
  Pending: 'bg-indigo-50 dark:bg-indigo-500/10 text-indigo-700 dark:text-indigo-300',
  Running: 'bg-amber-50 dark:bg-amber-500/10 text-amber-700 dark:text-amber-300',
  Completed: 'bg-emerald-50 dark:bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
  Failed: 'bg-red-50 dark:bg-red-500/10 text-red-700 dark:text-red-300',
  Cancelled: 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400',
};

const inputClass =
  'mt-1 w-full rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 px-3 py-2 text-sm text-slate-900 dark:text-slate-100';

type SearchSource = 'osm' | 'apify';

export function ProspectSearchPage() {
  const [source, setSource] = useState<SearchSource>('osm');
  const [selectedCategories, setSelectedCategories] = useState<ProspectCategory[]>([]);
  const [keywords, setKeywords] = useState<string[]>([]);
  const [categoryInput, setCategoryInput] = useState('');
  const [localityInput, setLocalityInput] = useState('');
  const [provinceInput, setProvinceInput] = useState('');
  const [localities, setLocalities] = useState<string[]>([]);
  const [radiusKm, setRadiusKm] = useState(10);
  const [maxResults, setMaxResults] = useState(50);

  const [preview, setPreview] = useState<ImportPreviewDto | null>(null);
  const [batchId, setBatchId] = useState<number | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const selectAllRef = useRef<HTMLInputElement>(null);
  const [lastResult, setLastResult] = useState<ImportConfirmResultDto | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);

  const onSearchSuccess = (result: ImportPreviewDto) => {
    setLastResult(null);
    setPreview(result);
    setBatchId(result.batchId);
  };

  const searchMutation = useMutation({
    mutationFn: searchOpenStreetMap,
    onSuccess: onSearchSuccess,
  });

  // Apify no tiene el problema de timeout de Overpass (es un servicio pago con su propio límite
  // de 300s del lado de Apify, no un servidor público bajo carga), pero igual puede tardar según
  // cuántas combinaciones rubro×localidad se pidan.
  const apifyMutation = useMutation({
    mutationFn: searchApify,
    onSuccess: onSearchSuccess,
  });

  const activeMutation = source === 'osm' ? searchMutation : apifyMutation;

  // Overpass (el servidor público de OSM) puede tardar bastante bajo carga (ver
  // OpenStreetMapClient.PostWithRetryAsync — reintenta con backoff en 429/504), así que un
  // simple "Buscando…" no alcanza para saber si sigue vivo. El contador de segundos es la señal
  // más simple de "esto sigue corriendo, no se colgó". Se reusa igual para Apify: aunque no tenga
  // el mismo problema de carga pública, la señal de "sigue corriendo" sirve igual.
  useEffect(() => {
    if (!activeMutation.isPending) {
      setElapsedSeconds(0);
      return;
    }
    const interval = setInterval(() => setElapsedSeconds((s) => s + 1), 1000);
    return () => clearInterval(interval);
  }, [activeMutation.isPending]);

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

  const queryClient = useQueryClient();
  const [scheduleCampaignId, setScheduleCampaignId] = useState('');
  const [scheduleAt, setScheduleAt] = useState('');

  const campaignsQuery = useQuery({
    queryKey: ['campaigns', 'schedulable'],
    queryFn: () => searchCampaigns({ pageSize: 100 }),
  });
  // Solo campañas en un estado editable pueden recibir nuevos destinatarios y arrancar
  // (ver validación equivalente en ScheduledProspectAutomationService.CreateAsync).
  const schedulableCampaigns = (campaignsQuery.data?.items ?? []).filter((c) =>
    ['Draft', 'Ready', 'Paused'].includes(c.status),
  );

  const automationsQuery = useQuery({
    queryKey: ['prospect-automations'],
    queryFn: listProspectAutomations,
  });

  const scheduleMutation = useMutation({
    mutationFn: createProspectAutomation,
    onSuccess: () => {
      setScheduleAt('');
      setScheduleCampaignId('');
      queryClient.invalidateQueries({ queryKey: ['prospect-automations'] });
    },
  });

  const cancelAutomationMutation = useMutation({
    mutationFn: cancelProspectAutomation,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['prospect-automations'] }),
  });

  function handleSchedule(event: FormEvent) {
    event.preventDefault();
    if (localities.length === 0 || !hasRubroSelected || !scheduleCampaignId || !scheduleAt) return;

    scheduleMutation.mutate({
      localities,
      categories: selectedCategories.length > 0 ? selectedCategories : undefined,
      keywords: keywords.length > 0 ? keywords : undefined,
      radiusKm,
      maxResults,
      campaignId: Number(scheduleCampaignId),
      scheduledAt: new Date(scheduleAt).toISOString(),
    });
  }

  function resetSearch() {
    setBatchId(null);
    setPreview(null);
    setSelectedIds(new Set());
  }

  // Si el texto matchea uno de los 5 rubros automotrices conocidos, se agrega como categoría
  // (búsqueda eficiente por tag exacto de OSM). Si no, se agrega como término libre: se busca por
  // coincidencia en el nombre del comercio, sin restringirse a esos 5 rubros.
  function addCategory() {
    const trimmed = categoryInput.trim();
    if (!trimmed) return;

    const resolved = resolveCategoryFromText(trimmed);
    if (resolved) {
      setSelectedCategories((prev) => (prev.includes(resolved) ? prev : [...prev, resolved]));
    } else {
      setKeywords((prev) => (prev.includes(trimmed) ? prev : [...prev, trimmed]));
    }
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

  function removeKeyword(keyword: string) {
    setKeywords((prev) => prev.filter((k) => k !== keyword));
  }

  function toggleAutoPartsStore() {
    setSelectedCategories((prev) =>
      prev.includes('AutoPartsStore') ? prev.filter((c) => c !== 'AutoPartsStore') : [...prev, 'AutoPartsStore'],
    );
  }

  function toggleWholesaleSuspension() {
    setKeywords((prev) =>
      prev.includes(WHOLESALE_SUSPENSION_KEYWORD)
        ? prev.filter((k) => k !== WHOLESALE_SUSPENSION_KEYWORD)
        : [...prev, WHOLESALE_SUSPENSION_KEYWORD],
    );
  }

  function addLocality() {
    const trimmed = localityInput.trim();
    if (trimmed && !localities.includes(trimmed) && localities.length < MAX_LOCALITIES) {
      setLocalities((prev) => [...prev, trimmed]);
    }
    setLocalityInput('');
  }

  function addProvince() {
    if (provinceInput && !localities.includes(provinceInput) && localities.length < MAX_LOCALITIES) {
      setLocalities((prev) => [...prev, provinceInput]);
    }
    setProvinceInput('');
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

  function toggleAll(selectableIds: number[]) {
    setSelectedIds((prev) => {
      const allSelected = selectableIds.length > 0 && selectableIds.every((id) => prev.has(id));
      return allSelected ? new Set() : new Set(selectableIds);
    });
  }

  // Apify no distingue "categoría conocida" de "rubro libre" (busca todo por texto contra Google
  // Maps), así que para esa fuente los chips de categoría se mandan por su label en español igual
  // que los de keyword, en vez de resolverse a ProspectCategory.
  const apifyRubroTerms = [
    ...selectedCategories.map((c) => categoryLabelByValue.get(c) ?? c),
    ...keywords,
  ];

  // Con las opciones de rubro fijas (Casa de repuestos / Mayorista suspensión-tren delantero) ya
  // no tiene sentido el default "sin selección = todos los rubros" que tenía OSM: acá se exige
  // elegir al menos una de las dos, en las dos fuentes, para que la búsqueda nunca quede abierta.
  const hasRubroSelected = selectedCategories.length > 0 || keywords.length > 0;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (localities.length === 0 || !hasRubroSelected) return;

    if (source === 'osm') {
      searchMutation.mutate({
        localities,
        categories: selectedCategories.length > 0 ? selectedCategories : undefined,
        keywords: keywords.length > 0 ? keywords : undefined,
        radiusKm,
        maxResults,
      });
    } else {
      apifyMutation.mutate({ localities, keywords: apifyRubroTerms, maxResults });
    }
  }

  const records = recordsQuery.data ?? [];
  const selectableRecords = records.filter((r) => r.status === 'Valid');
  const allSelected = selectableRecords.length > 0 && selectableRecords.every((r) => selectedIds.has(r.id));
  const someSelected = selectedIds.size > 0 && !allSelected;

  useEffect(() => {
    if (selectAllRef.current) selectAllRef.current.indeterminate = someSelected;
  }, [someSelected]);

  return (
    <div className="max-w-4xl space-y-4">
      <div>
        <Link to="/app/prospects" className="text-sm text-indigo-600 dark:text-indigo-400 hover:underline">
          ← Volver a prospectos
        </Link>
        <h2 className="mt-2 text-lg font-semibold text-slate-900 dark:text-slate-100">Buscar prospectos</h2>
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
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Fuente</label>
          <div className="mt-1 flex gap-2">
            <button
              type="button"
              onClick={() => setSource('osm')}
              className={`rounded-md border px-3 py-2 text-sm font-medium ${
                source === 'osm'
                  ? 'border-indigo-600 bg-indigo-50 text-indigo-700 dark:border-indigo-500 dark:bg-indigo-500/10 dark:text-indigo-300'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
              }`}
            >
              OpenStreetMap
            </button>
            <button
              type="button"
              onClick={() => setSource('apify')}
              className={`rounded-md border px-3 py-2 text-sm font-medium ${
                source === 'apify'
                  ? 'border-indigo-600 bg-indigo-50 text-indigo-700 dark:border-indigo-500 dark:bg-indigo-500/10 dark:text-indigo-300'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
              }`}
            >
              Google Maps (Apify)
            </button>
          </div>
          <p className="mt-1 text-xs text-slate-400">
            {source === 'osm'
              ? 'Gratis. "Casa de repuestos" busca por tag exacto de OpenStreetMap; "Mayorista" busca por coincidencia de nombre, así que puede no encontrar el negocio si no está bien cargado en OSM.'
              : 'Servicio pago (Apify, ~USD 1.50 cada 1000 resultados). Busca directo en Google Maps, igual que buscarlo a mano — más cobertura para el rubro de mayoristas.'}
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Rubro</label>
          <div className="mt-1 flex flex-wrap gap-2">
            <button
              type="button"
              onClick={toggleAutoPartsStore}
              className={`rounded-md border px-3 py-2 text-sm font-medium ${
                selectedCategories.includes('AutoPartsStore')
                  ? 'border-indigo-600 bg-indigo-50 text-indigo-700 dark:border-indigo-500 dark:bg-indigo-500/10 dark:text-indigo-300'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
              }`}
            >
              Casa de repuestos
            </button>
            <button
              type="button"
              onClick={toggleWholesaleSuspension}
              className={`rounded-md border px-3 py-2 text-sm font-medium ${
                keywords.includes(WHOLESALE_SUSPENSION_KEYWORD)
                  ? 'border-indigo-600 bg-indigo-50 text-indigo-700 dark:border-indigo-500 dark:bg-indigo-500/10 dark:text-indigo-300'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
              }`}
            >
              Mayorista (Suspensión / Tren delantero)
            </button>
          </div>
          {FREE_TEXT_RUBRO_ENABLED && (
            <div className="mt-2 flex gap-2">
              <input
                type="text"
                value={categoryInput}
                onChange={(e) => setCategoryInput(e.target.value)}
                onKeyDown={handleCategoryKeyDown}
                list="category-suggestions"
                placeholder="ej. gomería, taller, lubricentro, o cualquier otro rubro"
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
          )}
          {(selectedCategories.length > 0 || keywords.length > 0) && (
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
              {keywords.map((keyword) => (
                <span
                  key={keyword}
                  className="flex items-center gap-1 rounded-full bg-slate-100 dark:bg-slate-800 px-3 py-1 text-xs font-medium text-slate-600 dark:text-slate-300"
                  title="Búsqueda libre por nombre de negocio"
                >
                  {keyword}
                  <button
                    type="button"
                    onClick={() => removeKeyword(keyword)}
                    className="text-slate-400 hover:text-slate-700 dark:hover:text-slate-100"
                    aria-label={`Quitar ${keyword}`}
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
          <p className="mt-1 text-xs text-slate-400">Elegí al menos uno de los dos rubros para poder buscar.</p>
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
          {source === 'apify' && (
            <div className="mt-2 flex gap-2">
              <select
                value={provinceInput}
                onChange={(e) => setProvinceInput(e.target.value)}
                disabled={localities.length >= MAX_LOCALITIES}
                className={inputClass}
              >
                <option value="">O elegí una provincia entera…</option>
                {ARGENTINE_PROVINCES.map((province) => (
                  <option key={province} value={province}>
                    {province}
                  </option>
                ))}
              </select>
              <button
                type="button"
                onClick={addProvince}
                disabled={!provinceInput || localities.length >= MAX_LOCALITIES}
                className="mt-1 shrink-0 rounded-md border border-slate-300 dark:border-slate-700 px-3 py-2 text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40"
              >
                Agregar
              </button>
            </div>
          )}
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
          {source === 'osm' && (
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
          )}

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

        {activeMutation.isError && (
          <p className="text-sm text-red-600 dark:text-red-400">
            {activeMutation.error instanceof Error ? activeMutation.error.message : 'Ocurrió un error inesperado.'}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={activeMutation.isPending || localities.length === 0 || !hasRubroSelected}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {activeMutation.isPending ? `Buscando… (${elapsedSeconds}s)` : 'Buscar'}
          </button>
          {activeMutation.isPending && (
            <p className="text-xs text-slate-400">
              {source === 'osm'
                ? 'Puede tardar hasta 1 minuto según la carga del servidor público de OpenStreetMap. Sigue en curso, no cierres esta pestaña.'
                : 'Puede tardar según cuántas combinaciones de rubro y zona se estén buscando. Sigue en curso, no cierres esta pestaña.'}
            </p>
          )}
        </div>
      </form>

      <div className="space-y-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5">
        <div>
          <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Programar automatización</h3>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            Elegí una fecha/hora: en ese momento el sistema busca los prospectos con el rubro y zonas de arriba, los
            guarda automáticamente y envía la campaña seleccionada sin revisión manual. Siempre busca por
            OpenStreetMap (gratis) — el selector de fuente de arriba solo aplica a la búsqueda manual.
          </p>
        </div>

        <form onSubmit={handleSchedule} className="flex flex-wrap items-end gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Campaña</label>
            <select
              value={scheduleCampaignId}
              onChange={(e) => setScheduleCampaignId(e.target.value)}
              className={`${inputClass} w-56`}
            >
              <option value="">Seleccionar…</option>
              {schedulableCampaigns.map((campaign) => (
                <option key={campaign.id} value={campaign.id}>
                  {campaign.name}
                </option>
              ))}
            </select>
            {campaignsQuery.data && schedulableCampaigns.length === 0 && (
              <p className="mt-1 text-xs text-amber-600 dark:text-amber-400">
                No hay campañas disponibles para programar (deben estar en Borrador, Lista o Pausada).
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300">Fecha y hora</label>
            <input
              type="datetime-local"
              value={scheduleAt}
              onChange={(e) => setScheduleAt(e.target.value)}
              className={`${inputClass} w-56`}
            />
          </div>

          <button
            type="submit"
            disabled={
              scheduleMutation.isPending ||
              localities.length === 0 ||
              !hasRubroSelected ||
              !scheduleCampaignId ||
              !scheduleAt
            }
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {scheduleMutation.isPending ? 'Programando…' : 'Programar'}
          </button>
        </form>

        {scheduleMutation.isError && (
          <p className="text-sm text-red-600 dark:text-red-400">
            {scheduleMutation.error instanceof Error ? scheduleMutation.error.message : 'No se pudo programar la automatización.'}
          </p>
        )}

        {automationsQuery.data && automationsQuery.data.length > 0 && (
          <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-800 text-sm">
              <thead className="bg-slate-50 dark:bg-slate-900">
                <tr>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Zonas</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Campaña</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Programada</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Estado</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">Resultado</th>
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-950">
                {automationsQuery.data.map((automation) => (
                  <tr key={automation.id}>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{automation.localities.join(', ')}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{automation.campaignName}</td>
                    <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                      {new Date(automation.scheduledAt).toLocaleString()}
                    </td>
                    <td className="px-3 py-2">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${automationStatusClass[automation.status]}`}
                      >
                        {automationStatusLabels[automation.status]}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                      {automation.resultSummary ?? '—'}
                    </td>
                    <td className="px-3 py-2">
                      {automation.status === 'Pending' && (
                        <button
                          type="button"
                          onClick={() => cancelAutomationMutation.mutate(automation.id)}
                          disabled={cancelAutomationMutation.isPending}
                          className="text-xs font-medium text-red-600 dark:text-red-400 hover:underline disabled:opacity-60"
                        >
                          Cancelar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

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
                  <th className="px-3 py-2 text-left font-medium text-slate-500 dark:text-slate-400">
                    {selectableRecords.length > 0 && (
                      <input
                        ref={selectAllRef}
                        type="checkbox"
                        checked={allSelected}
                        onChange={() => toggleAll(selectableRecords.map((r) => r.id))}
                        aria-label="Seleccionar todos"
                        className="rounded border-slate-300 dark:border-slate-700"
                      />
                    )}
                  </th>
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
