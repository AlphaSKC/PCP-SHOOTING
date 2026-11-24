import type { ReactNode } from 'react';
import { cx } from '@/lib/format';
import styles from './CtaBanner.module.css';

type Props = {
  /** Texto rojo pequeño sobre el título. */
  eyebrow?: string;
  title: string;
  /** Contenido extra bajo el texto (etiquetas, notas). */
  extra?: ReactNode;
  /** Texto bajo el título. `textMobile` lo sustituye en celular (los diseños lo acortan). */
  text?: ReactNode;
  textMobile?: ReactNode;
  /** Botones. En celular se apilan a todo lo ancho. */
  actions: ReactNode;
  className?: string;
};

/** Franja negra con llamado a la acción ("¿No sabes cuál elegir?", "Etiquétanos en tus fotos"). */
export function CtaBanner({ eyebrow, title, extra, text, textMobile, actions, className }: Props) {
  return (
    <section className={cx(styles.banner, className)}>
      <div className={styles.text}>
        {eyebrow && <span className={styles.eyebrow}>{eyebrow}</span>}
        <h2 className={styles.title}>{title}</h2>
        {text && <p className={cx(styles.copy, textMobile !== undefined && styles.desktopOnly)}>{text}</p>}
        {textMobile && <p className={cx(styles.copy, styles.mobileOnly)}>{textMobile}</p>}
        {extra}
      </div>
      <div className={styles.actions}>{actions}</div>
    </section>
  );
}
