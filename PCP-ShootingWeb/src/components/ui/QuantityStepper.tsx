import { IconMinus, IconPlus } from '@/components/icons/icons';
import styles from './QuantityStepper.module.css';

type Props = {
  value: number;
  onChange: (value: number) => void;
  /** Nombre de lo que se cuenta, para lectores de pantalla ("Playera PCP Negra", "personas"). */
  label: string;
  min?: number;
  max?: number;
};

/** Selector − n + (cantidad en el carrito, número de personas). */
export function QuantityStepper({ value, onChange, label, min = 1, max = 99 }: Props) {
  return (
    <div role="group" aria-label={`Cantidad de ${label}`} className={styles.stepper}>
      <button
        type="button"
        className={styles.button}
        aria-label="Quitar uno"
        disabled={value <= min}
        onClick={() => onChange(Math.max(min, value - 1))}
      >
        <IconMinus size={16} />
      </button>
      <output className={styles.value} aria-live="polite">
        {value}
      </output>
      <button
        type="button"
        className={styles.button}
        aria-label="Agregar uno"
        disabled={value >= max}
        onClick={() => onChange(Math.min(max, value + 1))}
      >
        <IconPlus size={16} />
      </button>
    </div>
  );
}
