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
import { apiGet, apiGetAll, apiPost, apiPut, type WriteResult } from '@/lib/api';

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
    }))
    .sort((a, b) => Number(b.low) - Number(a.low) || a.name.localeCompare(b.name, 'es'));
}

export type MovementKind = 0 | 1 | 2;
export const MOVEMENT_LABEL: Record<MovementKind, string> = { 0: 'Entrada', 1: 'Salida', 2: 'Ajuste' };

export type Movement = { id: string; at: string; type: string; qty: number; reason: string | null };

type ApiMovement = { id: string; registradoEnUtc: string; tipo: string; variacionCantidad: number; motivo: string | null };

export async function listMovements(id: string): Promise<Movement[]> {
  const list = await apiGet<ApiMovement[]>(`/api/inventario/${id}/movimientos`);
  return list
    .map((m) => ({ id: m.id, at: m.registradoEnUtc, type: m.tipo, qty: m.variacionCantidad, reason: m.motivo }))
    .sort((a, b) => b.at.localeCompare(a.at));
}

/** Registra un movimiento. `quantity` siempre positiva para entrada/salida; con signo para ajuste. */
export function registerMovement(id: string, kind: MovementKind, quantity: number, reason: string, operationId: string) {
  const variacionCantidad = kind === 1 ? -Math.abs(quantity) : kind === 0 ? Math.abs(quantity) : quantity;
  return apiPost<WriteResult>(`/api/inventario/${id}/movimientos`, { idOperacion: operationId, tipo: kind, variacionCantidad, motivo: reason.trim() });
}

export const setMinimum = (id: string, min: number) => apiPut<WriteResult>(`/api/inventario/${id}/minimo`, { stockMinimo: min });

/** Tipo de movimiento de la API ("Entrada"/"Salida"/"Ajuste" o número) → etiqueta. */
export function movementLabel(type: string): string {
  const n = Number(type);
  if (!Number.isNaN(n) && n in MOVEMENT_LABEL) return MOVEMENT_LABEL[n as MovementKind];
  return type;
}
