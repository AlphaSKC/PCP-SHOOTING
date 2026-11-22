import { Link } from 'react-router';
import { StatusBadge } from '@/components/ui';
import { myOrders } from '@/data/profile';
import { useAsync } from '@/features/reservas/useAsync';
import { listMyOrders } from '@/features/tienda/api';
import { DATA_SOURCE } from '@/lib/api';
import { formatDate } from '@/lib/format';
import { ProfileSection } from './ProfileSection';
import styles from './PerfilPage.module.css';

/** "Pedidos de la tienda" del perfil. Con la API: GET /api/pedidos (pedidos propios). */
export function MyOrdersSection() {
  const api = DATA_SOURCE === 'api';
  const { data, loading, error } = useAsync(api ? listMyOrders : null, [api]);

  if (!api) {
    return (
      <ProfileSection id="pedidos" title="Pedidos de la tienda" summary={myOrders[0] && <StatusBadge status={myOrders[0].status} />} className={styles.orders}>
        <ul className={styles.items}>
          {myOrders.map((o) => (
            <li key={o.id} className={styles.item}>
              <span className={styles.itemText}>
                <strong>Pedido #{o.id}</strong>
                <span className={styles.muted}>{o.items}</span>
              </span>
              <StatusBadge status={o.status} />
            </li>
          ))}
        </ul>
        <Link to="/tienda" className={styles.cardLink}>
          Ir a la tienda
        </Link>
      </ProfileSection>
    );
  }

  const orders = data ?? [];
  return (
    <ProfileSection id="pedidos" title="Pedidos de la tienda" summary={orders[0] && <StatusBadge status={orders[0].status} />} className={styles.orders}>
      {loading ? (
        <p className={styles.muted}>Cargando tus pedidos…</p>
      ) : error ? (
        <p className={styles.muted}>No pudimos cargar tus pedidos. Recarga la página para intentar de nuevo.</p>
      ) : orders.length === 0 ? (
        <p className={styles.muted}>Aún no tienes pedidos.</p>
      ) : (
        <ul className={styles.items}>
          {orders.slice(0, 10).map((o) => (
            <li key={o.id} className={styles.item}>
              <span className={styles.itemText}>
                <Link to={`/tienda/pedido/${o.id}`}>
                  <strong>Pedido #{o.folio}</strong>
                </Link>
                <span className={styles.muted}>{formatDate(o.createdAt)}</span>
              </span>
              <StatusBadge status={o.status} />
            </li>
          ))}
        </ul>
      )}
      <Link to="/tienda" className={styles.cardLink}>
        Ir a la tienda
      </Link>
    </ProfileSection>
  );
}
