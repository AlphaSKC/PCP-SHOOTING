import { useParams } from 'react-router';
import { QRCodeSVG } from 'qrcode.react';
import { Button, ButtonLink, EmptyState, PageHero, StatusBadge, StepBar } from '@/components/ui';
import { IconCalendarPlus, IconCheck, IconClock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { getReservation } from '@/features/reservas/api';
import { RESERVATION_STEPS } from '@/features/reservas/config';
import { downloadIcs } from '@/features/reservas/calendar';
import { ReservationSummary } from '@/features/reservas/components/ReservationSummary';
import { peopleLabel, shortDay } from '@/features/reservas/format';
import { useAsync } from '@/features/reservas/useAsync';
import { formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './ConfirmacionPage.module.css';

/** Paso 5: reserva confirmada con su código QR (se valida en recepción). */
export default function ConfirmacionPage() {
  const { id = '' } = useParams();
  const { user } = useAuth();
  const { data: r, loading } = useAsync(() => getReservation(id), [id]);

  if (loading) return <LoadingScreen />;

  if (!r) {
    return (
      <div className={`container ${styles.notFound}`}>
        <title>Reserva no encontrada · PCP Shooting</title>
        <EmptyState
          title="No encontramos esta reserva"
          description="Revisa el enlace de tu correo o consulta tus reservas en tu perfil."
          action={<ButtonLink to={user ? '/perfil#reservas' : '/reservar'}>{user ? 'Ir a mi perfil' : 'Hacer una reserva'}</ButtonLink>}
        />
      </div>
    );
  }

  const confirmed = r.status === 'confirmada';
  const qrValue = r.code; // TODO(API): el backend define el contenido firmado del QR.

  return (
    <>
      <title>{confirmed ? 'Reserva confirmada' : 'Reserva pendiente de pago'} · PCP Shooting</title>

      {/* Escritorio/tableta: hero del wizard con todos los pasos completos. */}
      <div className={styles.desktopHero}>
        <PageHero
          compact
          eyebrow="Reservas en línea"
          title="Reserva tu línea de tiro"
          lead="15 líneas de tiro · Lunes a sábado, 9:00 a 17:00 · Pago seguro en línea"
          aside={<StepBar steps={RESERVATION_STEPS} current={confirmed ? RESERVATION_STEPS.length : 3} />}
        />
      </div>

      {/* Celular (MReserva5): franja negra con la palomita. */}
      <section className={styles.mobileHero}>
        <span className={confirmed ? styles.bigCheck : styles.bigPending} aria-hidden="true">
          {confirmed ? <IconCheck size={34} strokeWidth={3} /> : <IconClock size={34} />}
        </span>
        <h1 className={styles.mobileTitle}>{confirmed ? '¡Reserva confirmada!' : 'Pago en proceso'}</h1>
        <p>{confirmed ? 'Te enviamos los detalles por correo y WhatsApp.' : 'Te avisaremos en cuanto se confirme tu pago.'}</p>
      </section>

      <div className={`container ${styles.layout}`}>
        <div className={styles.main}>
          <section className={styles.card} aria-labelledby="confirmacion-titulo">
            <div className={styles.cardText}>
              <span className={confirmed ? styles.bigCheck : styles.bigPending} aria-hidden="true">
                {confirmed ? <IconCheck size={34} strokeWidth={3} /> : <IconClock size={34} />}
              </span>
              <h2 id="confirmacion-titulo" className={styles.title}>
                {confirmed ? '¡Reserva confirmada!' : 'Pago en proceso'}
              </h2>
              <p className={styles.lead}>
                Código de reserva <strong>{r.code}</strong>.{' '}
                {confirmed
                  ? 'Te enviamos los detalles por correo y WhatsApp. Presenta este código en recepción.'
                  : 'Tu reserva se confirmará en cuanto Mercado Pago apruebe el pago.'}
              </p>
            </div>

            <div className={styles.ticket}>
              <div className={styles.qr}>
                <QRCodeSVG value={qrValue} size={168} level="M" marginSize={0} title={`Código QR de la reserva ${r.code}`} />
              </div>
              <p className={styles.qrHint}>Muestra este código en recepción</p>
              <p className={styles.code}>{r.code}</p>
              <dl className={styles.details}>
                <div>
                  <dt>Actividad</dt>
                  <dd>
                    {r.packageName} · {peopleLabel(r.people, true)}
                  </dd>
                </div>
                <div>
                  <dt>Fecha</dt>
                  <dd>
                    {shortDay(r.date)} · {r.time}
                  </dd>
                </div>
                <div>
                  <dt>Línea</dt>
                  <dd>Línea {r.line}</dd>
                </div>
                <div>
                  <dt>{confirmed ? 'Pagado' : 'Por pagar'}</dt>
                  <dd>{formatMXN(r.total).replace(' MXN', '')}</dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>
                    <StatusBadge status={r.status} />
                  </dd>
                </div>
              </dl>
            </div>

            <div className={styles.actions}>
              <ButtonLink to={user ? '/perfil#reservas' : '/login?next=/perfil'} size="lg">
                Ver en mi perfil
              </ButtonLink>
              <Button variant="secondary" size="lg" iconStart={<IconCalendarPlus size={18} />} onClick={() => downloadIcs(r)}>
                Agregar al calendario
              </Button>
              <ButtonLink to="/reservar" size="lg" variant="ghost" className={styles.again}>
                Hacer otra reserva
              </ButtonLink>
            </div>
          </section>
        </div>

        <aside className={styles.aside}>
          <ReservationSummary
            data={{
              packageName: r.packageName,
              equipment: r.equipment,
              people: r.people,
              date: r.date,
              time: r.time,
              line: r.line,
              total: r.total,
            }}
            totalLabel={confirmed ? 'Total pagado' : 'Total'}
          />
        </aside>
      </div>
    </>
  );
}
