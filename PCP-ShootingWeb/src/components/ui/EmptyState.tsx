import type { ReactNode } from 'react';
import { IconInbox, type IconComponent } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './EmptyState.module.css';

type Props = {
  title: string;
  description?: ReactNode;
  icon?: IconComponent;
  /** Botón o enlace de acción (p. ej. "Ver productos"). */
  action?: ReactNode;
  className?: string;
};

/** Mensaje cuando no hay datos: carrito vacío, sin pedidos, búsqueda sin resultados. */
export function EmptyState({ title, description, icon: Icon = IconInbox, action, className }: Props) {
  return (
    <div className={cx(styles.empty, className)}>
      <Icon size={48} strokeWidth={1.5} className={styles.icon} />
      <p className={styles.title}>{title}</p>
      {description && <p className={styles.description}>{description}</p>}
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
