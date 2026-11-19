import { useState } from 'react';
import { Button, FormAlert } from '@/components/ui';
import { PaymentMethods, PaymentRedirect } from '@/features/pagos/PaymentMethods';
import { ReservationError, createReservation, simulatePayment } from '@/features/reservas/api';
import { ReservationSummary, type SummaryData } from '@/features/reservas/components/ReservationSummary';
import { WizardActions } from '@/features/reservas/components/WizardActions';
import type { Reservation, ReservationDraft } from '@/features/reservas/types';
import { formatMXN } from '@/lib/format';
import styles from '../ReservarPage.module.css';

type Props = {
  draft: ReservationDraft;
  summaryData: SummaryData;
  onBack: () => void;
  onLineTaken: () => void;
  onPaid: (reservation: Reservation) => void;
  summary: string;
  total: number;
};

/**
 * Pago en línea. La plataforma NO captura ni guarda datos de tarjeta (CLAUDE.md §6):
 * se crea la reserva en `pendiente_pago` y se redirige a Mercado Pago (Checkout Pro).
 * La reserva se confirma cuando la API recibe el webhook del pago. Aquí se simula todo el flujo.
 */
export function StepPayment({ draft, summaryData, onBack, onLineTaken, onPaid, summary, total }: Props) {
  const [phase, setPhase] = useState<'idle' | 'creating' | 'redirecting'>('idle');
  const [error, setError] = useState<{ message: string; lineTaken: boolean } | null>(null);

  const pay = async () => {
    setError(null);
    setPhase('creating');
    try {
      const reservation = await createReservation(draft);
      // TODO(API): redirigir a `init_point` de la preferencia de Mercado Pago.
      setPhase('redirecting');
      onPaid(await simulatePayment(reservation.id));
    } catch (e) {
      setPhase('idle');
      const lineTaken = e instanceof ReservationError && e.message.includes('línea');
      setError({
        message: e instanceof ReservationError ? e.message : 'No pudimos iniciar el pago. Intenta de nuevo.',
        lineTaken,
      });
    }
  };

  return (
    <section className={styles.step} aria-labelledby="paso-4">
      <h2 id="paso-4" className={`${styles.stepTitle} ${styles.desktopBlock}`}>
        4. Pago en línea
      </h2>

      <ReservationSummary data={summaryData} className={styles.mobileSummary} />

      <h3 className={`${styles.stepTitle} ${styles.mobileBlock}`}>Método de pago</h3>

      {error && (
        <FormAlert>
          {error.message}{' '}
          {error.lineTaken && (
            <button type="button" className={styles.inlineAction} onClick={onLineTaken}>
              Elegir otra línea
            </button>
          )}
        </FormAlert>
      )}

      <PaymentMethods intro="Pagas el 100% de la reserva en Mercado Pago, con el método que prefieras:" />

      {phase === 'redirecting' && <PaymentRedirect />}

      <WizardActions
        summary={summary}
        total={total}
        totalLabel="Total a pagar"
        onBack={phase === 'idle' ? onBack : undefined}
        primary={
          <Button size="lg" onClick={pay} loading={phase !== 'idle'}>
            Pagar {formatMXN(total).replace(' MXN', '')} y confirmar
          </Button>
        }
      />
    </section>
  );
}
