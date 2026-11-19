import type { ComponentPropsWithRef, ElementType, ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './Card.module.css';

type CardProps = ComponentPropsWithRef<'div'> & {
  as?: ElementType;
  /** none: sin padding (p. ej. tarjetas con foto) · md: 20px · lg: 24px (se reduce en celular) */
  padding?: 'none' | 'md' | 'lg';
};

/** Tarjeta blanca con borde #E3E3E3 y radio 16px. */
export function Card({ as: Tag = 'div', padding = 'lg', className, ...rest }: CardProps) {
  return <Tag className={cx(styles.card, styles[`pad-${padding}`], className)} {...rest} />;
}

/** Encabezado de tarjeta: título en mayúsculas + acción opcional (p. ej. "Ver todos"). */
export function CardHeader({ title, action, as: Heading = 'h2' }: { title: string; action?: ReactNode; as?: 'h2' | 'h3' }) {
  return (
    <div className={styles.header}>
      <Heading className={styles.title}>{title}</Heading>
      {action}
    </div>
  );
}
