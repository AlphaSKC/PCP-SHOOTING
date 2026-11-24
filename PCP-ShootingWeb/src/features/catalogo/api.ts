/*
 * Catálogo de armas (informativo, sin venta en línea) · categoría 3 de la API.
 *   Público → GET /api/productos?categoria=3   (solo activos)
 *   Panel   → GET /api/administracion/productos?categoria=3, POST/PUT /api/productos,
 *             PATCH /api/productos/{id}/estado, POST /api/productos/{id}/imagen
 * Siempre esVendible = false. precio null = "Por cotizar".
 * Los datos del rifle que no tienen columna propia viven en especificacionesTecnicas (objeto JSON):
 *   { tipo, calibres, modoDisparo, destaca, resumen, ficha: [{ etiqueta, valor }] }
 */
import { rifles as demoRifles, type Rifle } from '@/data/rifles';
import { DATA_SOURCE, apiGetAll, apiPost, apiPut, type WriteResult } from '@/lib/api';

export const RIFLE_CATEGORY = 3;

type ApiCatalogItem = {
  id: string;
  sku: string;
  nombre: string;
  descripcion: string | null;
  precio: number | null;
  activo?: boolean;
  imagenUrl: string | null;
  notasMarkdown: string | null;
  especificacionesTecnicas: unknown;
};

type RifleSpecs = {
  tipo?: string;
  calibres?: string;
  modoDisparo?: string | null;
  destaca?: string;
  resumen?: string;
  ficha?: { etiqueta: string; valor: string }[];
  [k: string]: unknown;
};

const str = (v: unknown) => (typeof v === 'string' ? v.trim() : '');

function readSpecs(raw: unknown): RifleSpecs {
  return raw && typeof raw === 'object' && !Array.isArray(raw) ? (raw as RifleSpecs) : {};
}

function readSheet(s: RifleSpecs) {
  return Array.isArray(s.ficha)
    ? s.ficha
        .map((f) => ({ label: str(f?.etiqueta), value: str(f?.valor) }))
        .filter((f) => f.label && f.value)
    : [];
}

export function toRifle(p: ApiCatalogItem): Rifle {
  const s = readSpecs(p.especificacionesTecnicas);
  const description = p.descripcion?.trim() ?? '';
  return {
    id: p.id,
    name: p.nombre,
    type: str(s.tipo) || 'Rifle PCP',
    description,
    short: str(s.resumen) || description,
    calibers: str(s.calibres) || 'Consultar',
    specs: readSheet(s),
    firingMode: str(s.modoDisparo) || null,
    highlight: str(s.destaca) || '—',
    price: p.precio,
    imageUrl: p.imagenUrl,
  };
}

/** Modelos visibles para la página pública /catalogo. */
export async function getRifles(): Promise<Rifle[]> {
  if (DATA_SOURCE !== 'api') return demoRifles;
  const items = await apiGetAll<ApiCatalogItem>('/api/productos', { categoria: RIFLE_CATEGORY });
  return items.map(toRifle);
}

/* ---------- Panel ---------- */

export type AdminRifleApi = {
  id: string;
  sku: string;
  name: string;
  description: string;
  short: string;
  type: string;
  calibers: string;
  firingMode: string;
  highlight: string;
  sheet: { label: string; value: string }[];
  price: number | null;
  active: boolean;
  imageUrl: string | null;
  notes: string | null;
  /** Especificaciones originales: se conservan las claves que el panel no edita. */
  rawSpecs: RifleSpecs;
};

export async function listAdminRifles(): Promise<AdminRifleApi[]> {
  const items = await apiGetAll<ApiCatalogItem>('/api/administracion/productos', { categoria: RIFLE_CATEGORY });
  return items
    .map((p) => {
      const s = readSpecs(p.especificacionesTecnicas);
      return {
        id: p.id,
        sku: p.sku,
        name: p.nombre,
        description: p.descripcion ?? '',
        short: str(s.resumen),
        type: str(s.tipo),
        calibers: str(s.calibres),
        firingMode: str(s.modoDisparo),
        highlight: str(s.destaca),
        sheet: readSheet(s),
        price: p.precio,
        active: p.activo ?? true,
        imageUrl: p.imagenUrl,
        notes: p.notasMarkdown,
        rawSpecs: s,
      };
    })
    .sort((a, b) => a.name.localeCompare(b.name, 'es'));
}

export type RifleDraft = Omit<AdminRifleApi, 'id' | 'active' | 'price'> & { id: string | null; price: string };

/** Precio opcional: vacío = null ("Por cotizar"); si no, número ≥ 0 con hasta 2 decimales. undefined = inválido. */
export function parseOptionalPrice(text: string): number | null | undefined {
  const t = text.trim().replace(',', '.');
  if (!t) return null;
  return /^\d+(\.\d{1,2})?$/.test(t) ? Number(t) : undefined;
}

/** Crea o actualiza. El PUT reemplaza todo: se reenvían foto, notas y claves de especificaciones no editadas. */
export async function saveRifle(d: RifleDraft): Promise<string> {
  const specs: RifleSpecs = {
    ...d.rawSpecs,
    tipo: d.type.trim(),
    calibres: d.calibers.trim(),
    modoDisparo: d.firingMode.trim() || null,
    destaca: d.highlight.trim(),
    resumen: d.short.trim(),
    ficha: d.sheet
      .map((f) => ({ etiqueta: f.label.trim(), valor: f.value.trim() }))
      .filter((f) => f.etiqueta && f.valor),
  };
  const body = {
    sku: d.sku.trim(),
    nombre: d.name.trim(),
    descripcion: d.description.trim() || null,
    categoria: RIFLE_CATEGORY,
    imagenUrl: d.imageUrl,
    notasMarkdown: d.notes,
    esVendible: false,
    precio: parseOptionalPrice(d.price) ?? null,
    especificacionesTecnicas: specs,
  };
  const r = d.id ? await apiPut<WriteResult>(`/api/productos/${d.id}`, body) : await apiPost<WriteResult>('/api/productos', body);
  return r.id;
}
