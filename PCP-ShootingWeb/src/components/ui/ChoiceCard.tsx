import type { ReactNode } from 'react';
import { IconCheck } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './ChoiceCard.module.css';

type Props = {
  selected: boolean;
  onSelect: () => void;
  title: string;
  description?: ReactNode;
  /** Línea destacada en rojo oscuro (precio, lo que incluye…). */
  meta?: ReactNode;
  /** Tamaño compacto (opciones de pago, equipo). */
  compact?: boolean;
  disabled?: boolean;
};

/**
 * Opción seleccionable tipo tarjeta (actividad del wizard, método de pago, plan…).
 * Escritorio: palomita en la esquina al seleccionar. Celular: círculo tipo radio a la izquierda (MReserva1).
 * Se usa dentro de un contenedor con role="radiogroup".
 */
export function ChoiceCard({ selected, onSelect, title, description, meta, compact = false, disabled }: Props) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      disabled={disabled}
      className={cx(styles.card, selected && styles.selected, compact && styles.compact)}
      onClick={onSelect}
    >
      <span className={styles.radio} aria-hidden="true">
        {selected && <IconCheck size={14} strokeWidth={3} />}
      </span>
      <span className={styles.body}>
        <span className={styles.head}>
          <span className={styles.title}>{title}</span>
          {selected && (
            <span className={styles.check} aria-hidden="true">
              <IconCheck size={14} strokeWidth={3} />
            </span>
          )}
        </span>
        {description && <span className={styles.description}>{description}</span>}
        {meta && <span className={styles.meta}>{meta}</span>}
      </span>
    </button>
  );
}
