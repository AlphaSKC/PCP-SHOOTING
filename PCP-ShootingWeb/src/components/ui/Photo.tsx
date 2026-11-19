import { cx } from '@/lib/format';
import styles from './Photo.module.css';

type Props = {
  /** URL de la foto (Supabase Storage). Si falta, se muestra el marcador rayado del diseño. */
  src?: string | null;
  alt: string;
  className?: string;
};

/**
 * Foto con marcador de posición. El tamaño lo define el contenedor (className),
 * así sirve igual para productos, galería, blog y eventos.
 */
export function Photo({ src, alt, className }: Props) {
  if (src) {
    return <img src={src} alt={alt} loading="lazy" decoding="async" className={cx(styles.photo, className)} />;
  }
  return (
    <div role="img" aria-label={alt} className={cx(styles.photo, styles.placeholder, className)}>
      <span aria-hidden="true">{alt}</span>
    </div>
  );
}
