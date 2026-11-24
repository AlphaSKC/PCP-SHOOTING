/*
 * Alimentos y bebidas — SIMULADO. Endpoints propuestos (CLAUDE.md §9):
 *   getMenu          → GET  /api/alimentos
 *   createFoodOrder  → POST /api/alimentos/pedidos   (estado inicial: recibido)
 *   (staff)          → PATCH /api/alimentos/pedidos/{id}/estado
 * DATOS DE EJEMPLO: la carta real (productos, precios, existencias) la captura el club en el panel.
 */

/**
 * PENDIENTE (CLAUDE.md §8): ¿se paga en línea o en el club?
 *   'en_club'  → el pedido se envía y se paga al recibirlo (por defecto).
 *   'en_linea' → se paga en Mercado Pago antes de prepararlo.
 */
export const FOOD_PAYMENT_MODE: 'en_club' | 'en_linea' = 'en_club';

export type FoodCategory = 'bebidas' | 'snacks' | 'comida';

export const FOOD_CATEGORIES: { id: FoodCategory | 'todo'; label: string }[] = [
  { id: 'todo', label: 'Todo' },
  { id: 'bebidas', label: 'Bebidas' },
  { id: 'snacks', label: 'Snacks' },
  { id: 'comida', label: 'Comida' },
];

export type FoodItem = {
  id: string;
  category: FoodCategory;
  name: string;
  description: string;
  price: number;
  available: boolean;
};

const MENU: FoodItem[] = [
  { id: 'agua', category: 'bebidas', name: 'Agua natural', description: 'Botella de 600 ml', price: 25, available: true },
  { id: 'refresco', category: 'bebidas', name: 'Refresco', description: 'Lata de 355 ml · varios sabores', price: 35, available: true },
  { id: 'agua-sabor', category: 'bebidas', name: 'Agua fresca', description: 'Vaso de 500 ml · sabor del día', price: 35, available: true },
  { id: 'cafe', category: 'bebidas', name: 'Café americano', description: 'Taza de 350 ml', price: 40, available: true },
  { id: 'electrolitos', category: 'bebidas', name: 'Bebida hidratante', description: 'Botella de 600 ml', price: 40, available: false },
  { id: 'papas', category: 'snacks', name: 'Papas fritas', description: 'Bolsa individual', price: 30, available: true },
  { id: 'cacahuates', category: 'snacks', name: 'Cacahuates japoneses', description: 'Porción individual', price: 25, available: true },
  { id: 'barra', category: 'snacks', name: 'Barra de cereal', description: 'Avena y miel', price: 30, available: true },
  { id: 'hotdog', category: 'comida', name: 'Hot dog', description: 'Salchicha de pavo, pan artesanal y aderezos', price: 70, available: true },
  { id: 'torta', category: 'comida', name: 'Torta de jamón', description: 'Jamón, queso, aguacate y jitomate', price: 90, available: true },
  { id: 'sincronizada', category: 'comida', name: 'Sincronizada', description: 'Tortilla de harina con jamón y queso', price: 75, available: true },
];

const delay = (ms = 300) => new Promise((r) => setTimeout(r, ms));

export async function getMenu(): Promise<FoodItem[]> {
  await delay(200);
  return MENU;
}

export type FoodOrderStatus = 'recibido' | 'en_preparacion' | 'listo' | 'entregado';

export const FOOD_STATUS_FLOW: { id: FoodOrderStatus; label: string }[] = [
  { id: 'recibido', label: 'Recibido' },
  { id: 'en_preparacion', label: 'En preparación' },
  { id: 'listo', label: 'Listo' },
  { id: 'entregado', label: 'Entregado' },
];

export type FoodDelivery = { type: 'linea'; line: number } | { type: 'barra' };

export type FoodOrder = {
  folio: string;
  status: FoodOrderStatus;
  items: { id: string; name: string; quantity: number; price: number }[];
  total: number;
  delivery: FoodDelivery;
  paid: boolean;
};

export async function createFoodOrder(
  items: { item: FoodItem; quantity: number }[],
  delivery: FoodDelivery,
  contact: { nombre: string; telefono: string },
): Promise<FoodOrder> {
  await delay(FOOD_PAYMENT_MODE === 'en_linea' ? 1400 : 700);
  if (import.meta.env.DEV) console.info('[simulado] POST /api/alimentos/pedidos', { items, delivery, contact });
  return {
    folio: `AB-${Math.floor(Math.random() * 900) + 100}`,
    status: 'recibido',
    items: items.map(({ item, quantity }) => ({ id: item.id, name: item.name, quantity, price: item.price })),
    total: items.reduce((s, { item, quantity }) => s + item.price * quantity, 0),
    delivery,
    paid: FOOD_PAYMENT_MODE === 'en_linea',
  };
}
