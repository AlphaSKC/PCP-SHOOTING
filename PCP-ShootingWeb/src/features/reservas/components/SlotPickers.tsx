import { cx } from '@/lib/format';
import type { DayAvailability, TimeSlot } from '../types';
import styles from './SlotPickers.module.css';

const dow = new Intl.DateTimeFormat('es-MX', { weekday: 'short', timeZone: 'UTC' });
const dayMonth = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', timeZone: 'UTC' });
const fullDay = new Intl.DateTimeFormat('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });

const clean = (s: string) => s.replace('.', '');

/** Selector de día (MAR · 5 ene). En celular se desliza horizontalmente. */
export function DayPicker({
  days,
  value,
  onChange,
}: {
  days: DayAvailability[];
  value: string | null;
  onChange: (date: string) => void;
}) {
  return (
    <div role="radiogroup" aria-label="Día" className={styles.days}>
      {days.map((d) => {
        const date = new Date(d.date);
        const selected = d.date === value;
        const allFull = d.slots.every((s) => s.full);
        return (
          <button
            key={d.date}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={`${fullDay.format(date)}${allFull ? ', sin horarios disponibles' : ''}`}
            disabled={allFull}
            className={cx(styles.day, selected && styles.daySelected)}
            onClick={() => onChange(d.date)}
          >
            <span className={styles.dow}>{clean(dow.format(date))}</span>
            <span className={styles.date}>{clean(dayMonth.format(date))}</span>
          </button>
        );
      })}
    </div>
  );
}

/** Selector de hora; los horarios llenos se muestran deshabilitados ("13:00 · Lleno"). */
export function TimePicker({
  slots,
  value,
  onChange,
}: {
  slots: TimeSlot[];
  value: string | null;
  onChange: (time: string) => void;
}) {
  return (
    <div role="radiogroup" aria-label="Hora" className={styles.times}>
      {slots.map((s) => {
        const selected = s.time === value;
        return (
          <button
            key={s.time}
            type="button"
            role="radio"
            aria-checked={selected}
            disabled={s.full}
            aria-label={s.full ? `${s.time}, lleno` : s.time}
            className={cx(styles.time, selected && styles.timeSelected, s.full && styles.timeFull)}
            onClick={() => onChange(s.time)}
          >
            {s.time}
            {s.full && <span className={styles.fullLabel}> · Lleno</span>}
          </button>
        );
      })}
    </div>
  );
}
