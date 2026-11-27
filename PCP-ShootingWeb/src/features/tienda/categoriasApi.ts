/*
 * Tienda por categorías y marcas · F24 (contrato en docs/ROADMAP.md, sección «F24»). PRIORIDAD 1 del backend.
 *   GET    /api/tienda/categorias                              público: activas con `cantidadProductos`
 *   GET    /api/administracion/tienda/categorias               Administrador: todas, con `activa` y `version`
 *   POST   /api/administracion/tienda/categorias               { nombre, slug, descripcion, orden, activa }
 *   PUT    /api/administracion/tienda/categorias/{id}          mismo cuerpo + version (409 = recargar)
 *   POST   /api/administracion/tienda/categorias/{id}/imagen   multipart: archivo + idOperacion → { imagenUrl, version }
 *   DELETE /api/administracion/tienda/categorias/{id}          409 si tiene productos
 * Productos: `categoriaTienda` y `marca` en el catálogo; `categoriaTiendaId`/`marcaId` en gestión y al guardar.
 * Mientras la ruta no exista (404/405) todo devuelve `null` y la tienda se ve como antes (sin datos inventados).
 */
import { ApiError, DATA_SOURCE, apiDelete, apiGet, apiPost, apiPut, apiUpload } from '@/lib/api';

/** Rutas del sitio que no pueden usarse como categoría (/tienda/carrito, /tienda/producto/…). */
export const RESERVED_SLUGS = ['carrito', 'checkout', 'pedido', 'producto'];

export type StoreCategory = {
  id: string;
  name: string;
  slug: string;
  description: string;
  imageUrl: string | null;
  order: number;
  /** Productos a la venta en la categoría (público) o asignados (panel). */
  count: number;
};

export type AdminStoreCategory = StoreCategory & { active: boolean; version: string | null };

type ApiCategory = {
  id: string;
  nombre: string;
  slug: string;
  descripcion: string | null;
  imagenUrl: string | null;
  orden: number;
  cantidadProductos?: number;
  activa?: boolean;
  version?: string;
};

const missing = (e: unknown) => e instanceof ApiError && e.missingRoute;

const toCategory = (c: ApiCategory): AdminStoreCategory => ({
  id: c.id,
  name: c.nombre,
  slug: c.slug,
  description: c.descripcion ?? '',
  imageUrl: c.imagenUrl,
  order: c.orden,
  count: c.cantidadProductos ?? 0,
  active: c.activa ?? true,
  version: c.version ?? null,
});

const byOrder = (a: StoreCategory, b: StoreCategory) => a.order - b.order || a.name.localeCompare(b.name, 'es');

let publicCache: StoreCategory[] | null | undefined;

/** Categorías públicas. `null` = el backend aún no tiene F24 (o modo demo): la tienda se muestra como lista única. */
export async function getStoreCategories(): Promise<StoreCategory[] | null> {
  if (DATA_SOURCE !== 'api') return null;
  try {
    const list = await apiGet<ApiCategory[]>('/api/tienda/categorias');
    publicCache = list.map(toCategory).sort(byOrder);
  } catch (e) {
    if (!missing(e)) throw e;
    publicCache = null;
  }
  return publicCache;
}

/** Última lista pública cargada (para pintar al instante al navegar). */
export const cachedStoreCategories = () => publicCache;

/* ---------- Panel ---------- */

/** Todas las categorías para el panel. `null` = F24 pendiente de backend. */
export async function listAdminCategories(): Promise<AdminStoreCategory[] | null> {
  try {
    const list = await apiGet<ApiCategory[]>('/api/administracion/tienda/categorias');
    return list.map(toCategory).sort(byOrder);
  } catch (e) {
    if (missing(e)) return null;
    throw e;
  }
}

export const emptyCategory = (order: number): AdminStoreCategory => ({
  id: '',
  name: '',
  slug: '',
  description: '',
  imageUrl: null,
  order,
  count: 0,
  active: true,
  version: null,
});

/** «Rifles PCP» → «rifles-pcp». */
export function slugify(text: string): string {
  return text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/&/g, ' y ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 60);
}

export function validateCategory(c: AdminStoreCategory, all: AdminStoreCategory[]): string | null {
  if (!c.name.trim()) return 'Escribe el nombre de la categoría.';
  if (c.name.trim().length > 60) return 'El nombre admite hasta 60 caracteres.';
  const slug = c.slug.trim();
  if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(slug)) return 'La dirección web solo admite minúsculas, números y guiones (ej. municiones).';
  if (RESERVED_SLUGS.includes(slug)) return `«${slug}» está reservada por el sitio. Usa otra dirección web.`;
  if (all.some((o) => o.slug === slug && o.id !== c.id)) return 'Ya hay otra categoría con esa dirección web.';
  if (c.description.trim().length > 300) return 'La descripción admite hasta 300 caracteres.';
  if (!Number.isInteger(c.order) || c.order < 0) return 'El orden debe ser un número entero de 0 en adelante.';
  return null;
}

export async function saveCategory(c: AdminStoreCategory): Promise<AdminStoreCategory> {
  const body = { nombre: c.name.trim(), slug: c.slug.trim(), descripcion: c.description.trim() || null, orden: c.order, activa: c.active };
  const r = c.id
    ? await apiPut<ApiCategory>(`/api/administracion/tienda/categorias/${c.id}`, { ...body, version: c.version })
    : await apiPost<ApiCategory>('/api/administracion/tienda/categorias', body);
  return toCategory(r);
}

export async function uploadCategoryImage(id: string, file: File, operationId: string): Promise<{ imageUrl: string; version: string }> {
  const form = new FormData();
  form.append('archivo', file);
  form.append('idOperacion', operationId);
  const r = await apiUpload<{ imagenUrl: string; version: string }>(`/api/administracion/tienda/categorias/${id}/imagen`, form);
  return { imageUrl: r.imagenUrl, version: r.version };
}

export const deleteCategory = (id: string) => apiDelete(`/api/administracion/tienda/categorias/${id}`);

export const CATEGORY_IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';
export const CATEGORY_IMAGE_MAX_BYTES = 5 * 1024 * 1024;
