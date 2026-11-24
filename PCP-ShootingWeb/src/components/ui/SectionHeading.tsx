import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './SectionHeading.module.css';

type Props = {
  eyebrow?: string;
  title: string;
  /** Acción a la derecha (se oculta en celular si `hideActionOnMobile`). */
  action?: ReactNode;
  hideActionOnMobile?: boolean;
  id?: string;
  className?: string;
};

/** Encabezado de sección sobre fondo claro: eyebrow rojo oscuro + título en mayúsculas. */
export function SectionHeading({ eyebrow, title, action, hideActionOnMobile, id, className }: Props) {
  return (
    <div className={cx(styles.heading, className)}>
      <div className={styles.text}>
        {eyebrow && <span className={styles.eyebrow}>{eyebrow}</span>}
        <h2 id={id} className={styles.title}>
          {title}
        </h2>
      </div>
      {action && <div className={cx(styles.action, hideActionOnMobile && styles.hideOnMobile)}>{action}</div>}
    </div>
  );
}
