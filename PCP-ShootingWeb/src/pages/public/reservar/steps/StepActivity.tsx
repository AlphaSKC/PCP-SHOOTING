import { useState } from 'react';
import { Button, ChoiceCard, FormAlert, QuantityStepper } from '@/components/ui';
import { MAX_PEOPLE, MIN_PEOPLE } from '@/features/reservas/config';
import type { Equipment, ReservationDraft, ShootingPackage } from '@/features/reservas/types';
import { formatMXN } from '@/lib/format';
import { WizardActions } from '@/features/reservas/components/WizardActions';
import { cx } from '@/lib/format';
import styles from '../ReservarPage.module.css';

type Props = {
  packages: ShootingPackage[];
  draft: ReservationDraft;
  update: (c: Partial<ReservationDraft>) => void;
  onNext: () => void;
  summary: string;
  total: number;
};

const EQUIPMENT: { id: Equipment; label: string; short: string; hint: string }[] = [
  { id: 'renta', label: 'Rentar rifle PCP + diábolos', short: 'Rentar rifle', hint: 'Incluye diábolos' },
  { id: 'propio', label: 'Traigo mi propio rifle', short: 'Traigo el mío', hint: 'Rifle PCP propio' },
];

export function StepActivity({ packages, draft, update, onNext, summary, total }: Props) {
  const [error, setError] = useState<string | null>(null);
  const selected = packages.find((p) => p.id === draft.packageId);

  const next = () => {
    if (!selected) return setError('Elige una actividad para continuar.');
    setError(null);
    onNext();
  };

  return (
    <section className={styles.step} aria-labelledby="paso-1">
      <div className={styles.stepHead}>
        <h2 id="paso-1" className={styles.stepTitle}>
          <span className={styles.desktopInline}>1. Elige tu modo o actividad</span>
          <span className={styles.mobileInline}>Elige tu actividad</span>
        </h2>
        <p className={cx(styles.stepLead, styles.desktopBlock)}>Cada actividad incluye tu línea de tiro asignada.</p>
      </div>

      {error && <FormAlert>{error}</FormAlert>}

      <div role="radiogroup" aria-labelledby="paso-1" className={styles.packages}>
        {packages.map((p) => (
          <ChoiceCard
            key={p.id}
            selected={p.id === draft.packageId}
            onSelect={() => update({ packageId: p.id, equipment: p.rentalAvailable ? draft.equipment : 'propio' })}
            title={p.name}
            description={p.description}
            meta={`${p.includes} · ${formatMXN(p.pricePerPerson).replace(' MXN', '')} por persona`}
          />
        ))}
      </div>

      <div className={styles.options}>
        <fieldset className={styles.fieldset}>
          <legend className={styles.legend}>Equipo</legend>
          <div role="radiogroup" aria-label="Equipo" className={styles.equipment}>
            {EQUIPMENT.map((e) => {
              const disabled = e.id === 'renta' && selected ? !selected.rentalAvailable : false;
              const active = draft.equipment === e.id;
              return (
                <button
                  key={e.id}
                  type="button"
                  role="radio"
                  aria-checked={active}
                  disabled={disabled}
                  className={cx(styles.toggle, active && styles.toggleActive)}
                  onClick={() => update({ equipment: e.id })}
                >
                  <span className={styles.desktopInline}>{e.label}</span>
                  <span className={styles.mobileBlock}>
                    <strong>{e.short}</strong>
                    <small>{e.hint}</small>
                  </span>
                </button>
              );
            })}
          </div>
        </fieldset>

        <div className={styles.people}>
          <span id="personas" className={styles.legend}>
            Personas
          </span>
          <QuantityStepper
            label="personas"
            value={draft.people}
            min={MIN_PEOPLE}
            max={MAX_PEOPLE}
            onChange={(people) => update({ people })}
          />
        </div>
      </div>

      <WizardActions
        summary={summary}
        total={total}
        primary={
          <Button size="lg" onClick={next}>
            Continuar: fecha y línea
          </Button>
        }
      />
    </section>
  );
}
