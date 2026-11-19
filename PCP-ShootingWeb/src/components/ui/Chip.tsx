import type { ComponentPropsWithRef, ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './Chip.module.css';

type ChipProps = Omit<ComponentPropsWithRef<'button'>, 'onClick'> & {
  selected: boolean;
  onClick: () => void;
};

/** Filtro tipo píldora (categorías de la tienda, estados de pedidos). */
export function Chip({ selected, className, children, ...rest }: ChipProps) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      className={cx(styles.chip, selected && styles.selected, className)}
      {...rest}
    >
      {children}
    </button>
  );
}

/** Agrupa chips: en celular hace scroll horizontal; en pantallas grandes se acomodan en filas. */
export function ChipGroup({ label, children, className }: { label: string; children: ReactNode; className?: string }) {
  return (
    <div role="group" aria-label={label} className={cx(styles.group, className)}>
      {children}
    </div>
  );
}
