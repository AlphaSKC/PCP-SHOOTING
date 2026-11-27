import { Link } from 'react-router';
import { cx } from '@/lib/format';
import type { StoreCategory } from '../categoriasApi';
import type { ProductBrand } from '../types';
import styles from './StoreNav.module.css';

const plural = (n: number) => `${n} producto${n === 1 ? '' : 's'}`;

/** Botones grandes con foto: una tarjeta por categoría de la tienda (F24). */
export function CategoryTiles({ categories }: { categories: StoreCategory[] }) {
  return (
    <ul className={styles.categories}>
      {categories.map((c) => (
        <li key={c.id}>
          <Link to={`/tienda/${c.slug}`} className={styles.category}>
            {/* Sin foto queda el fondo negro de la marca con el nombre encima. */}
            <span className={styles.categoryMedia}>{c.imageUrl && <img src={c.imageUrl} alt="" loading="lazy" />}</span>
            <span className={styles.categoryText}>
              <strong>{c.name}</strong>
              <span>{c.count > 0 ? plural(c.count) : 'Próximamente'}</span>
            </span>
          </Link>
        </li>
      ))}
    </ul>
  );
}

export type BrandTile = ProductBrand & { count: number };

/** Logos de las marcas de una categoría; la elegida queda marcada. «Todas» regresa a la categoría completa. */
export function BrandTiles({ categorySlug, brands, active, total }: { categorySlug: string; brands: BrandTile[]; active?: string; total: number }) {
  return (
    <ul className={styles.brands} aria-label="Marcas">
      <li>
        <Link to={`/tienda/${categorySlug}`} className={cx(styles.brand, styles.brandAll, !active && styles.brandOn)} aria-current={!active ? 'page' : undefined}>
          <strong>Todas</strong>
          <span>{plural(total)}</span>
        </Link>
      </li>
      {brands.map((b) => (
        <li key={b.id}>
          <Link
            to={`/tienda/${categorySlug}/${b.slug}`}
            className={cx(styles.brand, active === b.slug && styles.brandOn)}
            aria-current={active === b.slug ? 'page' : undefined}
          >
            {b.logoUrl ? <img src={b.logoUrl} alt={b.name} loading="lazy" className={styles.logo} /> : <strong className={styles.brandName}>{b.name}</strong>}
            <span>{plural(b.count)}</span>
          </Link>
        </li>
      ))}
    </ul>
  );
}
