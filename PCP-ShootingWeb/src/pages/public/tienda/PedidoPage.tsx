import { useState } from 'react';
import { useParams } from 'react-router';
import { useClub } from '@/features/contenido/ContenidoProvider';
import { Button, ButtonLink, EmptyState, FormAlert, StatusBadge } from '@/components/ui';
import { IconCheck, IconClock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { useAsync } from '@/features/reservas/useAsync';
import { ONLINE_PAYMENT, cancelOrder, getOrder } from '@/features/tienda/api';
import { ApiError } from '@/lib/api';
import { formatDate, formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './PedidoPage.module.css';

/** Pedido confirmado (MPedidoOk). Se recoge en el club; se avisa por WhatsApp cuando esté listo. */
export default function PedidoPage() {
  const club = useClub();
  const { id = '' } = useParams();
  const { user, status } = useAuth();
  const [reload, setReload] = useState(0);
  const [cancelling, setCancelling] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  // Con la API el pedido es privado: se consulta con la sesión del cliente.
  const needsLogin = !ONLINE_PAYMENT && !user;
  const { data: order, loading, error } = useAsync(
    status === 'loading' || needsLogin ? null : () => getOrder(id),
    [id, reload, status, needsLogin],
  );

  if (status === 'loading' || loading) return <LoadingScreen />;

  if (needsLogin) {
    return (
      <div className={`container ${styles.page}`}>
        <title>Tu pedido · PCP Shooting</title>
        <EmptyState
          title="Inicia sesión para ver tu pedido"
          description="Tus pedidos están guardados en tu cuenta."
          action={<ButtonLink to={`/login?next=/tienda/pedido/${id}`}>Iniciar sesión</ButtonLink>}
        />
      </div>
    );
  }

  if (error) {
    return (
      <div className={`container ${styles.page}`}>
        <title>Tu pedido · PCP Shooting</title>
        <EmptyState
          title="No pudimos cargar tu pedido"
          description={error instanceof ApiError ? error.message : 'Revisa tu conexión e intenta de nuevo.'}
          action={<Button onClick={() => setReload((n) => n + 1)}>Reintentar</Button>}
        />
      </div>
    );
  }

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

  const paid = order.status !== 'pendiente_pago' && order.status !== 'cancelada';
  const cancelled = order.status === 'cancelada';
  const ordersHref = user ? '/perfil#pedidos' : '/login?next=/perfil';
  const title = cancelled ? 'Pedido cancelado' : paid ? '¡Gracias por tu compra!' : ONLINE_PAYMENT ? 'Pago en proceso' : 'Pedido registrado';
  const pendingText = ONLINE_PAYMENT
    ? 'Te avisaremos en cuanto Mercado Pago confirme tu pago.'
    : 'Está pendiente de pago: el club te contactará por WhatsApp para completarlo. Lo recoges cuando esté pagado.';
  const paidText =
    order.status === 'entregado'
      ? `Lo recogiste${order.pickedUpAt ? ` el ${formatDate(order.pickedUpAt)}` : ''}. ¡Gracias!`
      : 'Te avisaremos por WhatsApp cuando esté listo para recoger en el club.';

  async function onCancel() {
    if (!order || !window.confirm(`¿Cancelar el pedido #${order.folio}?`)) return;
    setCancelling(true);
    setActionError(null);
    try {
      await cancelOrder(order.id);
      setReload((n) => n + 1);
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : 'No pudimos cancelar el pedido. Intenta de nuevo.');
    } finally {
      setCancelling(false);
    }
  }

  return (
    <>
      <title>{title} · PCP Shooting</title>
      <div className={`container ${styles.page}`}>
        <section className={styles.card} aria-labelledby="gracias">
          <span className={paid ? styles.check : styles.pending} aria-hidden="true">
            {paid ? <IconCheck size={34} strokeWidth={3} /> : <IconClock size={34} />}
          </span>
          <h1 id="gracias" className={styles.title}>
            {title}
          </h1>
          <p className={styles.lead}>
            Pedido <strong>#{order.folio}</strong> por <strong>{formatMXN(order.total)}</strong>.{' '}
            {cancelled ? 'Este pedido ya no está activo.' : paid ? paidText : pendingText}
          </p>
          {actionError && <FormAlert>{actionError}</FormAlert>}
          <div className={styles.actions}>
            <ButtonLink to={ordersHref} size="lg">
              Ver mis pedidos
            </ButtonLink>
            <ButtonLink to="/tienda" size="lg" variant="ghost" className={styles.secondary}>
              Seguir comprando
            </ButtonLink>
          </div>
          {order.status === 'pendiente_pago' && (
            <Button variant="text" className={styles.cancel} loading={cancelling} onClick={() => void onCancel()}>
              Cancelar pedido
            </Button>
          )}
        </section>

        <section className={styles.details} aria-label="Detalle del pedido">
          <div className={styles.detailsHead}>
            <h2 className={styles.detailsTitle}>Detalle</h2>
            <StatusBadge status={order.status} />
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
