import { IconCheck } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import { FOOD_STATUS_FLOW, type FoodOrderStatus } from './api';
import styles from './StatusTracker.module.css';

/** Seguimiento del pedido: recibido → en preparación → listo → entregado. */
export function StatusTracker({ status }: { status: FoodOrderStatus }) {
  const current = FOOD_STATUS_FLOW.findIndex((s) => s.id === status);
  return (
    <ol className={styles.tracker} aria-label="Estado del pedido">
      {FOOD_STATUS_FLOW.map((s, i) => {
        const state = i < current ? 'done' : i === current ? 'current' : 'todo';
        return (
          <li key={s.id} className={cx(styles.step, styles[state])} aria-current={state === 'current' ? 'step' : undefined}>
            <span className={styles.dot} aria-hidden="true">
              {state === 'done' ? <IconCheck size={14} strokeWidth={3} /> : i + 1}
            </span>
            <span className={styles.label}>{s.label}</span>
          </li>
        );
      })}
    </ol>
  );
}
