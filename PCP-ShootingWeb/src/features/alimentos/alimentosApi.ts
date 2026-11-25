/*
 * Alimentos y bebidas · API real. Contrato: docs/ALIMENTOS.md
 * Carta pública (sin sesión):
 *   GET /api/alimentos?busqueda&pagina&tamanoPagina   activos y vendibles de categoría 1 (agotados visibles con agotado=true)
 * Gestión (Administrador y Recepción, solo categoría 1) con las rutas de productos existentes:
 *   GET   /api/administracion/productos?categoria=1    + GET /api/inventario (existencias)
 *   POST  /api/productos  |  PUT /api/productos/{id}   (categoria=1; el PUT reemplaza todo)
 *   PATCH /api/productos/{id}/estado { activo }        mostrar/ocultar en la carta
 *   POST  /api/productos/{id}/imagen                   foto (Storage en Azure pendiente → 503)
 *   POST  /api/inventario/{id}/movimientos             entradas
 * Pendiente OQ-05: pedido en línea, pago, preparación/entrega, subcategorías y horarios de cocina.
 */
import { apiGetAll, apiPost, apiPut, type WriteResult } from '@/lib/api';
import { parsePrice } from '@/features/admin/productosApi';

export type MenuItem = {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  imageUrl: string | null;
  notes: string | null;
  stock: number;
  soldOut: boolean;
};

type ApiMenuItem = { id: string; sku: string; nombre: string; descripcion: string | null; precio: number; imagenUrl: string | null; notasMarkdown: string | null; existencia: number; agotado: boolean };

export async function getPublicMenu(): Promise<MenuItem[]> {
  const list = await apiGetAll<ApiMenuItem>('/api/alimentos');
  return list.map((a) => ({
    id: a.id,
    sku: a.sku,
    name: a.nombre,
    description: a.descripcion,
    price: a.precio,
    imageUrl: a.imagenUrl,
    notes: a.notasMarkdown,
    stock: a.existencia,
    soldOut: a.agotado,
  }));
}

/* ---------- Gestión ---------- */

export type FoodProduct = {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: number | null;
  active: boolean;
  imageUrl: string | null;
  notes: string | null;
  stock: number;
  minStock: number | null;
  low: boolean;
};

export type FoodDraft = { id: string | null; sku: string; name: string; description: string; price: string; notes: string; imageUrl: string | null };

type ApiProduct = {
  id: string;
  sku: string;
  nombre: string;
  descripcion: string | null;
  precio: number | null;
  activo: boolean;
  categoria: number;
  imagenUrl: string | null;
  notasMarkdown: string | null;
  stockMinimo: number | null;
};
type ApiStock = { productoId: string; existencia: number; stockMinimo: number | null; bajoMinimo: boolean };

export async function listFoodProducts(): Promise<FoodProduct[]> {
  const [products, stock] = await Promise.all([
    apiGetAll<ApiProduct>('/api/administracion/productos', { categoria: 1 }),
    apiGetAll<ApiStock>('/api/inventario'),
  ]);
  const byId = new Map(stock.map((s) => [s.productoId, s]));
  return products
    .filter((p) => p.categoria === 1)
    .map((p) => {
      const s = byId.get(p.id);
      return {
        id: p.id,
        sku: p.sku,
        name: p.nombre,
        description: p.descripcion ?? '',
        price: p.precio,
        active: p.activo,
        imageUrl: p.imagenUrl,
        notes: p.notasMarkdown,
        stock: s?.existencia ?? 0,
        minStock: s?.stockMinimo ?? p.stockMinimo,
        low: s?.bajoMinimo ?? false,
      };
    })
    .sort((a, b) => Number(b.active) - Number(a.active) || a.name.localeCompare(b.name, 'es'));
}

export const emptyFood = (): FoodDraft => ({ id: null, sku: '', name: '', description: '', price: '', notes: '', imageUrl: null });
export const toFoodDraft = (p: FoodProduct): FoodDraft => ({
  id: p.id,
  sku: p.sku,
  name: p.name,
  description: p.description,
  price: p.price == null ? '' : String(p.price),
  notes: p.notes ?? '',
  imageUrl: p.imageUrl,
});

export function validateFood(d: FoodDraft): string | null {
  if (!d.name.trim()) return 'Escribe el nombre.';
  if (!/^\S{1,60}$/.test(d.sku.trim())) return 'Escribe el SKU sin espacios (hasta 60 caracteres, ej. ALI-AGUA-500).';
  if (parsePrice(d.price) == null) return 'Escribe un precio válido mayor a 0 (ej. 25 o 45.50).';
  return null;
}

/** Crea o actualiza un alimento (categoría 1, vendible). El PUT reemplaza todo: se reenvían foto y notas. */
export async function saveFood(d: FoodDraft): Promise<string> {
  const body = {
    sku: d.sku.trim(),
    nombre: d.name.trim(),
    descripcion: d.description.trim() || null,
    categoria: 1,
    imagenUrl: d.imageUrl,
    notasMarkdown: d.notes.trim() || null,
    esVendible: true,
    precio: parsePrice(d.price),
    especificacionesTecnicas: null,
    usaTallas: false,
  };
  const r = d.id ? await apiPut<WriteResult>(`/api/productos/${d.id}`, body) : await apiPost<WriteResult>('/api/productos', body);
  return r.id;
}
