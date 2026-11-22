import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonAnchor, Chip, ChipGroup, DataTable, EmptyState, FormAlert, KpiCard, KpiGrid, SelectField, StatusBadge, Switch, TextField, type Column } from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { money, shortDayYear, when } from '@/features/admin/format';
import {
  addMonths,
  currentMembership,
  getClientHistory,
  lastDay,
  listClients,
  renewMembership,
  setClientActive,
  todayClub,
  type Client,
  type ClientHistory,
} from '@/features/admin/clientesApi';
import { revealEditor } from '@/features/admin/scroll';
import { downloadCsv, waLink } from '@/features/admin/store';
import { AdminBody, DemoNote, Panel, Split } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Filter = 'todos' | 'activos' | 'inactivos';
type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);
const PAYMENT_LABEL = ['Pendiente', 'Confirmado', 'Rechazado', 'Cancelado'];
const ORDER_LABEL: Record<string, string> = { pendiente_pago: 'Pendiente de pago', pagado: 'Pagado', entregado: 'Recogido', cancelada: 'Cancelado' };

/** Clientes conectados a la API: lista, ficha con historial, cuenta activa y membresía CARTAQ. */
export default function AdminClientesApi() {
  const [clients, setClients] = useState<Client[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<Filter>('todos');
  const [q, setQ] = useState('');

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [history, setHistory] = useState<ClientHistory | null>(null);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState<'active' | 'renew' | null>(null);
  const [renew, setRenew] = useState({ paymentId: '', from: todayClub(), months: '12', op: crypto.randomUUID() });

  const load = useCallback(async () => {
    try {
      setClients(await listClients());
      setStatus('ready');
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los clientes.'));
      setStatus('error');
    }
  }, []);
  useEffect(() => void load(), [load]);

  const openHistory = useCallback(async (id: string) => {
    setHistory(null);
    setHistoryError(null);
    try {
      setHistory(await getClientHistory(id));
    } catch (e) {
      setHistoryError(errorText(e, 'No pudimos cargar la ficha.'));
    }
  }, []);

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase();
    return clients.filter(
      (c) => (filter === 'todos' || (filter === 'activos' ? c.active : !c.active)) && (!t || `${c.name} ${c.phone ?? ''}`.toLowerCase().includes(t)),
    );
  }, [clients, filter, q]);

  function pick(c: Client) {
    setSelectedId(c.id);
    setMsg(null);
    setRenew({ paymentId: '', from: todayClub(), months: '12', op: crypto.randomUUID() });
    void openHistory(c.id);
    revealEditor();
  }

  const selected = clients.find((c) => c.id === selectedId) ?? null;
  const membership = history ? currentMembership(history.memberships) : null;
  const usedPayments = new Set(history?.memberships.map((m) => m.paymentId));
  const usablePayments = (history?.payments ?? []).filter((p) => p.status === 1 && !usedPayments.has(p.id));

  async function onToggle(active: boolean) {
    if (!selected) return;
    if (!active && !window.confirm(`¿Desactivar la cuenta de ${selected.name}? No podrá iniciar sesión ni comprar.`)) return;
    setBusy('active');
    setMsg(null);
    try {
      await setClientActive(selected.id, active);
      await load();
      await openHistory(selected.id);
      setMsg({ tone: 'success', text: active ? 'Cuenta activada.' : 'Cuenta desactivada.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar la cuenta.') });
    } finally {
      setBusy(null);
    }
  }

  async function onRenew() {
    if (!selected) return;
    if (!renew.paymentId) return setMsg({ tone: 'error', text: 'Elige el pago confirmado con el que se cubrió la membresía.' });
    const months = Number(renew.months);
    if (!renew.from || !Number.isInteger(months) || months < 1) return setMsg({ tone: 'error', text: 'Indica la fecha de inicio y la duración en meses.' });
    setBusy('renew');
    setMsg(null);
    try {
      await renewMembership({ operationId: renew.op, userId: selected.id, paymentId: renew.paymentId, from: renew.from, untilExclusive: addMonths(renew.from, months) });
      await openHistory(selected.id);
      setRenew({ paymentId: '', from: todayClub(), months: renew.months, op: crypto.randomUUID() });
      setMsg({ tone: 'success', text: 'Membresía registrada.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos registrar la membresía.') });
    } finally {
      setBusy(null);
    }
  }

  const cols: Column<Client>[] = [
    {
      key: 'name',
      header: 'Cliente',
      primary: true,
      render: (c) => (
        <span className={styles.stack}>
          <strong>{c.name}</strong>
          <span>{c.phone ?? 'Sin teléfono'}</span>
        </span>
      ),
    },
    { key: 'status', header: 'Cuenta', render: (c) => <StatusBadge status={c.active ? 'activo' : 'pausado'} /> },
    {
      key: 'ficha',
      header: 'Acción',
      hideOnMobile: true,
      render: (c) => (
        <button type="button" className={styles.linkBtn} onClick={() => pick(c)}>
          Ver ficha
        </button>
      ),
    },
  ];

  return (
    <>
      <title>Clientes · Panel PCP</title>
      <AdminPageHeader
        title="Clientes"
        subtitle="Cuentas, historial de compras y membresías CARTAQ"
        actions={
          <Button
            variant="secondary"
            iconStart={<IconDownload size={16} />}
            disabled={clients.length === 0}
            onClick={() => downloadCsv('clientes.csv', [['Cliente', 'Teléfono', 'Cuenta'], ...clients.map((c) => [c.name, c.phone ?? '', c.active ? 'Activa' : 'Desactivada'])])}
          >
            Exportar
          </Button>
        }
      />
      <AdminBody>
        {status === 'error' ? (
          <EmptyState title="No pudimos cargar los clientes" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <>
            <KpiGrid>
              <KpiCard label="Clientes" value={String(clients.length)} hint="Cuentas registradas" highlight />
              <KpiCard label="Activos" value={String(clients.filter((c) => c.active).length)} hint="Pueden iniciar sesión" />
              <KpiCard label="Desactivados" value={String(clients.filter((c) => !c.active).length)} hint="Sin acceso" />
            </KpiGrid>
            <DemoNote>Cartas responsivas, correo y última visita aún no existen en la API (llegan con el registro de llegadas, R06).</DemoNote>

            <Split
              main={
                <>
                  <div className={styles.toolbar}>
                    <ChipGroup label="Filtrar">
                      <Chip selected={filter === 'todos'} onClick={() => setFilter('todos')}>Todos ({clients.length})</Chip>
                      <Chip selected={filter === 'activos'} onClick={() => setFilter('activos')}>Activos</Chip>
                      <Chip selected={filter === 'inactivos'} onClick={() => setFilter('inactivos')}>Desactivados</Chip>
                    </ChipGroup>
                    <TextField label="Buscar cliente" placeholder="Nombre o teléfono" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
                  </div>
                  <div className={styles.tableCard}>
                    <DataTable
                      caption="Clientes"
                      columns={cols}
                      rows={visible}
                      rowKey={(c) => c.id}
                      onRowClick={pick}
                      selectedKey={selectedId ?? undefined}
                      empty={<p className={styles.empty}>{status === 'loading' ? 'Cargando clientes…' : 'No hay clientes con este filtro.'}</p>}
                    />
                  </div>
                </>
              }
              aside={
                selected ? (
                  <Panel title={selected.name} id="editor" actions={<StatusBadge status={selected.active ? 'activo' : 'pausado'} />}>
                    <p className={styles.muted}>{selected.phone ?? 'Sin teléfono'}</p>
                    {selected.phone && (
                      <ButtonAnchor href={waLink(selected.phone, `Hola ${selected.name.split(' ')[0]}, te escribimos de PCP Shooting.`)} target="_blank" rel="noreferrer" variant="dark" fullWidth>
                        Escribir por WhatsApp
                      </ButtonAnchor>
                    )}
                    {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                    {historyError && <FormAlert>{historyError}</FormAlert>}
                    {!history && !historyError && <p className={styles.muted}>Cargando ficha…</p>}

                    {history && (
                      <>
                        <div className={styles.subsection}>
                          <h3 className={styles.subTitle}>Membresía CARTAQ</h3>
                          {membership ? (
                            <p>
                              <StatusBadge status="activo" /> Vigente hasta el <strong>{shortDayYear(lastDay(membership.untilExclusive))}</strong>
                            </p>
                          ) : (
                            <p className={styles.muted}>Sin membresía vigente.</p>
                          )}
                          {history.memberships.length > 0 && (
                            <ul className={styles.list}>
                              {history.memberships.map((m) => (
                                <li key={m.id} className={styles.muted}>
                                  {shortDayYear(m.from)} → {shortDayYear(lastDay(m.untilExclusive))}
                                </li>
                              ))}
                            </ul>
                          )}
                          <p className={styles.muted}>
                            Para registrar o renovar, el cliente debe tener un <strong>pago confirmado</strong> (de un pedido) que cubra la membresía. Renovación manual, sin cargos automáticos.
                          </p>
                          {usablePayments.length === 0 ? (
                            <p className={styles.muted}>No tiene pagos confirmados disponibles. Regístralo en <Link to="/admin/pedidos">Pedidos</Link>.</p>
                          ) : (
                            <>
                              <SelectField
                                label="Pago que cubre la membresía"
                                value={renew.paymentId}
                                placeholder="Elige el pago…"
                                onChange={(e) => setRenew({ ...renew, paymentId: e.target.value })}
                                options={usablePayments.map((p) => ({ value: p.id, label: `${money(p.amount)} · ${p.reference ?? 'sin referencia'}${p.confirmedAt ? ` · ${when(p.confirmedAt)}` : ''}` }))}
                              />
                              <div className={styles.grid2}>
                                <TextField label="Inicio" type="date" value={renew.from} onChange={(e) => setRenew({ ...renew, from: e.target.value })} />
                                <SelectField
                                  label="Duración"
                                  value={renew.months}
                                  onChange={(e) => setRenew({ ...renew, months: e.target.value })}
                                  options={[{ value: '1', label: '1 mes' }, { value: '3', label: '3 meses' }, { value: '6', label: '6 meses' }, { value: '12', label: '12 meses' }]}
                                />
                              </div>
                              <Button fullWidth loading={busy === 'renew'} disabled={busy !== null} onClick={() => void onRenew()}>
                                Registrar membresía
                              </Button>
                            </>
                          )}
                        </div>

                        <div className={styles.subsection}>
                          <h3 className={styles.subTitle}>{`Pedidos (${history.ordersTotal})`}</h3>
                          {history.orders.length === 0 ? (
                            <p className={styles.muted}>Sin pedidos.</p>
                          ) : (
                            <ul className={styles.list}>
                              {history.orders.slice(0, 10).map((o) => (
                                <li key={o.id} className={styles.row}>
                                  <span className={styles.stack}>
                                    <strong>#{o.folio}</strong>
                                    <span>{when(o.createdAt)}</span>
                                  </span>
                                  <span className={styles.stack}>
                                    <strong>{money(o.total)}</strong>
                                    <span>{ORDER_LABEL[o.status] ?? o.status}</span>
                                  </span>
                                </li>
                              ))}
                            </ul>
                          )}
                        </div>

                        <div className={styles.subsection}>
                          <h3 className={styles.subTitle}>Pagos</h3>
                          {history.payments.length === 0 ? (
                            <p className={styles.muted}>Sin pagos.</p>
                          ) : (
                            <ul className={styles.list}>
                              {history.payments.slice(0, 10).map((p) => (
                                <li key={p.id} className={styles.muted}>
                                  {money(p.amount)} · {p.reference ?? 'sin referencia'} · {PAYMENT_LABEL[p.status] ?? p.status}
                                </li>
                              ))}
                            </ul>
                          )}
                        </div>

                        <Switch label="Cuenta activa" showLabel checked={selected.active} disabled={busy !== null} onChange={(a) => void onToggle(a)} />
                      </>
                    )}
                  </Panel>
                ) : (
                  <Panel title="Ficha del cliente" id="editor">
                    <p className={styles.muted}>Elige un cliente de la lista.</p>
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
