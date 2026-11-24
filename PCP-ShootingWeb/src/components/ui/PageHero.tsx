import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './PageHero.module.css';

type Props = {
  /** Texto rojo pequeño sobre el título ("Tienda oficial · Ropa y accesorios"). */
  eyebrow?: string;
  title: string;
  lead?: ReactNode;
  /** Botones u otro contenido bajo el texto. */
  children?: ReactNode;
  /** Contenido a la derecha en escritorio (foto, logo, filtros, buscador…). */
  aside?: ReactNode;
  /** En celular, el `aside` va ANTES del texto (p. ej. el logo de Nosotros). */
  asideFirstOnMobile?: boolean;
  /** Oculta el `lead` en celular (los diseños móviles lo recortan). */
  hideLeadOnMobile?: boolean;
  /** Oculta `aside` y/o `children` en celular. */
  hideAsideOnMobile?: boolean;
  hideChildrenOnMobile?: boolean;
  /** Menos alto (páginas internas como Reservar). */
  compact?: boolean;
  /**
   * dark: franja negra (por defecto).
   * light: fondo claro en tableta/escritorio y negro en celular (Galería y Blog).
   */
  tone?: 'dark' | 'light';
  /** Contenido extra al final de la franja, a todo lo ancho (p. ej. cifras del club). */
  footer?: ReactNode;
};

/** Franja de inicio de página con eyebrow, título en mayúsculas y texto. */
export function PageHero({
  eyebrow,
  title,
  lead,
  children,
  aside,
  asideFirstOnMobile = false,
  hideLeadOnMobile = false,
  hideAsideOnMobile = false,
  hideChildrenOnMobile = false,
  compact = false,
  tone = 'dark',
  footer,
}: Props) {
  return (
    <section className={cx(styles.hero, compact && styles.compact, tone === 'light' && styles.light)}>
      <div className={cx('container', styles.inner, asideFirstOnMobile && styles.asideFirst)}>
        <div className={styles.text}>
          {eyebrow && <span className={styles.eyebrow}>{eyebrow}</span>}
          <h1 className={styles.title}>{title}</h1>
          {lead && <p className={cx(styles.lead, hideLeadOnMobile && styles.hideOnMobile)}>{lead}</p>}
          {children && <div className={cx(styles.extra, hideChildrenOnMobile && styles.hideOnMobile)}>{children}</div>}
        </div>
        {aside && <div className={cx(styles.aside, hideAsideOnMobile && styles.hideOnMobile)}>{aside}</div>}
      </div>
      {footer}
    </section>
  );
}
