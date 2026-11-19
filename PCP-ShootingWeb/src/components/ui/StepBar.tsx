import { IconCheck } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './StepBar.module.css';

type Props = {
  steps: string[];
  /** Índice del paso actual (0 = primero). */
  current: number;
  label?: string;
};

/**
 * Progreso del wizard (va sobre fondo negro).
 * Escritorio/tableta: píldoras numeradas. Celular: barras + "Paso 2 de 4 · Fecha y línea".
 */
export function StepBar({ steps, current, label = 'Pasos de la reserva' }: Props) {
  return (
    <>
      <ol aria-label={label} className={styles.pills}>
        {steps.map((step, i) => {
          const state = i < current ? 'done' : i === current ? 'active' : 'todo';
          return (
            <li key={step} className={cx(styles.pill, styles[state])} aria-current={state === 'active' ? 'step' : undefined}>
              <span className={styles.dot}>{state === 'done' ? <IconCheck size={14} strokeWidth={3} /> : i + 1}</span>
              {step}
              {state === 'done' && <span className="visually-hidden"> (completado)</span>}
            </li>
          );
        })}
      </ol>

      <div className={styles.compact} role="group" aria-label={label}>
        <div className={styles.bars} aria-hidden="true">
          {steps.map((step, i) => (
            <span key={step} className={cx(styles.bar, i <= current && styles.barOn)} />
          ))}
        </div>
        <p className={styles.caption}>
          <strong>
            Paso {current + 1} de {steps.length}
          </strong>{' '}
          · {steps[current]}
        </p>
      </div>
    </>
  );
}
