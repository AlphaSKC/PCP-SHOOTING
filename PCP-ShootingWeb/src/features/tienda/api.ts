/*
 * Tienda. Con VITE_DATA_SOURCE=api todo usa la API real:
 *   getProducts  → GET  /api/productos?esVendible=true          (público)
 *   createOrder  → POST /api/pedidos { idOperacion, partidas }   (requiere sesión; nace PendientePago)
 *   getOrder     → GET  /api/pedidos/{id}                        (solo pedidos propios)
 *   listMyOrders → GET  /api/pedidos
 *   cancelOrder  → POST /api/pedidos/{id}/cancelacion            (solo pendientes de pago)
 * El pago en línea (Mercado Pago, R05) AÚN NO existe en la API: el pedido queda pendiente y el club
 * registra el pago desde el panel. En modo demo, `simulatePayment` imita el webhook.
 * Regla: SOLO recolección en el club, sin envíos ni costo de envío (CLAUDE.md §6).
 */

import { ApiError, DATA_SOURCE, apiGet, apiGetAll, apiPost, type WriteResult } from '@/lib/api';
import type { CartItem, Order, OrderContact, OrderStatus, OrderSummary, Product } from './types';
import { toast } from '@/lib/toast';

/** false mientras la API no tenga Mercado Pago: el checkout crea el pedido sin cobrar en línea. */
export const ONLINE_PAYMENT = DATA_SOURCE !== 'api';

const delay = (ms = 300) => new Promise((r) => setTimeout(r, ms));

/** DATOS DE EJEMPLO de los prototipos: productos, precios y fotos reales los entrega el cliente. */
const SIZES = ['CH', 'M', 'G', 'XG'];
export const PRODUCTS: Product[] = [
  { id: 'playera-negra', category: 'playeras', categoryLabel: 'Playera', name: 'Playera PCP Negra', description: 'Logo dorado · Tallas CH a XG', price: 450, sizes: SIZES, featured: 1 },
  { id: 'playera-roja', category: 'playeras', categoryLabel: 'Playera', name: 'Playera PCP Roja', description: 'Logo blanco · Tallas CH a XG', price: 450, sizes: SIZES, featured: 2 },
  { id: 'playera-blanca', category: 'playeras', categoryLabel: 'Playera', name: 'Playera PCP Blanca', description: 'Logo a todo color · Tallas CH a XG', price: 450, sizes: SIZES, featured: 3 },
  { id: 'gorra-negra', category: 'gorras', categoryLabel: 'Gorra', name: 'Gorra PCP Negra', description: 'Logo dorado · Ajustable', price: 380, featured: 4 },
  { id: 'gorra-blanca', category: 'gorras', categoryLabel: 'Gorra', name: 'Gorra PCP Blanca', description: 'Logo a todo color · Ajustable', price: 380, featured: 5 },
  { id: 'taza', category: 'accesorios', categoryLabel: 'Taza', name: 'Taza PCP', description: 'Disponible en negro, rojo y blanco', price: 220, featured: 6 },
  { id: 'stickers', category: 'accesorios', categoryLabel: 'Stickers', name: 'Paquete de stickers', description: 'Versiones negra, blanca y roja', price: 90, featured: 7 },
];

/* ---------- Catálogo: API real (GET /api/productos, público) o demo ---------- */

/** Respuesta de la API (ProductoCatalogo). Categoria: 0 SinClasificar, 1 Alimento, 2 Playera, 3 ReplicaDecorativa. */
type ApiProduct = {
  id: string;
  sku: string;
  nombre: string;
  descripcion: string | null;
  esVendible: boolean;
  precio: number | null;
  categoria: number;
  imagenUrl: string | null;
  notasMarkdown: string | null;
  /** Categoría de tienda y marca (F24, solicitado a backend). */
  categoriaTienda?: { id: string; nombre: string; slug: string } | null;
  marca?: { id: string; nombre: string; slug: string; logoUrl: string | null; orden?: number } | null;
  /** Varias fotos (F23, solicitado a backend): lista ordenada; la primera es la principal. */
  imagenes?: { url: string; orden?: number }[] | null;
  /** Existencias (F08/F11). */
  existencia?: number | null;
  agotado?: boolean | null;
  /** Tallas S/M/L/XL (F09). */
  usaTallas?: boolean;
  tallas?: { talla: string; existencia: number; agotado: boolean }[] | null;
};

/** Tallas fijas de la API, en este orden. */
export const SIZES_API = ['S', 'M', 'L', 'XL'];

