/*
 * Tienda. El catálogo ya usa la API real cuando VITE_DATA_SOURCE=api:
 *   getProducts  → GET  /api/productos?esVendible=true (CONECTADO)
 * Lo demás sigue simulado (requiere login con Supabase):
 *   createOrder  → POST /api/pedidos  → { pedido pendiente_pago, preferencia de Mercado Pago }
 *   getOrder     → GET  /api/pedidos/{id}
 * El pago se confirma con el webhook de Mercado Pago en el backend; aquí lo simula `simulatePayment`.
 * Regla: SOLO recolección en el club, sin envíos ni costo de envío (CLAUDE.md §6).
 */

import { DATA_SOURCE, apiGetAll } from '@/lib/api';
import type { CartItem, Order, OrderContact, Product } from './types';

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
};

const CACHE_KEY = 'pcp.catalogo.api';

/** Productos de la API ya cargados (y guardados para que el carrito los reconozca al recargar). */
let apiCache: Product[] = (() => {
  if (DATA_SOURCE !== 'api') return [];
  try {
    return JSON.parse(localStorage.getItem(CACHE_KEY) ?? '[]') as Product[];
  } catch {
    return [];
  }
})();

/**
 * Convierte un producto de la API al modelo de la tienda.
 * La tienda solo muestra artículos vendibles con precio; alimentos (1) y réplicas decorativas (3)
 * tienen su propia sección. Las tallas aún no existen en la API (cada talla sería un SKU distinto).
 */
export function mapApiProduct(p: ApiProduct, index: number): Product | null {
  if (!p.esVendible || p.precio == null || p.categoria === 1 || p.categoria === 3) return null;
  const isShirt = p.categoria === 2;
  return {
    id: p.id,
    category: isShirt ? 'playeras' : 'accesorios',
    categoryLabel: isShirt ? 'Playera' : 'Artículo',
    name: p.nombre,
    description: p.descripcion ?? '',
    price: p.precio,
    imageUrl: p.imagenUrl,
    featured: index + 1,
  };
}

export async function getProducts(): Promise<Product[]> {
  if (DATA_SOURCE === 'api') {
    const raw = await apiGetAll<ApiProduct>('/api/productos', { esVendible: true });
    apiCache = raw.map(mapApiProduct).filter((p): p is Product => p !== null);
    try {
      localStorage.setItem(CACHE_KEY, JSON.stringify(apiCache));
    } catch {
      /* sin almacenamiento */
    }
    return apiCache;
  }
  await delay(200);
  return PRODUCTS;
}

export const productById = (id: string) => (DATA_SOURCE === 'api' ? apiCache : PRODUCTS).find((p) => p.id === id);

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
  await delay(200);
  return readStore()[id] ?? memory[id] ?? null;
}
