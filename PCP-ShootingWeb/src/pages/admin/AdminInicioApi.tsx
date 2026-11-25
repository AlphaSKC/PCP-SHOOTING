import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { ButtonLink, KpiCard, KpiGrid } from '@/components/ui';
import { longDay, money, when } from '@/features/admin/format';
import { listStock, type StockItem } from '@/features/admin/inventarioApi';
import { listOrders, type AdminOrderApi } from '@/features/admin/pedidosApi';
import { clubDay, paymentsReport } from '@/features/admin/reportesApi';
import { AdminBody, Section } from '@/features/admin/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import shared from './AdminShared.module.css';

type Data<T> = { value: T | null; error: boolean };
const empty = <T,>(): Data<T> => ({ value: null, error: false });

/** Inicio del panel con datos reales (pedidos, ingresos del día e inventario). Reservas: pendiente de API (F02). */
export default function AdminInicioApi() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'admin';
  const today = clubDay(new Date().toISOString());
  const [orders, setOrders] = useState<Data<AdminOrderApi[]>>(empty);
  const [income, setIncome] = useState<Data<{ amount: number; count: number }>>(empty);
  const [stock, setStock] = useState<Data<StockItem[]>>(empty);

  useEffect(() => {
    // Cada bloque carga por separado: si uno falla (o no hay permiso), los demás se muestran.
    listOrders().then((v) => setOrders({ value: v, error: false }), () => setOrders({ value: null, error: true }));
    if (isAdmin) {
      paymentsReport(today, today).then(
        (r) => setIncome({ value: { amount: r.confirmed ?? r.rows.reduce((s, p) => s + p.importe, 0), count: r.total }, error: false }),
        () => setIncome({ value: null, error: true }),
      );
      listStock().then((v) => setStock({ value: v, error: false }), () => setStock({ value: null, error: true }));
    }
  }, [isAdmin, today]);

  const toCollect = orders.value?.filter((o) => o.status === 'pendiente_pago') ?? [];
  const toDeliver = orders.value?.filter((o) => o.status === 'pagado') ?? [];
  const alerts = stock.value?.filter((i) => i.active && (i.low || i.stock === 0)) ?? [];
  const nameOf = (o: AdminOrderApi) => o.customerName || `Cliente ${o.customerId.slice(0, 8)}`;
  const show = <T,>(d: Data<T>, render: (v: T) => string) => (d.error ? 'Sin datos' : d.value == null ? '…' : render(d.value));

  return (
    <>
      <title>Inicio · Panel PCP</title>
      <AdminPageHeader
        title="Inicio"
        subtitle={`${longDay(today)} · resumen del día`}
        actions={
          <ButtonLink to="/" variant="secondary">
            Ver sitio
          </ButtonLink>
        }
      />
      <AdminBody>
        <KpiGrid>
          {isAdmin && <KpiCard label="Ingresos de hoy" value={show(income, (v) => money(v.amount))} hint={show(income, (v) => `${v.count} pagos confirmados`)} highlight />}
          <KpiCard label="Pedidos por cobrar" value={show(orders, () => String(toCollect.length))} hint="Pendientes de pago" highlight={!isAdmin} />
          <KpiCard label="Pedidos por entregar" value={show(orders, () => String(toDeliver.length))} hint="Pagados, por recoger" />
          {isAdmin && <KpiCard label="Alertas de inventario" value={show(stock, () => String(alerts.length))} hint={alerts.slice(0, 2).map((i) => i.name).join(', ') || 'Todo en orden'} />}
        </KpiGrid>

        <div className={shared.grid2}>
          <Section title="Pedidos por entregar" actions={<Link to="/admin/pedidos">Ver pedidos</Link>}>
            {orders.error ? (
              <p className={shared.muted}>No pudimos cargar los pedidos.</p>
            ) : toDeliver.length === 0 ? (
              <p className={shared.muted}>{orders.value ? 'No hay pedidos pagados pendientes de entrega.' : 'Cargando…'}</p>
            ) : (
              <ul className={shared.list}>
                {toDeliver.slice(0, 6).map((o) => (
                  <li key={o.id} className={shared.row}>
                    <span className={shared.stack}>
                      <strong>#{o.folio}</strong>
                      <span>
                        {nameOf(o)} · {when(o.createdAt)}
                      </span>
                    </span>
                    <strong>{money(o.total)}</strong>
                  </li>
                ))}
              </ul>
            )}
          </Section>

          {isAdmin ? (
            <Section title="Inventario por reponer" actions={<Link to="/admin/inventario">Ver inventario</Link>}>
              {stock.error ? (
                <p className={shared.muted}>No pudimos cargar el inventario.</p>
              ) : alerts.length === 0 ? (
                <p className={shared.muted}>{stock.value ? 'Todo en orden.' : 'Cargando…'}</p>
              ) : (
                <ul className={shared.list}>
                  {alerts.slice(0, 6).map((i) => (
                    <li key={i.id} className={shared.row}>
                      <span>{i.name}</span>
                      <strong>
                        {i.stock}
                        {i.min != null ? ` / mín. ${i.min}` : ''}
                      </strong>
                    </li>
                  ))}
                </ul>
              )}
            </Section>
          ) : (
            <Section title="Pedidos por cobrar">
              <p className={shared.muted}>{toCollect.length} pedidos esperan pago. Confírmalos en Pedidos al recibir el pago.</p>
            </Section>
          )}
        </div>

        <Section title="Reservas de hoy">
          <p className={shared.muted}>
            Pendiente: el backend aún no permite al staff consultar las reservas de los clientes ni registrar llegadas (F02/F03 en el roadmap). Los
            clientes sí pueden reservar en línea.
          </p>
        </Section>
      </AdminBody>
    </>
  );
}
