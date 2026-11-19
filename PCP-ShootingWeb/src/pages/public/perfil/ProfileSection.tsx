import { useState, type ReactNode } from 'react';
import { IconChevronRight } from '@/components/icons/icons';
import { cx } from '@/lib/format';
import styles from './PerfilPage.module.css';

type Props = {
  id: string;
  title: string;
  /** Resumen a la derecha de la fila en celular ("1 inscrito", chip de estado…). */
  summary?: ReactNode;
  /** Acción junto al título en escritorio ("Ver historial"). */
  action?: ReactNode;
  /** Solo se muestra en celular (en escritorio lo cubre otra tarjeta). */
  mobileOnly?: boolean;
  className?: string;
  children: ReactNode;
};

/**
 * Sección del perfil.
 * Escritorio/tableta: tarjeta con título. Celular: fila de lista que se despliega (MPerfil).
 */
export function ProfileSection({ id, title, summary, action, mobileOnly, className, children }: Props) {
  const [open, setOpen] = useState(false);
  const bodyId = `${id}-contenido`;

  return (
    <section id={id} className={cx(styles.section, mobileOnly && styles.mobileOnlySection, className)} aria-labelledby={`${id}-titulo`}>
      <div className={styles.sectionHead}>
        <h2 id={`${id}-titulo`} className={styles.sectionTitle}>
          {title}
        </h2>
        {action}
      </div>

      <button
        type="button"
        className={styles.rowToggle}
        aria-expanded={open}
        aria-controls={bodyId}
        onClick={() => setOpen((o) => !o)}
      >
        <span className={styles.rowTitle}>{title}</span>
        {summary && <span className={styles.rowSummary}>{summary}</span>}
        <IconChevronRight size={18} className={cx(styles.rowChevron, open && styles.rowChevronOpen)} />
      </button>

      <div id={bodyId} className={cx(styles.sectionBody, !open && styles.collapsedOnMobile)}>
        {children}
      </div>
    </section>
  );
}