const CACHE_KEY = 'pcp.catalogo.api';
/** Evento que se emite al recargar el catálogo (el carrito recalcula existencias). */
export const CATALOG_EVENT = 'pcp:catalogo';

/** Productos de la API ya cargados (y guardados para que el carrito los reconozca al recargar). */
let apiCache: Product[] = (() => {
  if (DATA_SOURCE !== 'api') return [];
  try {
    return JSON.parse(localStorage.getItem(CACHE_KEY) ?? '[]') as Product[];
  } catch {
    return [];
  }
})();

/** Fotos en orden: las de F23 si existen; si no, la principal. Sin repetidas. */
function productImages(p: ApiProduct): string[] {
  const list = [...(p.imagenes ?? [])].sort((a, b) => (a.orden ?? 0) - (b.orden ?? 0)).map((i) => i.url);
  const all = p.imagenUrl ? [p.imagenUrl, ...list] : list;
  return [...new Set(all.filter(Boolean))];
}

/**
 * Convierte un producto de la API al modelo de la tienda.
 * La tienda solo muestra artículos vendibles con precio; alimentos (1) y réplicas decorativas (3)
 * tienen su propia sección. Las tallas aún no existen en la API (cada talla sería un SKU distinto).
 */
export function mapApiProduct(p: ApiProduct, index: number): Product | null {
  if (!p.esVendible || p.precio == null || p.categoria === 1 || p.categoria === 3) return null;
  const isShirt = p.categoria === 2;
  const sized = Boolean(p.usaTallas && p.tallas?.length);
  return {
    id: p.id,
    sku: p.sku,
    category: isShirt ? 'playeras' : 'accesorios',
    categoryLabel: isShirt ? 'Playera' : 'Artículo',
    name: p.nombre,
    description: p.descripcion ?? '',
    price: p.precio,
    imageUrl: p.imagenUrl,
    images: productImages(p),
    storeCategory: p.categoriaTienda ? { id: p.categoriaTienda.id, name: p.categoriaTienda.nombre, slug: p.categoriaTienda.slug } : null,
    brand: p.marca ? { id: p.marca.id, name: p.marca.nombre, slug: p.marca.slug, logoUrl: p.marca.logoUrl, order: p.marca.orden ?? 0 } : null,
    notes: p.notasMarkdown,
    featured: index + 1,
    stock: p.existencia ?? null,
    soldOut: Boolean(p.agotado),
    ...(sized
      ? {
          sizes: SIZES_API.filter((t) => p.tallas!.some((x) => x.talla === t)),
          sizeStock: Object.fromEntries(p.tallas!.map((x) => [x.talla, x.existencia])),
        }
      : {}),
  };
}

/**
 * Piezas disponibles para un producto (y talla). `Infinity` si no hay control de existencias (modo demo).
 * El dato público es informativo: la API vuelve a validar al crear el pedido y al pagar.
 */
export function availableFor(product: Product, size?: string): number {
  if (product.sizeStock) return size ? product.sizeStock[size] ?? 0 : 0;
  if (product.stock == null) return product.soldOut ? 0 : Infinity;
  return Math.max(0, product.stock);
}

/** Primera talla con existencias (prefiere M); undefined si no hay ninguna. */
export function firstAvailableSize(product: Product): string | undefined {
  if (!product.sizes) return undefined;
  const order = product.sizes.includes('M') ? ['M', ...product.sizes.filter((s) => s !== 'M')] : product.sizes;
  return order.find((s) => availableFor(product, s) > 0);
}

/** ¿Se puede agregar al carrito? (alguna talla o el producto con existencias). */
export const canBuy = (product: Product) => (product.sizes ? Boolean(firstAvailableSize(product)) : availableFor(product) > 0);

export async function getProducts(): Promise<Product[]> {
  if (DATA_SOURCE === 'api') {
    const raw = await apiGetAll<ApiProduct>('/api/productos', { esVendible: true });
    apiCache = raw.map(mapApiProduct).filter((p): p is Product => p !== null);
    try {
      localStorage.setItem(CACHE_KEY, JSON.stringify(apiCache));
    } catch {
      /* sin almacenamiento */
    }
    // Avisa al carrito que cambiaron precios/existencias.
    window.dispatchEvent(new Event(CATALOG_EVENT));
    return apiCache;
  }
  await delay(200);
  return PRODUCTS;
}

export const productById = (id: string) => (DATA_SOURCE === 'api' ? apiCache : PRODUCTS).find((p) => p.id === id);

