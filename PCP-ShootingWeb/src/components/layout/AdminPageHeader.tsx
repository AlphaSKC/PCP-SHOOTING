import type { ReactNode } from 'react';
import styles from './AdminPageHeader.module.css';

type Props = {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
};

/**
 * Encabezado de cada pantalla del panel.
 * Escritorio: barra blanca con título y acciones. Celular/tableta: título dentro del contenido.
 */
export function AdminPageHeader({ title, subtitle, actions }: Props) {
  return (
    <header className={styles.header}>
      <div className={styles.text}>
        <h1 className={styles.title}>{title}</h1>
        {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
      </div>
      {actions && <div className={styles.actions}>{actions}</div>}
    </header>
  );
}
