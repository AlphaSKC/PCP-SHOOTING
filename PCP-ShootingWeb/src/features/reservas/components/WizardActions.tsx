import type { ReactNode } from 'react';
import { Button } from '@/components/ui';
import { formatMXN } from '@/lib/format';
import styles from './WizardActions.module.css';

type Props = {
  /** Texto del resumen en la barra de celular ("Recreativo · 2 pers. · Lun 4 ene"). */
  summary: string;
  total: number;
  totalLabel?: string;
  onBack?: () => void;
  /** Botón principal: se le pasa ya armado (puede ser submit de un formulario). */
  primary: ReactNode;
};

/**
 * Botones de cada paso. Escritorio/tableta: al pie de la tarjeta.
 * Celular: barra fija abajo con resumen y total (MReserva1–4).
 */
export function WizardActions({ summary, total, totalLabel, onBack, primary }: Props) {
  return (
    <div className={styles.bar}>
      <div className={styles.summary} aria-hidden="true">
        <span className={styles.summaryText}>{totalLabel ?? summary}</span>
        <strong>{formatMXN(total).replace(' MXN', '')}</strong>
      </div>
      <div className={styles.buttons}>
        {onBack ? (
          <Button variant="secondary" size="lg" onClick={onBack} className={styles.back}>
            Atrás
          </Button>
        ) : (
          <span />
        )}
        {primary}
      </div>
    </div>
  );
}
