import { cx } from '@/lib/format';
import styles from './LineMap.module.css';

export type ShootingLine = {
  number: number;
  status: 'available' | 'occupied';
};

type Props = {
  lines: ShootingLine[];
  selected: number | null;
  onSelect: (line: number) => void;
  /** Título visible sobre el mapa. */
  title?: string;
};

/**
 * Mapa de las 15 líneas de tiro. El cliente elige UNA línea disponible
 * (no hay asignación automática; la API valida la disponibilidad).
 */
export function LineMap({ lines, selected, onSelect, title = 'Línea de tiro' }: Props) {
  return (
    <div className={styles.wrap}>
      <div className={styles.top}>
        <h3 className={styles.title}>{title}</h3>
        <ul className={styles.legend} aria-label="Leyenda">
          <li>
            <span className={cx(styles.swatch, styles.swatchFree)} aria-hidden="true" />
            Disponible
          </li>
          <li>
            <span className={cx(styles.swatch, styles.swatchSelected)} aria-hidden="true" />
            Tu línea
          </li>
          <li>
            <span className={cx(styles.swatch, styles.swatchBusy)} aria-hidden="true" />
            Ocupada
          </li>
        </ul>
      </div>

      <div className={styles.range}>
        <div className={styles.targets} aria-hidden="true">
          Zona de blancos
        </div>
        <div role="group" aria-label={title} className={styles.grid}>
          {lines.map(({ number, status }) => {
            const busy = status === 'occupied';
            const isSelected = !busy && number === selected;
            return (
              <button
                key={number}
                type="button"
                disabled={busy}
                aria-pressed={busy ? undefined : isSelected}
                aria-label={`Línea ${number}, ${busy ? 'ocupada' : isSelected ? 'seleccionada' : 'disponible'}`}
                className={cx(styles.line, busy ? styles.busy : isSelected ? styles.selected : styles.free)}
                onClick={() => onSelect(number)}
              >
                <span className={styles.kicker} aria-hidden="true">
                  {busy ? 'Ocupada' : 'Línea'}
                </span>
                <span className={styles.number} aria-hidden="true">
                  {number}
                </span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
