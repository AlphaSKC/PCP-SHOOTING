import { Link } from 'react-router';
import { useEffect, useRef } from 'react';
import { IconCart, IconClose } from '@/components/icons/icons';
import { formatMXN } from '@/lib/format';
import { Button } from './Button';
import { EmptyState } from './EmptyState';
import { Photo } from './Photo';
import { QuantityStepper } from './QuantityStepper';
import styles from './CartDrawer.module.css';

export type CartLine = {
  id: string;
  name: string;
  /** Página del producto (al tocar foto o nombre se abre y se cierra el carrito). */
  href?: string;
  /** Precio unitario. */
  price: number;
  quantity: number;
  imageUrl?: string | null;
  /** Talla elegida y tallas disponibles (solo playeras). */
  size?: string;
  sizes?: string[];
  /** Tallas sin existencias (se muestran deshabilitadas). */
  soldOutSizes?: string[];
  /** Piezas disponibles (tope del contador). */
  max?: number;
  /** Aviso cuando se piden más piezas de las que hay. */
  warning?: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
  lines: CartLine[];
  onQuantityChange: (id: string, quantity: number) => void;
  onSizeChange: (id: string, size: string) => void;
  onRemove: (id: string) => void;
  onCheckout: () => void;
  /** "Ver productos" / "Seguir comprando". Si no se pasa, solo cierra el panel. */
  onContinue?: () => void;
};

/**
 * Carrito lateral (escritorio y tableta). En celular ocupa toda la pantalla.
 * Solo recolección en el club: no hay costo de envío.
 */
export function CartDrawer({ open, onClose, lines, onQuantityChange, onSizeChange, onRemove, onCheckout, onContinue }: Props) {
  const continueShopping = onContinue ?? onClose;
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      dialog.showModal();
      closeRef.current?.focus();
    }
    if (!open && dialog.open) dialog.close();
  }, [open]);

  const count = lines.reduce((sum, l) => sum + l.quantity, 0);
  const subtotal = lines.reduce((sum, l) => sum + l.price * l.quantity, 0);

  return (
    <dialog
      ref={dialogRef}
      className={styles.dialog}
      aria-label="Carrito"
      onClose={onClose}
      onClick={(event) => {
        if (event.target === dialogRef.current) onClose();
      }}
    >
      <div className={styles.panel}>
        <header className={styles.header}>
          <h2 className={styles.title}>Tu carrito ({count})</h2>
          <button ref={closeRef} type="button" className={styles.close} aria-label="Cerrar carrito" onClick={onClose}>
            <IconClose size={20} />
          </button>
        </header>

        {lines.length === 0 ? (
          <EmptyState
            className={styles.empty}
            icon={IconCart}
            title="Tu carrito está vacío"
            description="Agrega playeras, gorras y más de la tienda oficial."
            action={
              <Button size="lg" onClick={continueShopping}>
                Ver productos
              </Button>
            }
          />
        ) : (
          <>
            <ul className={styles.list}>
              {lines.map((line) => (
                <li key={line.id} className={styles.item}>
                  {line.href ? (
                    <Link to={line.href} className={styles.thumb} aria-label={`Ver ${line.name}`} tabIndex={-1} onClick={onClose}>
                      <Photo src={line.imageUrl} alt={line.name} />
                    </Link>
                  ) : (
                    <div className={styles.thumb}>
                      <Photo src={line.imageUrl} alt={line.name} />
                    </div>
                  )}
                  <div className={styles.info}>
                    <div className={styles.row}>
                      {line.href ? <Link to={line.href} className={styles.name} onClick={onClose}>{line.name}</Link> : <span className={styles.name}>{line.name}</span>}
                      <span className={styles.lineTotal}>{formatMXN(line.price * line.quantity)}</span>
                    </div>
                    {line.sizes && line.sizes.length > 0 && (
                      <div className={styles.sizeRow}>
                        <label htmlFor={`talla-${line.id}`}>Talla</label>
                        <select
                          id={`talla-${line.id}`}
                          className={styles.size}
                          value={line.size}
                          onChange={(e) => onSizeChange(line.id, e.target.value)}
                        >
                          {line.sizes.map((s) => {
                            const out = line.soldOutSizes?.includes(s) ?? false;
                            return (
                              <option key={s} value={s} disabled={out && s !== line.size}>
                                {out ? `${s} · agotada` : s}
                              </option>
                            );
                          })}
                        </select>
                      </div>
                    )}
                    {line.warning && (
                      <p className={styles.warning} role="alert">
                        {line.warning}
                      </p>
                    )}
                    <div className={styles.row}>
                      <QuantityStepper
                        label={line.name}
                        value={line.quantity}
                        max={line.max !== undefined && Number.isFinite(line.max) ? Math.max(1, line.max) : undefined}
                        onChange={(q) => onQuantityChange(line.id, q)}
                      />
                      <Button variant="text" onClick={() => onRemove(line.id)} aria-label={`Eliminar ${line.name}`}>
                        Eliminar
                      </Button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>

            <footer className={styles.footer}>
              <div className={styles.subtotal}>
                <span>Subtotal</span>
                <span className={styles.subtotalValue}>{formatMXN(subtotal)}</span>
              </div>
              <p className={styles.note}>Recoge gratis en el club · Km 23 Ruta de los Cenotes.</p>
              <Button size="lg" fullWidth onClick={onCheckout}>
                Ir a pagar
              </Button>
              <Button variant="secondary" size="lg" fullWidth onClick={continueShopping}>
                Seguir comprando
              </Button>
            </footer>
          </>
        )}
      </div>
    </dialog>
  );
}
