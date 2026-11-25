import type { ReactNode } from 'react';
import { formatMXN } from '@/lib/format';
import { Photo } from './Photo';
import styles from './ProductCard.module.css';

export type ProductCardData = {
  name: string;
  /** Categoría visible (Playera, Gorra, Taza…). */
  category: string;
  description: string;
  price: number;
  imageUrl?: string | null;
  /** Sin existencias: botón deshabilitado y etiqueta «Agotado». */
  soldOut?: boolean;
  /** Aviso de pocas piezas («Quedan 2»). */
  stockNote?: string;
  /** Tallas con su disponibilidad (playeras). */
  sizes?: { label: string; out: boolean }[];
};

type Props = {
  product: ProductCardData;
  /** Ya está en el carrito: el botón cambia a negro ("Agregado"). */
  added: boolean;
  onAdd: () => void;
};

/** Rejilla de productos: columnas de 280px+ en escritorio/tableta, 2 columnas en celular. */
export function ProductGrid({ children }: { children: ReactNode }) {
  return <div className={styles.grid}>{children}</div>;
}

/**
 * Tarjeta de producto de la tienda. Se adapta al ANCHO DE LA TARJETA (container query):
 * ancha = versión escritorio (Main); angosta = versión celular de 2 columnas (MTienda).
 */
export function ProductCard({ product, added, onAdd }: Props) {
  const { name, category, description, price, imageUrl, soldOut, stockNote, sizes } = product;
  return (
    <article className={styles.card}>
      <div className={styles.inner}>
        <div className={styles.media}>
          <Photo src={imageUrl} alt={`Foto: ${name}`} />
          {soldOut && <span className={styles.soldOut}>Agotado</span>}
        </div>
        <div className={styles.body}>
          <span className={styles.category}>{category}</span>
          <h3 className={styles.name}>{name}</h3>
          <p className={styles.description} title={description}>
            {description}
          </p>
          {sizes && sizes.length > 0 && (
            <p className={styles.sizes} aria-label="Tallas">
              {sizes.map((t) => (
                <span key={t.label} className={t.out ? styles.sizeOut : styles.size} title={t.out ? `Talla ${t.label} agotada` : `Talla ${t.label}`}>
                  {t.label}
                </span>
              ))}
            </p>
          )}
          {!soldOut && stockNote && <p className={styles.stockNote}>{stockNote}</p>}
          <div className={styles.footer}>
            <span className={styles.price}>{formatMXN(price)}</span>
            {soldOut ? (
              <button type="button" className={styles.soldOutButton} disabled aria-label={`${name} agotado`}>
                Agotado
              </button>
            ) : (
              <button
                type="button"
                className={added ? styles.addedButton : styles.addButton}
                onClick={onAdd}
                aria-label={added ? `${name} agregado. Agregar otro` : `Agregar ${name} al carrito`}
              >
                <span className={styles.labelLong}>{added ? 'Agregado · agregar otro' : 'Agregar al carrito'}</span>
                <span className={styles.labelShort}>{added ? 'Agregado ✓' : 'Agregar'}</span>
              </button>
            )}
          </div>
        </div>
      </div>
    </article>
  );
}
