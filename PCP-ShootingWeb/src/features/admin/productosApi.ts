/*
 * Productos de la tienda en el panel · API real (VITE_DATA_SOURCE=api).
 *   Listar      → GET   /api/administracion/productos   (incluye inactivos)
 *   Existencias → GET   /api/inventario                 (existencia total por producto)
 *   Crear       → POST  /api/productos
 *   Editar      → PUT   /api/productos/{id}             (reemplaza todos los campos)
 *   Visible     → PATCH /api/productos/{id}/estado      { activo }
 *   Foto        → POST  /api/productos/{id}/imagen      (multipart: archivo + idOperacion)
 *   Entrada     → POST  /api/inventario/{id}/movimientos { idOperacion, tipo: 0, variacionCantidad, motivo }
 * Permisos: Administrador (Recepción solo alimentos). La API valida en cada petición.
 * Borrar → DELETE /api/productos/{id}  (F22, propuesto: solo productos sin historial; 409 si tiene pedidos/movimientos/paquetes)
 * Aún NO existe en la API: varias fotos por producto en la tienda (F23).
 * F24 (prioridad 1, pendiente de backend): `categoriaTiendaId` y `marcaId` al listar y al guardar.
 */
import { apiDelete, apiGetAll, apiPatch, apiPost, apiPut, apiUpload, type WriteResult } from '@/lib/api';

/** Categorías de la tienda de ropa. 1 Alimento, 3 Réplica decorativa y 4 Consumible tienen su propia pantalla. */
export const STORE_CATEGORIES = [
  { value: 2, label: 'Playera (con tallas)' },
  { value: 0, label: 'Artículo sin tallas (rifle, munición, accesorio, gorra…)' },
] as const;
export type StoreCategory = (typeof STORE_CATEGORIES)[number]['value'];
export const categoryLabel = (c: number) => (c === 2 ? 'Playera' : c === 0 ? 'Artículo' : 'Otra');

/** ProductoGestion de la API. */
type ApiProductoGestion = {
  id: string;
  sku: string;
  nombre: string;
  descripcion: string | null;
  esVendible: boolean;
  precio: number | null;
  activo: boolean;
  stockMinimo: number | null;
  categoria: number;
  imagenUrl: string | null;
  notasMarkdown: string | null;
  especificacionesTecnicas?: unknown;
  usaTallas?: boolean;
  tallas?: { talla: string; existencia: number; agotado: boolean }[] | null;
  /** F24. */
  categoriaTiendaId?: string | null;
  marcaId?: string | null;
};

/** ExistenciaProducto de la API. */
type ApiExistencia = { productoId: string; existencia: number; stockMinimo: number | null; bajoMinimo: boolean };

export type ApiAdminProduct = {
  id: string;
  sku: string;
  name: string;
  description: string;
  category: number;
  price: number | null;
  active: boolean;
  sellable: boolean;
  imageUrl: string | null;
  notes: string | null;
  specs: unknown;
  stock: number;
  minStock: number | null;
  lowStock: boolean;
  /** Playera con tallas S/M/L/XL (F09). */
  usesSizes: boolean;
  /** Existencia por talla (solo si usesSizes). */
  sizeStock: { size: string; stock: number }[] | null;
  /** Categoría de la tienda y marca (F24). */
  storeCategoryId: string | null;
  brandId: string | null;
};

export type ProductDraft = {
  id: string | null;
  sku: string;
  name: string;
  description: string;
  category: StoreCategory;
  price: string;
  imageUrl: string | null;
  notes: string | null;
  specs: unknown;
  /** Solo playeras: activar tallas S/M/L/XL. */
  usesSizes: boolean;
  /** F24: categoría de la tienda y marca ('' = sin asignar). */
  storeCategoryId: string;
  brandId: string;
};

