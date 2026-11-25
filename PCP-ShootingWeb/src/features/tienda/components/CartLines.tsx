import { Button, Photo, QuantityStepper } from '@/components/ui';
import { formatMXN } from '@/lib/format';
import { stockView, useCart, type CartLineView } from '../CartProvider';
import styles from './CartLines.module.css';

/** Renglones editables del carrito (página /tienda/carrito). */
export function CartLines({ lines }: { lines: CartLineView[] }) {
  const cart = useCart();
  return (
    <ul className={styles.list}>
      {lines.map((l) => {
        const view = stockView(l);
        return (
        <li key={l.key} className={styles.item}>
          <div className={styles.thumb}>
            <Photo src={l.product.imageUrl} alt={l.product.name} />
          </div>
          <div className={styles.info}>
            <div className={styles.row}>
              <span className={styles.name}>{l.product.name}</span>
              <span className={styles.price}>{formatMXN(l.lineTotal)}</span>
            </div>
            {l.product.sizes && (
              <div className={styles.sizeRow}>
                <label htmlFor={`talla-${l.key}`}>Talla</label>
                <select
                  id={`talla-${l.key}`}
                  className={styles.size}
                  value={l.size}
                  onChange={(e) => cart.setSize(l.key, e.target.value)}
                >
                  {l.product.sizes.map((s) => {
                    const out = view.soldOutSizes?.includes(s) ?? false;
                    return (
                      <option key={s} value={s} disabled={out && s !== l.size}>
                        {out ? `${s} · agotada` : s}
                      </option>
                    );
                  })}
                </select>
              </div>
            )}
            {view.warning && (
              <p className={styles.warning} role="alert">
                {view.warning}
              </p>
            )}
            <div className={styles.row}>
              <QuantityStepper
                label={l.product.name}
                value={l.quantity}
                max={view.max !== undefined ? Math.max(1, view.max) : undefined}
                onChange={(q) => cart.setQuantity(l.key, q)}
              />
              <Button variant="text" onClick={() => cart.remove(l.key)} aria-label={`Eliminar ${l.product.name}`}>
                Eliminar
              </Button>
            </div>
          </div>
        </li>
        );
      })}
    </ul>
  );
}

/** Lista compacta (resumen del checkout). */
export function OrderLinesSummary({ lines }: { lines: CartLineView[] }) {
  return (
    <ul className={styles.summaryList}>
      {lines.map((l) => (
        <li key={l.key} className={styles.summaryItem}>
          <div className={styles.summaryThumb}>
            <Photo src={l.product.imageUrl} alt={l.product.name} />
          </div>
          <span className={styles.summaryText}>
            <strong>{l.product.name}</strong>
            <span>
              {l.size && `Talla ${l.size} · `}
              {l.quantity} pza.
            </span>
            {stockView(l).warning && <span className={styles.warning}>{stockView(l).warning}</span>}
          </span>
          <span className={styles.summaryPrice}>{formatMXN(l.lineTotal)}</span>
        </li>
      ))}
    </ul>
  );
}
