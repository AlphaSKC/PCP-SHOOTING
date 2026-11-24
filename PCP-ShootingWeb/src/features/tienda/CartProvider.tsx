import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { productById } from './api';
import type { CartItem, Product } from './types';

const KEY = 'pcp.carrito';

/** Clave de renglón: producto + talla (la misma playera en otra talla es otro renglón). */
export const lineKey = (i: Pick<CartItem, 'productId' | 'size'>) => `${i.productId}:${i.size ?? ''}`;

export type CartLineView = CartItem & { key: string; product: Product; lineTotal: number };

type CartContextValue = {
  items: CartItem[];
  lines: CartLineView[];
  count: number;
  subtotal: number;
  /** Panel lateral del carrito (escritorio y tableta). */
  drawerOpen: boolean;
  openDrawer: () => void;
  closeDrawer: () => void;
  add: (product: Product, size?: string) => void;
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

  const add = useCallback((product: Product, size?: string) => {
    const chosen = product.sizes ? (size ?? (product.sizes.includes('M') ? 'M' : product.sizes[0])) : undefined;
    setItems((prev) => {
      const key = lineKey({ productId: product.id, size: chosen });
      const found = prev.find((i) => lineKey(i) === key);
      if (found) return prev.map((i) => (i === found ? { ...i, quantity: i.quantity + 1 } : i));
      return [...prev, { productId: product.id, size: chosen, quantity: 1 }];
    });
  }, []);

  const setQuantity = useCallback((key: string, quantity: number) => {
    setItems((prev) => prev.map((i) => (lineKey(i) === key ? { ...i, quantity: Math.max(1, quantity) } : i)));
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
      return product ? [{ ...i, key: lineKey(i), product, lineTotal: product.price * i.quantity }] : [];
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
  }, [items, drawerOpen, add, setQuantity, setSize, remove, clear]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart debe usarse dentro de <CartProvider>.');
  return ctx;
}
