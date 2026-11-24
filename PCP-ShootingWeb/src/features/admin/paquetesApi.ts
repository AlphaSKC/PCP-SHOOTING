/*
 * Paquetes y consumibles (R02) · API real.
 *   GET  /api/administracion/paquetes[?activo]      (Administrador y Recepción)
 *   POST /api/administracion/paquetes                (solo Administrador)
 *   PUT  /api/administracion/paquetes/{id}           (solo Administrador; cuerpo completo + version; 409 = desactualizado)
 * Consumibles = productos internos de categoría 4 (no vendibles):
 *   GET  /api/administracion/productos?categoria=4   · POST /api/productos (crear)
 * Reglas: por actividad y por persona, sin duración. Precio null = pendiente (no es cero).
 * Para activar: precio por persona > 0 (y CARTAQ > 0 si se indica) y consumibles activos.
 * PUT reemplaza la composición completa. No hay borrado: se desactiva.
 */
import { apiGetAll, apiPost, apiPut, type WriteResult } from '@/lib/api';

const BASE = '/api/administracion/paquetes';
export const CONSUMABLE_CATEGORY = 4;

type ApiPackage = {
  id: string;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  incluyeMarkdown: string;
  requisitosMarkdown: string | null;
  precioPorPersona: number | null;
  precioSocioCartaq: number | null;
  activo: boolean;
  visibleEnCatalogo: boolean;
  actualizadoEnUtc: string;
  version: string;
  consumibles: { productoId: string; sku: string; nombre: string; cantidadPorPersona: number; activo: boolean }[];
};

export type PackageConsumable = { productId: string; name: string; perPerson: string; active: boolean };

export type PackageDraft = {
  id: string | null;
  version: string | null;
  code: string;
  name: string;
  description: string;
  includes: string;
  requirements: string;
  price: string;
  memberPrice: string;
  active: boolean;
  visible: boolean;
  updatedAt: string | null;
  consumables: PackageConsumable[];
};

export type Consumable = { id: string; sku: string; name: string; active: boolean };

const priceText = (n: number | null) => (n == null ? '' : String(n));

const toDraft = (p: ApiPackage): PackageDraft => ({
  id: p.id,
  version: p.version,
  code: p.codigo,
  name: p.nombre,
  description: p.descripcion ?? '',
  includes: p.incluyeMarkdown,
  requirements: p.requisitosMarkdown ?? '',
  price: priceText(p.precioPorPersona),
  memberPrice: priceText(p.precioSocioCartaq),
  active: p.activo,
  visible: p.visibleEnCatalogo,
  updatedAt: p.actualizadoEnUtc,
  consumables: p.consumibles.map((c) => ({ productId: c.productoId, name: c.nombre, perPerson: String(c.cantidadPorPersona), active: c.activo })),
});

export const emptyPackage = (): PackageDraft => ({
  id: null,
  version: null,
  code: '',
  name: '',
  description: '',
  includes: '',
  requirements: '',
  price: '',
  memberPrice: '',
  active: false,
  visible: false,
  updatedAt: null,
  consumables: [],
});

export async function listPackages(): Promise<PackageDraft[]> {
  const list = await apiGetAll<ApiPackage>(BASE);
  return list.map(toDraft).sort((a, b) => a.name.localeCompare(b.name, 'es'));
}

export async function listConsumables(): Promise<Consumable[]> {
  const list = await apiGetAll<{ id: string; sku: string; nombre: string; activo: boolean }>('/api/administracion/productos', {
    categoria: CONSUMABLE_CATEGORY,
  });
  return list.map((p) => ({ id: p.id, sku: p.sku, name: p.nombre, active: p.activo })).sort((a, b) => a.name.localeCompare(b.name, 'es'));
}

/** Crea un consumible interno (categoría 4, no vendible). Sus existencias se manejan en Inventario. */
export async function createConsumable(name: string): Promise<string> {
  const sku = `CONS-${suggestCode(name).slice(0, 40)}-${crypto.randomUUID().slice(0, 4).toUpperCase()}`;
  const r = await apiPost<WriteResult>('/api/productos', { sku, nombre: name.trim(), categoria: CONSUMABLE_CATEGORY, esVendible: false, precio: null });
  return r.id;
}

/** Código sugerido: solo letras ASCII, números y guiones simples ("Tirada básica" → "TIRADA-BASICA"). */
export function suggestCode(name: string): string {
  return name
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);
}

export const CODE_PATTERN = /^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$/;

/** Precio opcional: vacío = null (pendiente). undefined = inválido. */
export function parsePrice(text: string): number | null | undefined {
  const t = text.trim().replace(',', '.');
  if (!t) return null;
  return /^\d+(\.\d{1,2})?$/.test(t) ? Number(t) : undefined;
}

/** Revisión local antes de guardar (la API vuelve a validar). */
export function validatePackage(d: PackageDraft): string | null {
  if (!d.name.trim()) return 'Escribe el nombre del paquete.';
  if (!CODE_PATTERN.test(d.code.trim())) return 'El código solo admite letras, números y guiones (ej. TIRADA-BASICA).';
  if (!d.includes.trim()) return 'Escribe qué incluye el paquete (se muestra al reservar).';
  const price = parsePrice(d.price);
  const member = parsePrice(d.memberPrice);
  if (price === undefined || member === undefined) return 'Los precios deben ser números con hasta 2 decimales.';
  if (d.active) {
    if (!price) return 'Para activar el paquete, el precio por persona debe ser mayor a 0.';
    if (member === 0) return 'El precio CARTAQ debe ser mayor a 0 o quedar vacío.';
    if (d.consumables.some((c) => !c.active)) return 'Para activar el paquete, todos sus consumibles deben estar activos.';
  }
  const ids = d.consumables.map((c) => c.productId);
  if (new Set(ids).size !== ids.length) return 'Cada consumible solo puede aparecer una vez.';
  for (const c of d.consumables) {
    const n = Number(c.perPerson);
    if (!Number.isInteger(n) || n < 1 || n > 100000) return `La cantidad por persona de "${c.name}" debe ser un número entero mayor a 0.`;
  }
  return null;
}

export async function savePackage(d: PackageDraft): Promise<string> {
  const body = {
    codigo: d.code.trim().toUpperCase(),
    nombre: d.name.trim(),
    descripcion: d.description.trim() || null,
    incluyeMarkdown: d.includes.trim(),
    requisitosMarkdown: d.requirements.trim() || null,
    precioPorPersona: parsePrice(d.price) ?? null,
    precioSocioCartaq: parsePrice(d.memberPrice) ?? null,
    activo: d.active,
    consumibles: d.consumables.map((c) => ({ productoId: c.productId, cantidadPorPersona: Number(c.perPerson) })),
  };
  if (d.id) {
    const r = await apiPut<{ id: string }>(`${BASE}/${d.id}`, { ...body, version: d.version });
    return r.id ?? d.id;
  }
  const r = await apiPost<{ id: string }>(BASE, body);
  return r.id;
}