/** Agrega desde una tarjeta (con la talla elegida en ella) y confirma con una alerta. */
export function addToCart(cart: { add: (p: Product, size?: string, quantity?: number) => void }, product: Product, size?: string) {
  cart.add(product, size);
  toast.success(`Agregado al carrito: ${product.name}${size ? ` · talla ${size}` : ''}.`);
}

/** Código que va en la URL del producto (SKU en la API, id en demo). */
export const productCode = (p: Product) => p.sku ?? p.id;
/** Página de vista previa del producto. */
export const productPath = (p: Product) => `/tienda/producto/${encodeURIComponent(productCode(p))}`;

/** Catálogo ya cargado (sin volver a pedirlo a la API). */
export const cachedProducts = () => (DATA_SOURCE === 'api' ? apiCache : PRODUCTS);

/** Producto ya cargado (para pintar al instante mientras se refresca). */
export const cachedProductByCode = (code: string) => (DATA_SOURCE === 'api' ? apiCache : PRODUCTS).find((p) => productCode(p) === code);

/**
 * Producto por SKU con precio y existencias al día. Usa el catálogo público (`GET /api/productos`),
 * porque la ficha `GET /api/productos/{sku}` no incluye precio ni existencias. `null` si no se vende en la tienda.
 */
export async function getProductByCode(code: string): Promise<Product | null> {
  const list = await getProducts();
  return list.find((p) => productCode(p) === code) ?? null;
}

/* ---------- Pedidos: API real ---------- */

/** EstadoPedido de la API (texto en /api/pedidos, número en /api/administracion). */
export function mapOrderStatus(estado: string | number): OrderStatus {
  switch (estado) {
    case 'Pagado':
    case 1:
      return 'pagado';
    case 'Recogido':
    case 2:
      return 'entregado';
    case 'Cancelado':
    case 3:
      return 'cancelada';
    default:
      return 'pendiente_pago';
  }
}

/** Folio corto y legible a partir del UUID del pedido. */
export const folioOf = (id: string) => `PCP-${id.replace(/-/g, '').slice(0, 8).toUpperCase()}`;

type ApiOrderSummary = { id: string; creadoEnUtc: string; estado: string; recogidoEnUtc: string | null };
type ApiOrder = {
  resumen: ApiOrderSummary;
  detalles: { productoId: string; cantidad: number; precioUnitario: number; talla?: string | null }[];
  total: number;
  importeConfirmado: number;
};

/** Mismo id de operación mientras el carrito no cambie: un reintento no duplica el pedido. */
const OP_KEY = 'pcp.pedido.operacion';
function operationIdFor(partidas: { productoId: string; talla: string | null; cantidad: number }[]) {
  const signature = JSON.stringify([...partidas].sort((a, b) => `${a.productoId}${a.talla}`.localeCompare(`${b.productoId}${b.talla}`)));
  try {
    const saved = JSON.parse(sessionStorage.getItem(OP_KEY) ?? 'null') as { signature: string; id: string } | null;
    if (saved?.signature === signature) return saved.id;
    const id = crypto.randomUUID();
    sessionStorage.setItem(OP_KEY, JSON.stringify({ signature, id }));
    return id;
  } catch {
    return crypto.randomUUID();
  }
}
const forgetOperation = () => {
  try {
    sessionStorage.removeItem(OP_KEY);
  } catch {
    /* sin almacenamiento */
  }
};

/** Una partida por producto + talla (la API rechaza combinaciones repetidas). */
function toPartidas(items: CartItem[]) {
  const byKey = new Map<string, { productoId: string; talla: string | null; cantidad: number }>();
  for (const i of items) {
    const key = `${i.productId}:${i.size ?? ''}`;
    const cur = byKey.get(key);
    if (cur) cur.cantidad += i.quantity;
    else byKey.set(key, { productoId: i.productId, talla: i.size ?? null, cantidad: i.quantity });
  }
  return [...byKey.values()];
}

async function createOrderApi(items: CartItem[]): Promise<Order> {
  const partidas = toPartidas(items);
  if (partidas.length === 0) throw new OrderError('Tu carrito está vacío.');
  const r = await apiPost<WriteResult>('/api/pedidos', { idOperacion: operationIdFor(partidas), partidas });
  forgetOperation();
  const order = await getOrderApi(r.id);
  if (!order) throw new OrderError('Creamos tu pedido pero no pudimos consultarlo. Revísalo en tu perfil.');
  return order;
}

