import { useParams } from 'react-router';
import { club } from '@/app/navigation';
import { ButtonLink, EmptyState, StatusBadge } from '@/components/ui';
import { IconCheck, IconClock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { useAsync } from '@/features/reservas/useAsync';
import { getOrder } from '@/features/tienda/api';
import { formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './PedidoPage.module.css';

/** Pedido confirmado (MPedidoOk). Se recoge en el club; se avisa por WhatsApp cuando esté listo. */
export default function PedidoPage() {
  const { id = '' } = useParams();
  const { user } = useAuth();
  const { data: order, loading } = useAsync(() => getOrder(id), [id]);

  if (loading) return <LoadingScreen />;

  if (!order) {
    return (
      <div className={`container ${styles.page}`}>
        <title>Pedido no encontrado · PCP Shooting</title>
        <EmptyState
          title="No encontramos este pedido"
          description="Revisa el enlace de tu correo o consulta tus pedidos en tu perfil."
          action={<ButtonLink to="/tienda">Ir a la tienda</ButtonLink>}
        />
      </div>
    );
  }

  const paid = order.status !== 'pendiente_pago';
  const ordersHref = user ? '/perfil#pedidos' : '/login?next=/perfil';

  return (
    <>
      <title>{paid ? '¡Gracias por tu compra!' : 'Pago en proceso'} · PCP Shooting</title>
      <div className={`container ${styles.page}`}>
        <section className={styles.card} aria-labelledby="gracias">
          <span className={paid ? styles.check : styles.pending} aria-hidden="true">
            {paid ? <IconCheck size={34} strokeWidth={3} /> : <IconClock size={34} />}
          </span>
          <h1 id="gracias" className={styles.title}>
            {paid ? '¡Gracias por tu compra!' : 'Pago en proceso'}
          </h1>
          <p className={styles.lead}>
            Pedido <strong>#{order.folio}</strong> por <strong>{formatMXN(order.total)}</strong>.{' '}
            {paid
              ? 'Te avisaremos por WhatsApp cuando esté listo para recoger en el club.'
              : 'Te avisaremos en cuanto Mercado Pago confirme tu pago.'}
          </p>
          <div className={styles.actions}>
            <ButtonLink to={ordersHref} size="lg">
              Ver mis pedidos
            </ButtonLink>
            <ButtonLink to="/tienda" size="lg" variant="ghost" className={styles.secondary}>
              Seguir comprando
            </ButtonLink>
          </div>
        </section>

        <section className={styles.details} aria-label="Detalle del pedido">
          <div className={styles.detailsHead}>
            <h2 className={styles.detailsTitle}>Detalle</h2>
            <StatusBadge status={order.status === 'pendiente_pago' ? 'pendiente_pago' : order.status} />
          </div>
          <ul className={styles.lines}>
            {order.lines.map((l) => (
              <li key={`${l.productId}:${l.size ?? ''}`}>
                <span>
                  {l.name}
                  {l.size && ` · ${l.size}`} <span className={styles.qty}>× {l.quantity}</span>
                </span>
                <strong>{formatMXN(l.price * l.quantity)}</strong>
              </li>
            ))}
          </ul>
          <p className={styles.pickup}>
            <strong>Recoge en el club:</strong> {club.address} · {club.hours}. Presenta tu número de pedido.
          </p>
        </section>
      </div>
    </>
  );
}
