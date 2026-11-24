import type { ReactNode } from 'react';
import { IconUpload } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './ui.module.css';

/** Contenido de una pantalla del panel (bajo AdminPageHeader). */
export function AdminBody({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cx(styles.body, className)}>{children}</div>;
}

/**
 * Lista + editor lateral (Productos, Pedidos, Eventos, Contenido…).
 * Escritorio: dos columnas con el editor fijo. Tableta/celular: el editor va debajo.
 */
export function Split({ main, aside, asideWidth = 360 }: { main: ReactNode; aside: ReactNode; asideWidth?: number }) {
  return (
    <div className={styles.split} style={{ ['--aside-w' as string]: `${asideWidth}px` }}>
      <div className={styles.splitMain}>{main}</div>
      <div className={styles.splitAside}>{aside}</div>
    </div>
  );
}

/** Tarjeta de edición con borde superior rojo ("Editar producto", "Reserva seleccionada"). */
export function Panel({ title, children, id, actions }: { title: string; children: ReactNode; id?: string; actions?: ReactNode }) {
  return (
    <section id={id} className={styles.panel} aria-labelledby={id ? `${id}-t` : undefined}>
      <div className={styles.panelHead}>
        <h2 id={id ? `${id}-t` : undefined} className={styles.panelTitle}>
          {title}
        </h2>
        {actions}
      </div>
      {children}
    </section>
  );
}

/** Tarjeta blanca de sección con título y acción opcional. */
export function Section({ title, actions, children, className }: { title?: string; actions?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={cx(styles.section, className)}>
      {(title || actions) && (
        <div className={styles.sectionHead}>
          {title && <h2 className={styles.sectionTitle}>{title}</h2>}
          {actions}
        </div>
      )}
      {children}
    </section>
  );
}

/** Pestañas segmentadas ("Tienda de ropa (7) | Catálogo de armas (3)"). */
export function SegmentedTabs<T extends string>({
  value,
  onChange,
  options,
  label,
}: {
  value: T;
  onChange: (v: T) => void;
  options: { value: T; label: string }[];
  label: string;
}) {
  return (
    <div role="tablist" aria-label={label} className={styles.tabs}>
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          role="tab"
          aria-selected={value === o.value}
          className={cx(styles.tab, value === o.value && styles.tabActive)}
          onClick={() => onChange(o.value)}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

export function ProgressBar({ value, max, label }: { value: number; max: number; label: string }) {
  const pct = max > 0 ? Math.min(100, Math.round((value / max) * 100)) : 0;
  return (
    <div className={styles.progress} role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={max} aria-valuenow={value}>
      <span style={{ width: `${pct}%` }} />
    </div>
  );
}

/** Zona para subir archivos (en demo solo muestra el nombre; con Supabase sube a Storage). */
export function UploadDrop({
  title,
  hint = 'Arrastra aquí o haz clic · JPG o PNG',
  accept = 'image/png,image/jpeg',
  multiple,
  onFiles,
}: {
  title: string;
  hint?: string;
  accept?: string;
  multiple?: boolean;
  onFiles?: (files: File[]) => void;
}) {
  return (
    <label className={styles.drop}>
      <input
        type="file"
        accept={accept}
        multiple={multiple}
        className="visually-hidden"
        onChange={(e) => onFiles?.(Array.from(e.target.files ?? []))}
      />
      <IconUpload size={24} className={styles.dropIcon} />
      <strong>{title}</strong>
      <span>{hint}</span>
    </label>
  );
}

/** Aviso de que el panel usa datos de demostración. */
export function DemoNote({ children }: { children: ReactNode }) {
  return <p className={styles.demo}>{children}</p>;
}
