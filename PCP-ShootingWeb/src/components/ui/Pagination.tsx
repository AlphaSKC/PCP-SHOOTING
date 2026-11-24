import { cx } from '@/lib/format';
import styles from './Pagination.module.css';

type Props = {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
  label?: string;
};

/** Paginación numerada con "Anterior" / "Siguiente". No se muestra si hay una sola página. */
export function Pagination({ page, totalPages, onChange, label = 'Paginación' }: Props) {
  if (totalPages <= 1) return null;
  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);

  return (
    <nav aria-label={label} className={styles.nav}>
      {page > 1 && (
        <button type="button" className={cx(styles.item, styles.wide)} onClick={() => onChange(page - 1)}>
          Anterior
        </button>
      )}
      {pages.map((p) => (
        <button
          key={p}
          type="button"
          className={cx(styles.item, p === page && styles.current)}
          aria-current={p === page ? 'page' : undefined}
          aria-label={`Página ${p}`}
          onClick={() => onChange(p)}
        >
          {p}
        </button>
      ))}
      {page < totalPages && (
        <button type="button" className={cx(styles.item, styles.wide)} onClick={() => onChange(page + 1)}>
          Siguiente
        </button>
      )}
    </nav>
  );
}
