export type ProductCategory = 'playeras' | 'gorras' | 'accesorios';

export type Product = {
  id: string;
  category: ProductCategory;
  /** Etiqueta visible ("Playera", "Taza"…). */
  categoryLabel: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string | null;
  /** Tallas disponibles (solo ropa). */
  sizes?: string[];
  /** Orden de "Destacados". */
  featured: number;
};

/** Renglón del carrito: un producto + talla. */
export type CartItem = {
  productId: string;
  size?: string;
  quantity: number;
};

export type OrderContact = { nombre: string; email: string; telefono: string };

/**
 * Estados del pedido. API: PendientePago → Pagado → Recogido (= entregado) · Cancelado (= cancelada).
 * 'preparando' y 'listo_para_recoger' solo existen en el diseño/demo (la API aún no los tiene).
 */
export type OrderStatus = 'pendiente_pago' | 'pagado' | 'preparando' | 'listo_para_recoger' | 'entregado' | 'cancelada';

export type OrderLine = { productId: string; name: string; size?: string; quantity: number; price: number };

export type Order = {
  id: string;
  /** Folio visible: "PCP-0142". */
  folio: string;
  status: OrderStatus;
  lines: OrderLine[];
  total: number;
  /** Datos de contacto (en la API salen del perfil del cliente). */
  contact?: OrderContact;
  /** Solo recolección en el club (no hay envíos). */
  delivery: 'recoger_en_club';
  createdAt: string;
  pickedUpAt?: string | null;
};

/** Pedido en la lista "Mis pedidos" (la API no devuelve partidas ni total en la lista). */
export type OrderSummary = { id: string; folio: string; status: OrderStatus; createdAt: string; pickedUpAt: string | null };
