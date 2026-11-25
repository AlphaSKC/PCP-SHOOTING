import { useCallback, useEffect, useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import {
  Button,
  ButtonAnchor,
  ButtonLink,
  Chip,
  ChipGroup,
  DataTable,
  EmptyState,
  FormAlert,
  STATUS,
  StatusBadge,
  TextField,
  type Column,
} from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { money, when } from '@/features/admin/format';
import {
  cancelOrderAdmin,
  getOrderDetail,
  listOrders,
  listProductNames,
  markPickedUp,
  registerAndConfirmPayment,
  type AdminOrderApi,
  type AdminOrderDetailApi,
} from '@/features/admin/pedidosApi';
import { revealEditor } from '@/features/admin/scroll';
import { downloadCsv, waLink } from '@/features/admin/store';
import { DEFAULT_ORDER_READY, fillTemplate, getTemplates } from '@/features/admin/avisosConfigApi';
import { useAuth } from '@/features/auth/AuthProvider';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import type { OrderStatus } from '@/features/tienda/types';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

/** Estados que maneja la API, en orden. */
const FILTERS: OrderStatus[] = ['pendiente_pago', 'pagado', 'entregado', 'cancelada'];
const LABEL: Partial<Record<OrderStatus, string>> = { entregado: 'Recogido', cancelada: 'Cancelado' };
const labelOf = (s: OrderStatus) => LABEL[s] ?? STATUS[s].label;
const PAYMENT_LABEL = { pendiente: 'Pendiente', confirmado: 'Confirmado', rechazado: 'Rechazado', cancelado: 'Cancelado' } as const;

const OrderBadge = ({ status }: { status: OrderStatus }) => <StatusBadge tone={STATUS[status].tone}>{labelOf(status)}</StatusBadge>;

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

/** Pedidos de la tienda conectados a la API real. */
export default function AdminPedidosApi() {
  // Plantilla «pedido listo» (F10). Solo el Administrador puede leerla; Recepción usa el texto estándar.
  const { user } = useAuth();
  const [template, setTemplate] = useState(DEFAULT_ORDER_READY);
  useEffect(() => {
    if (user?.role !== 'admin') return;
    let alive = true;
    getTemplates().then(
      (t) => alive && setTemplate(t.waOrderReady.trim() || DEFAULT_ORDER_READY),
      () => undefined,
    );
    return () => {
      alive = false;
    };
  }, [user?.role]);
  const [orders, setOrders] = useState<AdminOrderApi[]>([]);
  const [names, setNames] = useState<Map<string, string>>(new Map());
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<OrderStatus | 'todos'>('todos');
  const [q, setQ] = useState('');

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<AdminOrderDetailApi | null>(null);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState<'pay' | 'pickup' | 'cancel' | null>(null);
  const [payment, setPayment] = useState({ reference: '', op: crypto.randomUUID() });

  const load = useCallback(async () => {
    try {
      const [o, n] = await Promise.all([listOrders(), listProductNames()]);
      setOrders(o);
      setNames(n);
      setStatus('ready');
      return { o, n };
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los pedidos.'));
      setStatus('error');
      return null;
    }
  }, []);

  const openDetail = useCallback(async (id: string, productNames: Map<string, string>) => {
    setDetailError(null);
    try {
      const d = await getOrderDetail(id, productNames);
      setDetail(d);
      setOrders((list) => list.map((o) => (o.id === d.id ? { ...o, status: d.status, total: d.total } : o)));
    } catch (e) {
      setDetail(null);
      setDetailError(errorText(e, 'No pudimos cargar el pedido.'));
    }
  }, []);

  useEffect(() => {
    void load().then((r) => {
      const first = r?.o[0];
      if (r && first) {
        setSelectedId(first.id);
        void openDetail(first.id, r.n);
      }
    });
  }, [load, openDetail]);

  const customerOf = (o: AdminOrderApi) => ({ name: o.customerName, phone: o.customerPhone ?? '' });
  const nameOf = (o: AdminOrderApi) => o.customerName || `Cliente ${o.customerId.slice(0, 8)}`;

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase().replace(/^#/, '');
    const digits = t.replace(/\D/g, '');
    return orders.filter(
      (o) =>
        (filter === 'todos' || o.status === filter) &&
        (!t ||
          o.folio.toLowerCase().includes(t) ||
          (o.customerName ?? '').toLowerCase().includes(t) ||
          (digits.length >= 3 && (o.customerPhone ?? '').replace(/\D/g, '').includes(digits))),
    );
  }, [orders, filter, q]);

  function select(o: AdminOrderApi) {
    setSelectedId(o.id);
    setDetail(null);
    setMsg(null);
    setPayment({ reference: `MOSTRADOR-${o.folio}`, op: crypto.randomUUID() });
    void openDetail(o.id, names);
    revealEditor();
  }

  async function run(kind: 'pay' | 'pickup' | 'cancel', action: () => Promise<unknown>, ok: string) {
    if (!detail) return;
    setBusy(kind);
    setMsg(null);
    try {
      await action();
      await openDetail(detail.id, names);
      setMsg({ tone: 'success', text: ok });
      if (kind === 'pay') setPayment((p) => ({ ...p, op: crypto.randomUUID() }));
    } catch (e) {
      // Se refresca el detalle: un pago pudo quedar registrado aunque la confirmación fallara.
      await openDetail(detail.id, names);
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos completar la acción. Intenta de nuevo.') });
    } finally {
      setBusy(null);
    }
  }

  const onPay = () => {
    if (!detail) return;
    const hasPending = detail.payments.some((p) => p.status === 'pendiente');
    if (!hasPending && !payment.reference.trim()) return setMsg({ tone: 'error', text: 'Escribe la referencia del pago (folio de transferencia, recibo, etc.).' });
    void run('pay', () => registerAndConfirmPayment(detail, payment.reference, payment.op), 'Pago confirmado. El pedido quedó pagado y se descontó del inventario.');
  };

  const message = (o: AdminOrderApi) => {
    const first = nameOf(o).split(' ')[0] ?? '';
    if (o.status === 'pagado') return fillTemplate(template, { nombre: first, número: `#${o.folio}` });
    if (o.status === 'pendiente_pago')
      return `Hola ${first}, recibimos tu pedido #${o.folio} de PCP Shooting por ${money(o.total)}. Te compartimos cómo completar el pago para que puedas recogerlo en el club.`;
    return `Hola ${first}, tu pedido #${o.folio} de PCP Shooting está: ${labelOf(o.status)}.`;
  };

  const columns: Column<AdminOrderApi>[] = [
    {
      key: 'id',
      header: 'Pedido',
      strong: true,
      primary: true,
      render: (o) => (
        <button type="button" className={styles.linkBtn} onClick={() => select(o)}>
          #{o.folio}
        </button>
      ),
    },
    {
      key: 'client',
      header: 'Cliente',
      render: (o) => (
        <span className={styles.stack}>
          <strong>{nameOf(o)}</strong>
          <span>{when(o.createdAt)}</span>
        </span>
      ),
    },
    { key: 'total', header: 'Total', strong: true, render: (o) => money(o.total) },
    { key: 'status', header: 'Estado', render: (o) => <OrderBadge status={o.status} /> },
  ];

  const selected = orders.find((o) => o.id === selectedId) ?? null;
  const customer = selected ? customerOf(selected) : undefined;
  const pendingPayment = detail?.payments.find((p) => p.status === 'pendiente');

  return (
    <>
      <title>Pedidos · Panel PCP</title>
      <AdminPageHeader
        title="Pedidos"
        subtitle="Pedidos de la tienda · registra el pago, entrega y avisa al cliente"
        actions={
          <>
            <ButtonLink to="/tienda" variant="secondary">
              Ver sitio
            </ButtonLink>
            <Button
              variant="secondary"
              iconStart={<IconDownload size={16} />}
              disabled={orders.length === 0}
              onClick={() =>
                downloadCsv('pedidos-tienda.csv', [
                  ['Pedido', 'Cliente', 'Teléfono', 'Total', 'Fecha', 'Estado'],
                  ...orders.map((o) => [o.folio, nameOf(o), customerOf(o)?.phone ?? '', o.total, o.createdAt, labelOf(o.status)]),
                ])
              }
            >
              Exportar
            </Button>
          </>
        }
      />
      <AdminBody>
        {status === 'error' ? (
          <EmptyState title="No pudimos cargar los pedidos" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <>
            <div className={styles.toolbar}>
            <ChipGroup label="Filtrar por estado">
              <Chip selected={filter === 'todos'} onClick={() => setFilter('todos')}>
                Todos ({orders.length})
              </Chip>
              {FILTERS.map((s) => (
                <Chip key={s} selected={filter === s} onClick={() => setFilter(s)}>
                  {labelOf(s)} ({orders.filter((o) => o.status === s).length})
                </Chip>
              ))}
            </ChipGroup>
              <TextField label="Buscar pedido" placeholder="Folio, nombre o teléfono" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
            </div>

            <Split
              main={
                <div className={styles.tableCard}>
                  <DataTable
                    caption="Pedidos de la tienda"
                    columns={columns}
                    rows={visible}
                    rowKey={(o) => o.id}
                    onRowClick={select}
                    selectedKey={selectedId ?? undefined}
                    empty={
                      <p className={styles.empty}>
                        {status === 'loading' ? 'Cargando pedidos…' : orders.length === 0 ? 'Aún no hay pedidos.' : 'No hay pedidos con ese estado o búsqueda.'}
                      </p>
                    }
                  />
                </div>
              }
              aside={
                selected ? (
                  <Panel title={`Pedido #${selected.folio}`} id="editor" actions={<span className={styles.muted}>{when(selected.createdAt)}</span>}>
                    <div>
                      <p className={styles.big}>{nameOf(selected)}</p>
                      <p className={styles.muted}>
                        {customer?.phone || 'Sin teléfono'} · Recoger en el club
                      </p>
                    </div>

                    {detailError && <FormAlert>{detailError}</FormAlert>}
                    {!detail && !detailError && <p className={styles.muted}>Cargando detalle…</p>}

                    {detail && (
                      <>
                        <div className={styles.box}>
                          <span className={styles.stack}>
                            {detail.lines.map((l) => (
                              <span key={`${l.productId}:${l.size ?? ''}`}>
                                {l.name}
                                {l.size ? ` · talla ${l.size}` : ''} × {l.quantity} · {money(l.price * l.quantity)}
                              </span>
                            ))}
                          </span>
                          <strong>{money(detail.total)}</strong>
                        </div>

                        <div className={styles.row}>
                          <span className={styles.muted}>Estado</span>
                          <OrderBadge status={detail.status} />
                        </div>

                        {detail.payments.length > 0 && (
                          <div>
                            <span className={styles.muted}>Pagos</span>
                            {detail.payments.map((p) => (
                              <p key={p.id} className={styles.muted}>
                                {money(p.amount)} · {p.reference || 'Sin referencia'} · {PAYMENT_LABEL[p.status]}
                                {p.confirmedAt && ` · ${when(p.confirmedAt)}`}
                              </p>
                            ))}
                          </div>
                        )}

                        {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}

                        {detail.status === 'pendiente_pago' && (
                          <>
                            {pendingPayment ? (
                              <p className={styles.muted}>Hay un pago registrado sin confirmar ({pendingPayment.reference}). Confírmalo cuando lo recibas.</p>
                            ) : (
                              <TextField
                                label="Referencia del pago"
                                hint="Folio de transferencia, recibo de caja, etc. No se puede repetir entre pagos."
                                value={payment.reference}
                                maxLength={200}
                                onChange={(e) => setPayment({ ...payment, reference: e.target.value })}
                              />
                            )}
                            <Button size="lg" fullWidth loading={busy === 'pay'} disabled={busy !== null} onClick={onPay}>
                              Confirmar pago de {money(detail.total)}
                            </Button>
                            <p className={styles.muted}>Al confirmar, el pedido pasa a «Pagado» y se descuentan las piezas del inventario.</p>
                            <Button
                              variant="secondary"
                              fullWidth
                              loading={busy === 'cancel'}
                              disabled={busy !== null}
                              onClick={() => {
                                if (window.confirm(`¿Cancelar el pedido #${detail.folio}?`))
                                  void run('cancel', () => cancelOrderAdmin(detail.id), 'Pedido cancelado.');
                              }}
                            >
                              Cancelar pedido
                            </Button>
                          </>
                        )}

                        {detail.status === 'pagado' && (
                          <Button size="lg" fullWidth loading={busy === 'pickup'} disabled={busy !== null} onClick={() => void run('pickup', () => markPickedUp(detail.id), 'Pedido entregado al cliente.')}>
                            Marcar como recogido
                          </Button>
                        )}

                        {customer?.phone && (detail.status === 'pendiente_pago' || detail.status === 'pagado') && (
                          <div className={styles.waBox}>
                            <strong>Mensaje de WhatsApp</strong>
                            <p>{message(detail)}</p>
                            <ButtonAnchor href={waLink(customer.phone, message(detail))} target="_blank" rel="noreferrer" variant="dark" fullWidth>
                              Enviar por WhatsApp
                            </ButtonAnchor>
                          </div>
                        )}
                      </>
                    )}
                  </Panel>
                ) : (
                  <Panel title="Pedido" id="editor">
                    <p className={styles.muted}>{status === 'loading' ? 'Cargando…' : 'Elige un pedido de la lista.'}</p>
                  </Panel>
                )
              }
            />
          </>
        )}
      </AdminBody>
    </>
  );
}
