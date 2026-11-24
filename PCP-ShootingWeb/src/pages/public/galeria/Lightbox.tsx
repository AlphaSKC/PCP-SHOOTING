import { useEffect, useRef } from 'react';
import { Photo } from '@/components/ui';
import { IconChevronLeft, IconChevronRight, IconClose } from '@/components/icons/icons';
import styles from './Lightbox.module.css';

type Props = {
  items: { id: string; category: string; title: string; kind: 'foto' | 'video'; imageUrl?: string | null; alt?: string }[];
  /** Índice abierto o null si está cerrado. */
  index: number | null;
  onChange: (index: number | null) => void;
  categoryLabel: (item: Props['items'][number]) => string;
};

/** Visor de fotos a pantalla completa con anterior/siguiente (también con las flechas del teclado). */
export function Lightbox({ items, index, onChange, categoryLabel }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const open = index !== null;
  const item = open ? items[index] : null;

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      dialog.showModal();
      closeRef.current?.focus();
    }
    if (!open && dialog.open) dialog.close();
  }, [open]);

  const go = (step: number) => {
    if (index === null) return;
    onChange((index + step + items.length) % items.length);
  };

  return (
    <dialog
      ref={dialogRef}
      className={styles.dialog}
      aria-label={item ? item.title : 'Visor de fotos'}
      onClose={() => onChange(null)}
      onClick={(e) => {
        if (e.target === dialogRef.current) onChange(null);
      }}
      onKeyDown={(e) => {
        if (e.key === 'ArrowRight') go(1);
        if (e.key === 'ArrowLeft') go(-1);
      }}
    >
      {item && (
        <div className={styles.panel}>
          <div className={styles.top}>
            <p className={styles.caption}>
              <span className={styles.tag}>{categoryLabel(item)}</span>
              {item.kind === 'video' ? 'Video' : 'Foto'}: {item.title}
              <span className={styles.counter}>
                {index! + 1} / {items.length}
              </span>
            </p>
            <button ref={closeRef} type="button" className={styles.iconButton} aria-label="Cerrar" onClick={() => onChange(null)}>
              <IconClose size={20} />
            </button>
          </div>
          <div className={styles.stage}>
            <Photo src={item.imageUrl} alt={item.alt ?? `${item.kind === 'video' ? 'Video' : 'Foto'}: ${item.title}`} className={styles.photo} />
          </div>
          {items.length > 1 && (
            <div className={styles.nav}>
              <button type="button" className={styles.iconButton} aria-label="Foto anterior" onClick={() => go(-1)}>
                <IconChevronLeft />
              </button>
              <button type="button" className={styles.iconButton} aria-label="Foto siguiente" onClick={() => go(1)}>
                <IconChevronRight />
              </button>
            </div>
          )}
        </div>
      )}
    </dialog>
  );
}
