import { Link } from 'react-router';
import { formatMXN, cx } from '@/lib/format';
import { equipmentLabel, shortDay } from '../format';
import type { Equipment } from '../types';
import styles from './ReservationSummary.module.css';

export type SummaryData = {
  packageName?: string;
  /** Solo en demo (la API no maneja equipo). */
  equipment?: Equipment;
  people: number;
  date: string | null;
  time: string | null;
  line?: number | null;
  /** API: una línea por persona. */
  lines?: number[];
  total: number;
  /** Texto en lugar del total (p. ej. "Precio pendiente"). */
  totalText?: string;
};

type Props = {
  data: SummaryData;
  title?: string;
  /** Muestra el aviso de membresía (solo a quien no es socio). */
  showMemberNote?: boolean;
  totalLabel?: string;
  className?: string;
};

const dash = '—';

/** Tarjeta "Tu reserva" con el resumen y el total. */
export function ReservationSummary({ data, title = 'Tu reserva', showMemberNote, totalLabel = 'Total', className }: Props) {
  const lineText = data.lines
    ? data.lines.length
      ? `${data.lines.length === 1 ? 'Línea' : 'Líneas'} ${[...data.lines].sort((a, b) => a - b).join(', ')}`
      : dash
    : data.line
      ? `Línea ${data.line}`
      : dash;
  const rows: [string, string][] = [
    ['Actividad', data.packageName ?? dash],
    ...(data.equipment ? ([['Equipo', equipmentLabel(data.equipment)]] as [string, string][]) : []),
    ['Personas', String(data.people)],
    ['Fecha', data.date ? shortDay(data.date) : dash],
    ['Hora', data.time ?? dash],
    [data.lines && data.lines.length > 1 ? 'Líneas de tiro' : 'Línea de tiro', lineText],
  ];

  return (
    <section className={cx(styles.summary, className)} aria-label="Resumen de tu reserva">
      <h2 className={styles.title}>{title}</h2>
      <dl className={styles.rows}>
        {rows.map(([label, value]) => (
          <div key={label} className={styles.row}>
            <dt>{label}</dt>
            <dd>{value}</dd>
          </div>
        ))}
      </dl>
      <div className={styles.total}>
        <span>{totalLabel}</span>
        <strong>{data.totalText ?? formatMXN(data.total).replace(' MXN', '')}</strong>
      </div>
      {showMemberNote && (
        <p className={styles.note}>
          <strong>Socios con membresía:</strong> precio preferente en cada reserva.{' '}
          <Link to="/membresias">Conoce la membresía</Link>
        </p>
      )}
    </section>
  );
}
