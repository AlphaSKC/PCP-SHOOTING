/*
 * Inventario · API real. Administrador (todo) y Recepción (solo alimentos; la API lo filtra).
 *   GET  /api/inventario                         existencias de todos los productos (incluye inactivos)
 *   GET  /api/inventario/{id}/movimientos        historial del artículo
 *   POST /api/inventario/{id}/movimientos        { idOperacion, tipo: 0 Entrada(+) | 1 Salida(−) | 2 Ajuste(±), variacionCantidad, motivo }
 *   PUT  /api/inventario/{id}/minimo             { stockMinimo }
 *   PATCH /api/productos/{id}/estado             { activo }   (activar/desactivar artículo)
 *   POST /api/productos                          crear consumible interno (categoría 4, no vendible)
 * La API impide existencias negativas (409). No hay historial global: el historial es por artículo.
 */
import { apiGet, apiGetAll, apiPost, apiPut, type Page, type WriteResult } from '@/lib/api';

export type CategoryId = 0 | 1 | 2 | 3 | 4;
export const CATEGORY_LABEL: Record<CategoryId, string> = {
  0: 'Artículo de tienda',
  1: 'Alimento / bebida',
  2: 'Playera',
  3: 'Catálogo de armas',
  4: 'Consumible',
};

export type StockItem = {
  id: string;
  sku: string;
  name: string;
  category: CategoryId;
  active: boolean;
  sellable: boolean;
  imageUrl: string | null;
  stock: number;
  min: number | null;
  low: boolean;
  /** Playera con existencias por talla (F09). */
  usesSizes: boolean;
};

type ApiStock = {
  productoId: string;
  sku: string;
  nombre: string;
  categoria: CategoryId;
  activo: boolean;
  esVendible: boolean;
  imagenUrl: string | null;
  existencia: number;
  stockMinimo: number | null;
  bajoMinimo: boolean;
  usaTallas?: boolean;
};

export async function listStock(): Promise<StockItem[]> {
  const list = await apiGetAll<ApiStock>('/api/inventario');
  return list
    .filter((p) => p.categoria !== 3) // el catálogo de armas es informativo: no lleva existencias
    .map((p) => ({
      id: p.productoId,
      sku: p.sku,
      name: p.nombre,
      category: p.categoria,
      active: p.activo,
      sellable: p.esVendible,
      imageUrl: p.imagenUrl,
      stock: p.existencia,
      min: p.stockMinimo,
      low: p.bajoMinimo,
      usesSizes: Boolean(p.usaTallas),
    }))
    .sort((a, b) => Number(b.low) - Number(a.low) || a.name.localeCompare(b.name, 'es'));
}

export type MovementKind = 0 | 1 | 2;
export const MOVEMENT_LABEL: Record<MovementKind, string> = { 0: 'Entrada', 1: 'Salida', 2: 'Ajuste' };

export type Movement = {
  id: string;
  at: string;
  type: string;
  qty: number;
  reason: string | null;
  size: string | null;
  /** F13: artículo y autor (nombre actual del perfil; null si no se conoce). */
  productId: string | null;
  productName: string | null;
  sku: string | null;
  actorName: string | null;
};

type ApiMovement = {
  id: string;
  registradoEnUtc: string;
  tipo: string | number;
  variacionCantidad: number;
  motivo: string | null;
  talla?: string | number | null;
  productoId?: string | null;
  sku?: string | null;
  nombreProducto?: string | null;
  actorId?: string | null;
  actorNombre?: string | null;
};

const SIZE_NAMES = ['S', 'M', 'L', 'XL'];
const toMovement = (m: ApiMovement): Movement => ({
  id: m.id,
  at: m.registradoEnUtc,
  type: String(m.tipo),
  qty: m.variacionCantidad,
  reason: m.motivo,
  size: m.talla == null ? null : typeof m.talla === 'number' ? SIZE_NAMES[m.talla] ?? String(m.talla) : m.talla,
  productId: m.productoId ?? null,
  productName: m.nombreProducto ?? null,
  sku: m.sku ?? null,
  actorName: m.actorNombre ?? null,
});

export async function listMovements(id: string): Promise<Movement[]> {
  const list = await apiGet<ApiMovement[]>(`/api/inventario/${id}/movimientos`);
  return list.map(toMovement).sort((a, b) => b.at.localeCompare(a.at));
}

export type MovementFilters = { search: string; kind: '' | MovementKind; from: string; to: string };

/** Día local del club → instante UTC (inicio del día). */
const dayStartUtc = (d: string) => new Date(`${d}T00:00:00-05:00`).toISOString();
const nextDay = (d: string) => {
  const x = new Date(`${d}T12:00:00Z`);
  x.setUTCDate(x.getUTCDate() + 1);
  return x.toISOString().slice(0, 10);
};

/** Historial global (F13, solo Administrador): paginado, más recientes primero. Fechas = días del club. */
export async function listAllMovements(f: MovementFilters, pagina: number, tamanoPagina = 25): Promise<Page<Movement>> {
  const page = await apiGet<Page<ApiMovement>>('/api/inventario/movimientos', {
    pagina,
    tamanoPagina,
    busqueda: f.search.trim() || undefined,
    tipo: f.kind === '' ? undefined : f.kind,
    desdeUtc: f.from ? dayStartUtc(f.from) : undefined,
    hastaUtc: f.to ? dayStartUtc(nextDay(f.to)) : undefined,
  });
  return { ...page, elementos: page.elementos.map(toMovement) };
}

/** Registra un movimiento. `quantity` siempre positiva para entrada/salida; con signo para ajuste. */
export function registerMovement(id: string, kind: MovementKind, quantity: number, reason: string, operationId: string, size?: string | null) {
  const variacionCantidad = kind === 1 ? -Math.abs(quantity) : kind === 0 ? Math.abs(quantity) : quantity;
  return apiPost<WriteResult>(`/api/inventario/${id}/movimientos`, { idOperacion: operationId, tipo: kind, variacionCantidad, motivo: reason.trim(), talla: size ?? null });
}

/** Existencias por talla de una playera con tallas (S, M, L, XL). */
export async function getSizeStock(id: string): Promise<{ size: string; stock: number }[]> {
  const list = await apiGet<{ talla: string; existencia: number }[]>(`/api/inventario/${id}/tallas`);
  return list.map((t) => ({ size: t.talla, stock: t.existencia }));
}

export const setMinimum = (id: string, min: number) => apiPut<WriteResult>(`/api/inventario/${id}/minimo`, { stockMinimo: min });

/** Tipo de movimiento de la API ("Entrada"/"Salida"/"Ajuste" o número) → etiqueta. */
export function movementLabel(type: string): string {
  const n = Number(type);
  if (!Number.isNaN(n) && n in MOVEMENT_LABEL) return MOVEMENT_LABEL[n as MovementKind];
  return type;
}
