import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './DataTable.module.css';

export type Column<T> = {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  align?: 'start' | 'end';
  /** Texto en negritas (p. ej. la hora o el folio). */
  strong?: boolean;
  /** En celular, esta columna es el título de la tarjeta. */
  primary?: boolean;
  /** En celular no se muestra. */
  hideOnMobile?: boolean;
};

type Props<T> = {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  /** Descripción para lectores de pantalla. */
  caption: string;
  /** Se muestra si no hay filas. */
  empty?: ReactNode;
  /** Clic en la fila (atajo de ratón; incluye también un botón accesible en una columna). */
  onRowClick?: (row: T) => void;
  /** Fila resaltada (la que se está editando). */
  selectedKey?: string | null;
};

/**
 * Tabla del panel. Escritorio/tableta: tabla con scroll horizontal si no cabe.
 * Celular: cada fila se convierte en tarjeta con "Etiqueta: valor" (como MAdminPedidos).
 */
export function DataTable<T>({ columns, rows, rowKey, caption, empty, onRowClick, selectedKey }: Props<T>) {
  if (rows.length === 0 && empty) return <>{empty}</>;

  return (
    <div className={styles.scroll}>
      <table className={styles.table}>
        <caption className="visually-hidden">{caption}</caption>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.key} scope="col" className={cx(col.align === 'end' && styles.end)}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              className={cx(onRowClick && styles.clickable, selectedKey === rowKey(row) && styles.rowSelected)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
            >
              {columns.map((col) => (
                <td
                  key={col.key}
                  data-label={col.header}
                  className={cx(
                    col.align === 'end' && styles.end,
                    col.strong && styles.strong,
                    col.primary && styles.primary,
                    col.hideOnMobile && styles.hideOnMobile,
                  )}
                >
                  {col.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
