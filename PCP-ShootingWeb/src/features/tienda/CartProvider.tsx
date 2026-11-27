import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { CATALOG_EVENT, availableFor, firstAvailableSize, productById } from './api';
import type { CartItem, Product } from './types';

const KEY = 'pcp.carrito';

/** Clave de renglón: producto + talla (la misma playera en otra talla es otro renglón). */
export const lineKey = (i: Pick<CartItem, 'productId' | 'size'>) => `${i.productId}:${i.size ?? ''}`;

/** `max`: piezas disponibles para esa talla (Infinity sin control de existencias). `short`: el carrito pide más de lo disponible. */
export type CartLineView = CartItem & { key: string; product: Product; lineTotal: number; max: number; short: boolean };

type CartContextValue = {
  items: CartItem[];
  lines: CartLineView[];
  count: number;
  subtotal: number;
  /** Panel lateral del carrito (escritorio y tableta). */
  drawerOpen: boolean;
  openDrawer: () => void;
  closeDrawer: () => void;
  add: (product: Product, size?: string, quantity?: number) => void;
  setQuantity: (key: string, quantity: number) => void;
  setSize: (key: string, size: string) => void;
  remove: (key: string) => void;
  clear: () => void;
  has: (productId: string) => boolean;
};

const CartContext = createContext<CartContextValue | null>(null);

function load(): CartItem[] {
  try {
    const raw = localStorage.getItem(KEY);
    const parsed = raw ? (JSON.parse(raw) as CartItem[]) : [];
    // Descarta productos que ya no existen.
    return parsed.filter((i) => productById(i.productId) && i.quantity > 0);
  } catch {
    return [];
  }
}

/**
 * Carrito global, guardado en localStorage para que sobreviva al recargar.
 * Al integrar la API los precios se vuelven a validar en el backend al crear el pedido.
 */
export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(load);
  const [drawerOpen, setDrawerOpen] = useState(false);
  // Sube cada vez que se recarga el catálogo, para recalcular existencias de los renglones.
  const [catalogVersion, setCatalogVersion] = useState(0);
  useEffect(() => {
    const onCatalog = () => setCatalogVersion((n) => n + 1);
    window.addEventListener(CATALOG_EVENT, onCatalog);
    return () => window.removeEventListener(CATALOG_EVENT, onCatalog);
  }, []);

  useEffect(() => {
    try {
      localStorage.setItem(KEY, JSON.stringify(items));
    } catch {
      /* el carrito vive en memoria */
    }
  }, [items]);

  // Sincroniza el carrito entre pestañas abiertas.
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key === KEY) setItems(load());
    };
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, []);

  /** Agrega piezas (1 por defecto). Con tallas elige la indicada o la primera con existencias; sin existencias no agrega. */
  const add = useCallback((product: Product, size?: string, quantity = 1) => {
    const chosen = product.sizes ? (size ?? firstAvailableSize(product) ?? product.sizes[0]) : undefined;
    const max = availableFor(product, chosen);
    if (max <= 0) return;
    setItems((prev) => {
      const key = lineKey({ productId: product.id, size: chosen });
      const found = prev.find((i) => lineKey(i) === key);
      if (found) return prev.map((i) => (i === found ? { ...i, quantity: Math.min(max, i.quantity + quantity) } : i));
      return [...prev, { productId: product.id, size: chosen, quantity: Math.min(max, quantity) }];
    });
  }, []);

  const setQuantity = useCallback((key: string, quantity: number) => {
    setItems((prev) =>
      prev.map((i) => {
        if (lineKey(i) !== key) return i;
        const product = productById(i.productId);
        const max = product ? availableFor(product, i.size) : Infinity;
        return { ...i, quantity: Math.max(1, Math.min(quantity, max === Infinity ? quantity : Math.max(1, max))) };
      }),
    );
  }, []);

  const setSize = useCallback((key: string, size: string) => {
    setItems((prev) => {
      const current = prev.find((i) => lineKey(i) === key);
      if (!current) return prev;
      const target = prev.find((i) => i !== current && lineKey(i) === lineKey({ productId: current.productId, size }));
      // Si ya existe esa talla, se juntan las cantidades.
      if (target) {
        return prev
          .filter((i) => i !== current)
          .map((i) => (i === target ? { ...i, quantity: i.quantity + current.quantity } : i));
      }
      return prev.map((i) => (i === current ? { ...i, size } : i));
    });
  }, []);

  const remove = useCallback((key: string) => setItems((prev) => prev.filter((i) => lineKey(i) !== key)), []);
  const clear = useCallback(() => setItems([]), []);

  const value = useMemo<CartContextValue>(() => {
    const lines: CartLineView[] = items.flatMap((i) => {
      const product = productById(i.productId);
      if (!product) return [];
      const max = availableFor(product, i.size);
      return [{ ...i, key: lineKey(i), product, lineTotal: product.price * i.quantity, max, short: i.quantity > max }];
    });
    return {
      items,
      lines,
      count: lines.reduce((s, l) => s + l.quantity, 0),
      subtotal: lines.reduce((s, l) => s + l.lineTotal, 0),
      drawerOpen,
      openDrawer: () => setDrawerOpen(true),
      closeDrawer: () => setDrawerOpen(false),
      add,
      setQuantity,
      setSize,
      remove,
      clear,
      has: (productId) => items.some((i) => i.productId === productId),
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- catalogVersion fuerza el recálculo con el catálogo nuevo
  }, [items, drawerOpen, add, setQuantity, setSize, remove, clear, catalogVersion]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart debe usarse dentro de <CartProvider>.');
  return ctx;
}

/** Datos de existencias de un renglón para la interfaz (tallas agotadas y aviso). */
export function stockView(l: CartLineView): { soldOutSizes?: string[]; max?: number; warning?: string } {
  const soldOutSizes = l.product.sizeStock ? Object.entries(l.product.sizeStock).filter(([, n]) => n <= 0).map(([t]) => t) : undefined;
  const warning = !l.short
    ? undefined
    : l.max <= 0
      ? l.size
        ? `La talla ${l.size} se agotó. Elige otra o elimínala.`
        : 'Se agotó. Elimínalo para continuar.'
      : `Solo quedan ${l.max} ${l.max === 1 ? 'pieza' : 'piezas'}${l.size ? ` en talla ${l.size}` : ''}.`;
  return { soldOutSizes, max: Number.isFinite(l.max) ? l.max : undefined, warning };
}
