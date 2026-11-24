import { useMemo } from 'react';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { ButtonLink, KpiCard, KpiGrid, StatusBadge } from '@/components/ui';
import { IconBox, IconCalendar, IconPlus, IconQr, IconStar } from '@/components/icons/icons';
import { longDay, money, packageName, reservationBadge, timeToMinutes } from '@/features/admin/format';
import { todayISO } from '@/features/admin/seed';
import { useAdmin } from '@/features/admin/store';
import { AdminBody, ProgressBar, Section } from '@/features/admin/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { DATA_SOURCE } from '@/lib/api';
import AdminInicioApi from './AdminInicioApi';
import styles from './AdminInicioPage.module.css';

export default function AdminInicioPage() {
  return DATA_SOURCE === 'api' ? <AdminInicioApi /> : <AdminInicioDemo />;
}

function AdminInicioDemo() {
  const { user } = useAuth();
  const reservations = useAdmin((d) => d.reservations);
  const orders = useAdmin((d) => d.orders);
  const events = useAdmin((d) => d.events);
  const sales = useAdmin((d) => d.sales);
  const inventory = useAdmin((d) => d.inventory);
  const isAdmin = user?.role === 'admin';
  const today = todayISO();

  const agenda = useMemo(
    () =>
      reservations
        .filter((r) => r.date === today && r.status !== 'cancelada')
        .sort((a, b) => timeToMinutes(a.time) - timeToMinutes(b.time) || a.line - b.line),
    [reservations, today],
  );
  const people = agenda.reduce((s, r) => s + r.people, 0);
  const pending = agenda.filter((r) => r.status === 'confirmada').length;
  const toPrepare = orders.filter((o) => o.status === 'pagado' || o.status === 'preparando');
  const ready = orders.filter((o) => o.status === 'listo_para_recoger').length;
  const todaySales = sales[sales.length - 1];
  const totalToday = todaySales ? todaySales.reservas + todaySales.tienda + todaySales.alimentos : 0;
  const lowStock = inventory.filter((i) => i.stock <= i.min);

  return (
    <>
      <title>Inicio · Panel PCP</title>
      <AdminPageHeader
        title="Inicio"
        subtitle={`${longDay(today)} · resumen del día`}
        actions={
          <>
            <ButtonLink to="/" variant="secondary">
              Ver sitio
            </ButtonLink>
            <ButtonLink to="/admin/reservas?nueva=1" iconStart={<IconPlus size={16} />}>
              Nueva reserva
            </ButtonLink>
          </>
        }
      />
      <AdminBody>
        <KpiGrid>
          <KpiCard label="Reservas hoy" value={String(agenda.length)} hint={`${people} personas · ${pending} por llegar`} highlight />
          <KpiCard
            label="Ventas del día"
            value={money(totalToday)}
            hint={todaySales ? `Reservas ${money(todaySales.reservas)} · Tienda ${money(todaySales.tienda)}` : ''}
          />
          <KpiCard label="Pedidos por preparar" value={String(toPrepare.length)} hint={`${ready} ${ready === 1 ? 'listo' : 'listos'} para recoger`} />
          <KpiCard
            label="Inventario bajo mínimo"
            value={String(lowStock.length)}
            hint={lowStock.length ? lowStock.slice(0, 2).map((i) => i.name).join(', ') : 'Todo en orden'}
          />
        </KpiGrid>

        <div className={styles.quick}>
          <Link to="/admin/reservas#llegada" className={`${styles.action} ${styles.actionPrimary}`}>
            <IconQr size={22} />
            <span>
              <strong>Registrar llegada</strong>
              <small>Escanear QR o código</small>
            </span>
          </Link>
          <Link to="/admin/reservas?nueva=1" className={`${styles.action} ${styles.actionDark}`}>
            <IconCalendar size={22} />
            <span>
              <strong>Nueva reserva</strong>
              <small>Por teléfono o en mostrador</small>
            </span>
          </Link>
          {isAdmin && (
            <>
              <Link to="/admin/productos?nuevo=1" className={styles.action}>
                <IconBox size={22} />
                <span>
                  <strong>Nuevo producto</strong>
                  <small>Fotos, precio y tallas</small>
                </span>
              </Link>
              <Link to="/admin/eventos?nuevo=1" className={styles.action}>
                <IconStar size={22} />
                <span>
                  <strong>Nuevo evento</strong>
                  <small>Fecha, cupo y precio</small>
                </span>
              </Link>
            </>
          )}
        </div>

        <div className={styles.grid}>
          <Section title="Agenda de hoy" actions={<Link to="/admin/reservas">Ver calendario</Link>} className={styles.agenda}>
            {agenda.length === 0 ? (
              <p className={styles.muted}>No hay reservas para hoy.</p>
            ) : (
              <ul className={styles.list}>
                {agenda.slice(0, 8).map((r) => (
                  <li key={r.id}>
                    <strong className={styles.time}>{r.time}</strong>
                    <span className={styles.who}>
                      <strong>{r.client}</strong>
                      <span>
                        {packageName(r.packageId)} · {r.people} pers. · Línea {r.line}
                      </span>
                    </span>
                    <StatusBadge status={reservationBadge(r)} />
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <div className={styles.side}>
            <Section title="Pedidos por atender" actions={<Link to="/admin/pedidos">Ver todos</Link>}>
              <ul className={styles.list}>
                {orders
                  .filter((o) => o.status !== 'entregado')
                  .slice(0, 4)
                  .map((o) => (
                    <li key={o.id}>
                      <span className={styles.who}>
                        <strong>#{o.id}</strong>
                        <span>{o.items}</span>
                      </span>
                      <StatusBadge status={o.status} />
                    </li>
                  ))}
              </ul>
            </Section>

            {isAdmin && (
              <Section title="Próximos eventos" actions={<Link to="/admin/eventos">Ver todos</Link>}>
                <ul className={styles.events}>
                  {events
                    .filter((e) => e.status === 'publicado')
                    .slice(0, 3)
                    .map((e) => (
                      <li key={e.id}>
                        <div className={styles.eventHead}>
                          <strong>{e.name}</strong>
                          <span>{e.date.split('-').reverse().join('/')}</span>
                        </div>
                        <ProgressBar value={e.enrollees.length} max={e.capacity} label={`Cupo de ${e.name}`} />
                        <small>
                          {e.enrollees.length} de {e.capacity} lugares ocupados
                        </small>
                      </li>
                    ))}
                </ul>
              </Section>
            )}
          </div>
        </div>
      </AdminBody>
    </>
  );
}
