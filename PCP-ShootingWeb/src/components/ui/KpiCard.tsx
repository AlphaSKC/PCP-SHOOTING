import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './KpiCard.module.css';

type Props = {
  label: string;
  value: string;
  hint?: string;
  /** Borde superior rojo para el indicador principal. */
  highlight?: boolean;
};

/** Indicador del panel (Reservas hoy, Ventas del día…). */
export function KpiCard({ label, value, hint, highlight = false }: Props) {
  return (
    <div className={cx(styles.kpi, highlight && styles.highlight)}>
      <span className={styles.label}>{label}</span>
      <span className={styles.value}>{value}</span>
      {hint && <span className={styles.hint}>{hint}</span>}
    </div>
  );
}

/** Rejilla de KPIs: 4 columnas en escritorio, 2 en celular. */
export function KpiGrid({ children }: { children: ReactNode }) {
  return <div className={styles.grid}>{children}</div>;
}
