/*
 * Marcas aliadas ("Marcas que nos acompañan"). CONTRATO PROPUESTO al backend (docs/ROADMAP.md §8, F17):
 *   GET    /api/marcas                                  público: activas, ordenadas por `orden`
 *   GET    /api/administracion/marcas                   Administrador: todas, con `activo` y `version`
 *   POST   /api/administracion/marcas                   { nombre, descripcion, url, activo, orden }
 *   PUT    /api/administracion/marcas/{id}              mismo cuerpo + version (409 = otra edición)
 *   POST   /api/administracion/marcas/{id}/logo         multipart: archivo + idOperacion → { logoUrl }
 *   DELETE /api/administracion/marcas/{id}/logo         quita el logo (la marca se muestra como texto)
 * Mientras el endpoint no exista (404), la portada usa la lista fija de src/data/brands.ts y el panel muestra «pendiente».
 */
import { brands as staticBrands, type Brand } from '@/data/brands';
import { ApiError, DATA_SOURCE, apiDelete, apiGet, apiPost, apiPut, apiUpload } from '@/lib/api';

type ApiBrand = {
  id: string;
  nombre: string;
  descripcion: string | null;
  url: string | null;
  logoUrl: string | null;
  orden: number;
  activo?: boolean;
  version?: string;
  /** F24 (tienda por marcas). */
  slug?: string;
  visibleEnCarrusel?: boolean;
};

const toBrand = (b: ApiBrand): Brand => ({ name: b.nombre, tagline: b.descripcion ?? undefined, logo: b.logoUrl, url: b.url });

/** El endpoint aún no existe en el backend (ruta desconocida o método no permitido). */
export const isMissingEndpoint = (e: unknown) => e instanceof ApiError && (e.status === 404 || e.status === 405);

/** Marcas de la portada. Con la API usa /api/marcas; si el backend aún no lo tiene, la lista fija. */
export async function getBrands(): Promise<Brand[]> {
  if (DATA_SOURCE !== 'api') return staticBrands;
  try {
    const list = await apiGet<ApiBrand[]>('/api/marcas');
    return [...list].sort((a, b) => a.orden - b.orden).map(toBrand);
  } catch (e) {
    if (isMissingEndpoint(e)) return staticBrands;
    throw e;
  }
}

/* ---------- Panel ---------- */

export type BrandDraft = {
  id: string | null;
  version: string | null;
  name: string;
  tagline: string;
  url: string;
  logoUrl: string | null;
  active: boolean;
  order: number;
  /** F24: dirección web de la marca en la tienda (/tienda/{categoria}/{slug}). */
  slug: string;
  /** F24: además de la tienda, sale en el carrusel de la portada. */
  carousel: boolean;
  /** El backend ya tiene F24 (la respuesta trae `slug`). */
  taxonomy: boolean;
};

export const emptyBrand = (order: number, taxonomy = false): BrandDraft => ({ id: null, version: null, name: '', tagline: '', url: '', logoUrl: null, active: true, order, slug: '', carousel: true, taxonomy });

const toDraft = (b: ApiBrand): BrandDraft => ({
  id: b.id,
  version: b.version ?? null,
  name: b.nombre,
  tagline: b.descripcion ?? '',
  url: b.url ?? '',
  logoUrl: b.logoUrl,
  active: b.activo ?? true,
  order: b.orden,
  slug: b.slug ?? '',
  carousel: b.visibleEnCarrusel ?? true,
  taxonomy: b.slug !== undefined,
});

export async function listAdminBrands(): Promise<BrandDraft[]> {
  const list = await apiGet<ApiBrand[]>('/api/administracion/marcas');
  return list.map(toDraft).sort((a, b) => a.order - b.order);
}

export function validateBrand(d: BrandDraft): string | null {
  if (!d.name.trim()) return 'Escribe el nombre de la marca.';
  if (d.name.trim().length > 80) return 'El nombre admite hasta 80 caracteres.';
  if (d.url.trim() && !/^https:\/\/[^\s@]+$/i.test(d.url.trim())) return 'El sitio de la marca debe empezar con https://';
  if (d.taxonomy && d.slug.trim() && !/^[a-z0-9]+(-[a-z0-9]+)*$/.test(d.slug.trim())) return 'La dirección web solo admite minúsculas, números y guiones (ej. h-n).';
  return null;
}

export async function saveBrand(d: BrandDraft): Promise<BrandDraft> {
  const body = {
    nombre: d.name.trim(),
    descripcion: d.tagline.trim() || null,
    url: d.url.trim() || null,
    activo: d.active,
    orden: d.order,
    // F24: solo si el backend ya lo tiene (si no, el cuerpo queda igual que antes).
    ...(d.taxonomy ? { slug: d.slug.trim() || null, visibleEnCarrusel: d.carousel } : {}),
  };
  const r = d.id
    ? await apiPut<ApiBrand>(`/api/administracion/marcas/${d.id}`, { ...body, version: d.version })
    : await apiPost<ApiBrand>('/api/administracion/marcas', body);
  return toDraft(r);
}

export async function uploadBrandLogo(id: string, file: File, operationId: string): Promise<string> {
  const form = new FormData();
  form.append('idOperacion', operationId);
  form.append('archivo', file);
  const r = await apiUpload<{ logoUrl: string }>(`/api/administracion/marcas/${id}/logo`, form);
  return r.logoUrl;
}

export const removeBrandLogo = (id: string) => apiDelete(`/api/administracion/marcas/${id}/logo`);

export const LOGO_ACCEPT = 'image/png,image/jpeg,image/webp';
export const LOGO_MAX_BYTES = 2 * 1024 * 1024;
