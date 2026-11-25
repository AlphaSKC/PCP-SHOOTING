import { Link } from 'react-router';
import { StatusBadge } from '@/components/ui';
import { clubLongDay, clubTime, listMyReservations } from '@/features/reservas/reservasApi';
import { useAsync } from '@/features/reservas/useAsync';
import { formatMXN } from '@/lib/format';
import styles from './PerfilPage.module.css';

/** "Mis reservas" con la API (GET /api/reservas): pendiente de pago, vencida o pagada (Confirmada). */
export function MyReservationsApi() {
  const { data, loading, error } = useAsync(listMyReservations, []);

  if (loading) return <p className={styles.muted}>Cargando tus reservas…</p>;
  if (error) return <p className={styles.muted}>No pudimos cargar tus reservas. Recarga la página para intentar de nuevo.</p>;
  if (!data?.length)
    return (
      <p className={styles.muted}>
        Aún no tienes reservas. <Link to="/reservar">Reserva tu línea de tiro</Link>.
      </p>
    );

  return (
    <ul className={styles.items}>
      {data.map((r) => (
        <li key={r.id} className={styles.item}>
          <span className={styles.itemText}>
            <Link to={`/reservar/confirmacion/${r.id}`}>
              <strong>{r.nombrePaquete}</strong>
            </Link>
            <span className={styles.muted}>
              {clubLongDay(r.inicioUtc)} · {clubTime(r.inicioUtc)}
              {r.cantidadParticipantes ? ` · ${r.cantidadParticipantes} ${r.cantidadParticipantes === 1 ? 'persona' : 'personas'}` : ''} · {formatMXN(r.total)}
            </span>
          </span>
          <StatusBadge status={r.estado === 'Pagada' ? 'confirmada' : r.estado === 'Vencida' ? 'vencida' : 'pendiente_pago'} />
        </li>
      ))}
    </ul>
  );
}