async function getOrderApi(id: string): Promise<Order | null> {
  try {
    const o = await apiGet<ApiOrder>(`/api/pedidos/${id}`);
    return {
      id: o.resumen.id,
      folio: folioOf(o.resumen.id),
      status: mapOrderStatus(o.resumen.estado),
      lines: o.detalles.map((d) => ({
        productId: d.productoId,
        name: productById(d.productoId)?.name ?? 'Producto de la tienda',
        size: d.talla ?? undefined,
        quantity: d.cantidad,
        price: d.precioUnitario,
      })),
      total: o.total,
      delivery: 'recoger_en_club',
      createdAt: o.resumen.creadoEnUtc,
      pickedUpAt: o.resumen.recogidoEnUtc,
    };
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

export async function listMyOrders(): Promise<OrderSummary[]> {
  if (DATA_SOURCE !== 'api') {
    return Object.values(readStore())
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
      .map((o) => ({ id: o.id, folio: o.folio, status: o.status, createdAt: o.createdAt, pickedUpAt: null }));
  }
  const list = await apiGet<ApiOrderSummary[]>('/api/pedidos');
  return list
    .map((o) => ({ id: o.id, folio: folioOf(o.id), status: mapOrderStatus(o.estado), createdAt: o.creadoEnUtc, pickedUpAt: o.recogidoEnUtc }))
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

export async function cancelOrder(id: string): Promise<void> {
  if (DATA_SOURCE !== 'api') {
    const current = readStore()[id] ?? memory[id];
    if (current) save({ ...current, status: 'cancelada' });
    return;
  }
  await apiPost<WriteResult>(`/api/pedidos/${id}/cancelacion`);
}

/* ---------- Pedidos simulados ---------- */

const STORE_KEY = 'pcp.pedidos.demo';
const memory: Record<string, Order> = {};

function readStore(): Record<string, Order> {
  try {
    return JSON.parse(localStorage.getItem(STORE_KEY) ?? '{}') as Record<string, Order>;
  } catch {
    return {};
  }
}

function save(order: Order) {
  memory[order.id] = order;
  try {
    localStorage.setItem(STORE_KEY, JSON.stringify({ ...readStore(), [order.id]: order }));
  } catch {
    /* sin almacenamiento: el pedido vive en memoria */
  }
}

export class OrderError extends Error {}

export async function createOrder(items: CartItem[], contact: OrderContact): Promise<Order> {
  if (DATA_SOURCE === 'api') return createOrderApi(items);
  await delay(600);
  const lines = items.map((i) => {
    const p = productById(i.productId);
    if (!p) throw new OrderError('Uno de los productos ya no está disponible.');
    return { productId: p.id, name: p.name, size: i.size, quantity: i.quantity, price: p.price };
  });
  if (lines.length === 0) throw new OrderError('Tu carrito está vacío.');
  const n = String(Math.floor(Math.random() * 9000) + 1000);
  const order: Order = {
    id: `PCP-${n}`,
    folio: `PCP-${n}`,
    status: 'pendiente_pago',
    lines,
    total: lines.reduce((s, l) => s + l.price * l.quantity, 0),
    contact,
    delivery: 'recoger_en_club',
    createdAt: new Date().toISOString(),
  };
  save(order);
  return order;
}

export async function simulatePayment(id: string): Promise<Order> {
  await delay(1200);
  const current = readStore()[id] ?? memory[id];
  if (!current) throw new OrderError('No encontramos el pedido.');
  const paid: Order = { ...current, status: 'pagado' };
  save(paid);
  return paid;
}

export async function getOrder(id: string): Promise<Order | null> {
  if (DATA_SOURCE === 'api') return getOrderApi(id);
  await delay(200);
  return readStore()[id] ?? memory[id] ?? null;
}

/** Datos de la tarjeta de producto (existencias y tallas incluidas). */
export function cardData(p: Product) {
  const left = p.sizes ? undefined : availableFor(p);
  return {
    name: p.name,
    // Con F24 la etiqueta es la marca (o la categoría de la tienda); si no, el tipo.
    category: p.brand?.name ?? p.storeCategory?.name ?? p.categoryLabel,
    description: p.description,
    price: p.price,
    imageUrl: p.imageUrl,
    soldOut: !canBuy(p),
    stockNote: left !== undefined && Number.isFinite(left) && left > 0 && left <= 3 ? `Quedan ${left}` : undefined,
    sizes: p.sizes?.map((t) => ({ label: t, out: availableFor(p, t) <= 0 })),
  };
}
