import { useState } from 'react';
import styles from './BarChart.module.css';

type Point = { label: string; value: number };

/**
 * Barras de UNA serie (el título nombra la serie: no lleva leyenda).
 * Barras delgadas con extremo redondeado anclado a la base, 2px de separación,
 * eje y cuadrícula recesivos, y detalle al pasar el cursor o enfocar cada barra.
 * La tabla de datos acompaña siempre a la gráfica en la página (vista accesible).
 */
export function BarChart({ data, format, title }: { data: Point[]; format: (n: number) => string; title: string }) {
  const [active, setActive] = useState<number | null>(null);
  const max = Math.max(1, ...data.map((d) => d.value));
  const ticks = [0, 0.5, 1].map((t) => Math.round(max * t));

  return (
    <figure className={styles.figure} aria-label={title}>
      <div className={styles.plot}>
        <div className={styles.axis} aria-hidden="true">
          {[...ticks].reverse().map((t) => (
            <span key={t}>{format(t)}</span>
          ))}
        </div>
        <div className={styles.area} onMouseLeave={() => setActive(null)}>
          {[0, 0.5, 1].map((g) => (
            <span key={g} className={styles.grid} style={{ bottom: `${g * 100}%` }} aria-hidden="true" />
          ))}
          {data.map((d, i) => (
            <button
              key={d.label}
              type="button"
              className={styles.hit}
              aria-label={`${d.label}: ${format(d.value)}`}
              onMouseEnter={() => setActive(i)}
              onFocus={() => setActive(i)}
              onBlur={() => setActive(null)}
            >
              <span className={styles.bar} style={{ height: `${(d.value / max) * 100}%` }} data-active={active === i || undefined} />
            </button>
          ))}
          {active !== null && data[active] && (
            <div
              className={styles.tip}
              role="status"
              style={{ left: `${((active + 0.5) / data.length) * 100}%` }}
            >
              <strong>{format(data[active].value)}</strong>
              <span>{data[active].label}</span>
            </div>
          )}
        </div>
      </div>
      <figcaption className={styles.x} aria-hidden="true">
        <span>{data[0]?.label}</span>
        <span>{data[data.length - 1]?.label}</span>
      </figcaption>
    </figure>
  );
}
