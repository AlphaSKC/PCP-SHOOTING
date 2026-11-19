import { Link } from 'react-router';
import { ButtonLink, EmptyState, MobileActionBar } from '@/components/ui';
import { IconArrowLeft, IconCart } from '@/components/icons/icons';
import { useCart } from '@/features/tienda/CartProvider';
import { CartLines } from '@/features/tienda/components/CartLines';
import { formatMXN } from '@/lib/format';
import styles from './TiendaFlow.module.css';

/** Carrito como página: principal en celular (MCarrito); en escritorio también funciona como respaldo del panel. */
export default function CarritoPage() {
  const { lines, count, subtotal } = useCart();

  return (
    <>
      <title>Carrito · PCP Shooting</title>
      <div className={`container ${styles.page}`}>
        <Link to="/tienda" className={styles.back}>
          <IconArrowLeft size={18} /> Volver a la tienda
        </Link>
        <h1 className={styles.title}>Tu carrito ({count})</h1>

        {lines.length === 0 ? (
          <div className={styles.card}>
            <EmptyState
              icon={IconCart}
              title="Tu carrito está vacío"
              description="Agrega playeras, gorras y más de la tienda oficial."
              action={<ButtonLink to="/tienda">Ver productos</ButtonLink>}
            />
          </div>
        ) : (
          <div className={styles.layout}>
            <section className={`${styles.card} ${styles.flatOnMobile}`} aria-label="Productos en tu carrito">
              <CartLines lines={lines} />
            </section>

            <aside className={styles.summary}>
              <dl className={styles.totals}>
                <div>
                  <dt>Subtotal</dt>
                  <dd>{formatMXN(subtotal)}</dd>
                </div>
                <div>
                  <dt>Entrega</dt>
                  <dd>Gratis en el club</dd>
                </div>
                <div className={styles.grandTotal}>
                  <dt>Total</dt>
                  <dd>{formatMXN(subtotal)}</dd>
                </div>
              </dl>
              <p className={styles.note}>Solo recolección en el club · Km 23 Ruta de los Cenotes.</p>
              <ButtonLink to="/tienda/checkout" size="lg" fullWidth className={styles.desktopOnly}>
                Ir a pagar
              </ButtonLink>
              <ButtonLink to="/tienda" variant="secondary" size="lg" fullWidth>
                Seguir comprando
              </ButtonLink>
            </aside>
          </div>
        )}
      </div>

      {lines.length > 0 && (
        <MobileActionBar label={`Total · ${count} producto${count === 1 ? '' : 's'}`} amount={subtotal}>
          <ButtonLink to="/tienda/checkout" size="lg" fullWidth>
            Ir a pagar
          </ButtonLink>
        </MobileActionBar>
      )}
    </>
  );
}
