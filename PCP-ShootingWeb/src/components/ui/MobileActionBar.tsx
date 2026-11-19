import type { ReactNode } from 'react';
import { formatMXN } from '@/lib/format';
import styles from './MobileActionBar.module.css';

type Props = {
  /** "Total · 2 productos" */
  label: string;
  amount: number;
  /** Botón principal (a todo lo ancho). */
  children: ReactNode;
};

/**
 * Barra fija inferior SOLO en celular: total + botón principal (MCarrito, MCheckout).
 * En tableta/escritorio no se muestra (la acción vive en la columna de resumen).
 * La página debe dejar espacio abajo (≈140px) para no tapar contenido.
 */
export function MobileActionBar({ label, amount, children }: Props) {
  return (
    <div className={styles.bar}>
      <div className={styles.total}>
        <span>{label}</span>
        <strong>{formatMXN(amount)}</strong>
      </div>
      {children}
    </div>
  );
}
