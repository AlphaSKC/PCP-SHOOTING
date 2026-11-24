import { useState } from 'react';
import { useParams } from 'react-router';
import { Button, ButtonLink, EmptyState, StatusBadge } from '@/components/ui';
import { IconClock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { ReservationSummary } from '@/features/reservas/components/ReservationSummary';
import { shortDay } from '@/features/reservas/format';
import { clubTime, getApiReservation } from '@/features/reservas/reservasApi';
import { useAsync } from '@/features/reservas/useAsync';
import { ApiError } from '@/lib/api';
import { formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './ConfirmacionPage.module.css';

/** Números de línea guardados al reservar (la API devuelve solo ids de línea). */
function savedLanes(id: string): number[] | null {
  try {
    return JSON.parse(sessionStorage.getItem(`pcp.reserva.lineas.${id}`) ?? 'null') as number[] | null;
  } catch {
    return null;
  }
}

/**
 * Detalle de una reserva real (GET /api/reservas/{id}). Mientras no exista Mercado Pago (R05)
 * la reserva queda "Pendiente de pago" y vence; el QR de llegada llega con R05/R06.
 */
export default function ConfirmacionApi() {
  const { id = '' } = useParams();
  const { user, status } = useAuth();
  const [reload, setReload] = useState(0);
  const { data: r, loading, error } = useAsync(status === 'loading' || !user ? null : () => getApiReservation(id), [id, user, status, reload]);

  if (status === 'loading' || loading) return <LoadingScreen />;

  if (!user) {
    return (
      <div className={`container ${styles.notFound}`}>
        <title>Tu reserva · PCP Shooting</title>
        <EmptyState title="Inicia sesión para ver tu reserva" action={<ButtonLink to={`/login?next=/reservar/confirmacion/${id}`}>Iniciar sesión</ButtonLink>} />
      </div>
    );
  }

  if (error || !r) {
    const notFound = error instanceof ApiError && error.status === 404;
    return (
      <div className={`container ${styles.notFound}`}>
        <title>Reserva · PCP Shooting</title>
        <EmptyState
          title={notFound ? 'No encontramos esta reserva' : 'No pudimos cargar tu reserva'}
          description={notFound ? 'Consulta tus reservas en tu perfil.' : 'Revisa tu conexión e intenta de nuevo.'}
          action={notFound ? <ButtonLink to="/perfil#reservas">Ir a mi perfil</ButtonLink> : <Button onClick={() => setReload((n) => n + 1)}>Reintentar</Button>}
        />
      </div>
    );
  }

  const expired = r.estado === 'Vencida';
  const lanes = savedLanes(r.id);
  const people = r.participantes.length;
  const time = r.hora.slice(0, 5);

  return (
    <>
      <title>{expired ? 'Reserva vencida' : 'Reserva pendiente de pago'} · PCP Shooting</title>
      <div className={`container ${styles.layout}`}>
        <div className={styles.main}>
          <section className={styles.card} aria-labelledby="reserva-titulo">
            <div className={styles.cardText}>
              <span className={styles.bigPending} aria-hidden="true">
                <IconClock size={34} />
              </span>
              <h1 id="reserva-titulo" className={styles.title}>
                {expired ? 'Reserva vencida' : 'Pendiente de pago'}
              </h1>
              <p className={styles.lead}>
                {expired
                  ? 'No se registró el pago a tiempo y las líneas se liberaron. Puedes hacer una nueva reserva.'
                  : `El pago en línea aún no está activo. Si no se paga, las líneas se liberan a las ${clubTime(r.vencePagoEnUtc)}.`}
              </p>
            </div>

            <div className={styles.ticket}>
              <dl className={styles.details}>
                <div>
                  <dt>Actividad</dt>
                  <dd>{r.nombrePaquete}</dd>
                </div>
                <div>
                  <dt>Fecha</dt>
                  <dd>
                    {shortDay(r.fecha)} · {time}
                  </dd>
                </div>
                {lanes && (
                  <div>
                    <dt>{lanes.length === 1 ? 'Línea' : 'Líneas'}</dt>
                    <dd>{lanes.join(', ')}</dd>
                  </div>
                )}
                <div>
                  <dt>Participantes</dt>
                  <dd>{r.participantes.map((p) => p.nombre).join(', ')}</dd>
                </div>
                <div>
                  <dt>Por pagar</dt>
                  <dd>{formatMXN(r.total).replace(' MXN', '')}</dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>{expired ? <StatusBadge status="vencida" /> : <StatusBadge status="pendiente_pago" />}</dd>
                </div>
              </dl>
            </div>

            <div className={styles.actions}>
              <ButtonLink to="/perfil#reservas" size="lg">
                Ver mis reservas
              </ButtonLink>
              <ButtonLink to="/reservar" size="lg" variant="secondary">
                Hacer otra reserva
              </ButtonLink>
            </div>
          </section>
        </div>
        <aside className={styles.aside}>
          <ReservationSummary data={{ packageName: r.nombrePaquete, people, date: r.fecha, time, lines: lanes ?? [], total: r.total }} />
        </aside>
      </div>
    </>
  );
}