export async function listStoreProducts(): Promise<ApiAdminProduct[]> {
  const [products, stock] = await Promise.all([
    apiGetAll<ApiProductoGestion>('/api/administracion/productos'),
    apiGetAll<ApiExistencia>('/api/inventario'),
  ]);
  const byId = new Map(stock.map((s) => [s.productoId, s]));
  return products
    .filter((p) => p.categoria === 0 || p.categoria === 2)
    .map((p) => {
      const s = byId.get(p.id);
      return {
        id: p.id,
        sku: p.sku,
        name: p.nombre,
        description: p.descripcion ?? '',
        category: p.categoria,
        price: p.precio,
        active: p.activo,
        sellable: p.esVendible,
        imageUrl: p.imagenUrl,
        notes: p.notasMarkdown,
        specs: p.especificacionesTecnicas ?? null,
        stock: s?.existencia ?? 0,
        minStock: s?.stockMinimo ?? p.stockMinimo,
        lowStock: s?.bajoMinimo ?? false,
        usesSizes: Boolean(p.usaTallas),
        sizeStock: p.usaTallas && p.tallas ? p.tallas.map((t) => ({ size: t.talla, stock: t.existencia })) : null,
        storeCategoryId: p.categoriaTiendaId ?? null,
        brandId: p.marcaId ?? null,
      };
    })
    .sort((a, b) => a.name.localeCompare(b.name, 'es'));
}

/** Precio en MXN con hasta 2 decimales ("450", "380.50"). null si no es válido. */
export function parsePrice(text: string): number | null {
  const t = text.trim().replace(',', '.');
  if (!/^\d+(\.\d{1,2})?$/.test(t)) return null;
  const n = Number(t);
  return n > 0 ? n : null;
}

/** SKU sugerido a partir del nombre: "Playera PCP Negra" → "PLAYERA-PCP-NEGRA". */
export function suggestSku(name: string): string {
  return name
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 60);
}

/**
 * Crea o actualiza. El PUT reemplaza todo: se reenvían foto, notas y especificaciones actuales para no borrarlas.
 * `withTaxonomy`: solo cuando el backend ya tiene F24 se envían categoría de tienda y marca.
 */
export async function saveProduct(d: ProductDraft, withTaxonomy = false): Promise<string> {
  const price = parsePrice(d.price);
  const body = {
    sku: d.sku.trim(),
    nombre: d.name.trim(),
    descripcion: d.description.trim() || null,
    categoria: d.category,
    imagenUrl: d.imageUrl,
    notasMarkdown: d.notes,
    esVendible: true,
    precio: price,
    especificacionesTecnicas: d.specs ?? null,
    // Tallas solo en playeras; en artículos se envía false explícito.
    usaTallas: d.category === 2 ? d.usesSizes : false,
    ...(withTaxonomy ? { categoriaTiendaId: d.storeCategoryId || null, marcaId: d.brandId || null } : {}),
  };
  const r = d.id ? await apiPut<WriteResult>(`/api/productos/${d.id}`, body) : await apiPost<WriteResult>('/api/productos', body);
  return r.id;
}

export const setProductActive = (id: string, activo: boolean) => apiPatch<WriteResult>(`/api/productos/${id}/estado`, { activo });

/** Sube la foto principal. `operationId` se conserva entre reintentos del mismo archivo. */
export async function uploadProductImage(id: string, file: File, operationId: string): Promise<string> {
  const form = new FormData();
  form.append('idOperacion', operationId);
  form.append('archivo', file);
  const r = await apiUpload<{ url: string; esImagenActual: boolean }>(`/api/productos/${id}/imagen`, form);
  return r.url;
}

/** Registra una entrada de mercancía (suma piezas). `operationId` evita duplicados si se reintenta. Talla obligatoria si el producto usa tallas. */
export const addStock = (id: string, quantity: number, reason: string, operationId: string, size?: string | null) =>
  apiPost<WriteResult>(`/api/inventario/${id}/movimientos`, {
    idOperacion: operationId,
    tipo: 0,
    variacionCantidad: quantity,
    motivo: reason,
    talla: size ?? null,
  });

export const SIZES = ['S', 'M', 'L', 'XL'] as const;

export const IMAGE_ACCEPT = 'image/png,image/jpeg,image/webp';
export const IMAGE_MAX_BYTES = 5 * 1024 * 1024;

/** Elimina un producto sin historial (F22). 404/405 = el backend aún no tiene la ruta; 409 = tiene historial (ocultarlo). */
export const deleteProduct = (id: string) => apiDelete(`/api/productos/${id}`);
