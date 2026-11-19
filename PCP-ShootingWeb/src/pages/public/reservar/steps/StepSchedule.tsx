import { useEffect, useState } from 'react';
import { Button, FormAlert, LineMap, type ShootingLine } from '@/components/ui';
import { getDays, getLines } from '@/features/reservas/api';
import { TOTAL_LINES } from '@/features/reservas/config';
import { DayPicker, TimePicker } from '@/features/reservas/components/SlotPickers';
import { WizardActions } from '@/features/reservas/components/WizardActions';
import type { ReservationDraft } from '@/features/reservas/types';
import { useAsync } from '@/features/reservas/useAsync';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from '../ReservarPage.module.css';

type Props = {
  draft: ReservationDraft;
  update: (c: Partial<ReservationDraft>) => void;
  onBack: () => void;
  onNext: () => void;
  summary: string;
  total: number;
};

export function StepSchedule({ draft, update, onBack, onNext, summary, total }: Props) {
  const [error, setError] = useState<string | null>(null);
  const days = useAsync(() => getDays(draft.packageId), [draft.packageId]);
  const day = days.data?.find((d) => d.date === draft.date);

  // Al cargar la disponibilidad, descarta un día u horario guardado que ya no existe o se llenó.
  useEffect(() => {
    if (!days.data) return;
    if (draft.date && !day) {
      update({ date: null, time: null, line: null });
    } else if (day && draft.time && !day.slots.some((s) => s.time === draft.time && !s.full)) {
      update({ time: null, line: null });
    }
  }, [days.data, day, draft.date, draft.time, update]);

  const lines = useAsync(
    draft.date && draft.time ? () => getLines(draft.date!, draft.time!) : null,
    [draft.date, draft.time],
  );

  const lineList: ShootingLine[] = Array.from({ length: TOTAL_LINES }, (_, i) => ({
    number: i + 1,
    status: lines.data?.occupied.includes(i + 1) ? 'occupied' : 'available',
  }));

  // Si la línea elegida quedó ocupada en el nuevo horario, se descarta.
  const lineStillFree = draft.line != null && lines.data && !lines.data.occupied.includes(draft.line);

  const next = () => {
    if (!draft.date) return setError('Elige un día.');
    if (!draft.time) return setError('Elige un horario.');
    if (!draft.line || !lineStillFree) return setError('Elige una línea de tiro disponible.');
    setError(null);
    onNext();
  };

  if (days.loading && !days.data) return <LoadingScreen />;

  return (
    <section className={styles.step} aria-labelledby="paso-2">
      <h2 id="paso-2" className={styles.stepTitle}>
        <span className={styles.desktopInline}>2. Fecha, hora y línea de tiro</span>
        <span className={styles.mobileInline}>Fecha, hora y línea</span>
      </h2>

      {error && <FormAlert>{error}</FormAlert>}

      <div className={styles.group}>
        <h3 className={styles.groupTitle}>Día</h3>
        <DayPicker
          days={days.data ?? []}
          value={draft.date}
          onChange={(date) => {
            const slot = days.data?.find((d) => d.date === date)?.slots.find((s) => s.time === draft.time);
            update({ date, time: slot && !slot.full ? draft.time : null, line: null });
          }}
        />
      </div>

      {day && (
        <div className={styles.group}>
          <h3 className={styles.groupTitle}>Hora</h3>
          <TimePicker slots={day.slots} value={draft.time} onChange={(time) => update({ time, line: null })} />
        </div>
      )}

      {draft.date && draft.time && (
        <div className={styles.group} aria-busy={lines.loading}>
          <LineMap lines={lineList} selected={lineStillFree ? draft.line : null} onSelect={(line) => update({ line })} />
        </div>
      )}

      <WizardActions
        summary={summary}
        total={total}
        onBack={onBack}
        primary={
          <Button size="lg" onClick={next}>
            Continuar: datos
          </Button>
        }
      />
    </section>
  );
}
