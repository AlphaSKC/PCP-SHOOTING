import { useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, Chip, ChipGroup, DataTable, FormAlert, STATUS, SelectField, StatusBadge, TextField, type Column } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { FOOD_PAYMENT_MODE } from '@/features/alimentos/api';
import { money, when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminFoodOrder, FoodStatus } from '@/features/admin/seed';
import { newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import styles from './AdminShared.module.css';

const FLOW: FoodStatus[] = ['recibido', 'en_preparacion', 'listo', 'entregado'];

/** Pedidos de alimentos y bebidas + venta en mostrador (CLAUDE.md §7: sin diseño, sigue Pedidos). */
export default function AdminAlimentosPage() {
  const orders = useAdmin((d) => d.foodOrders);
  const [filter, setFilter] = useState<FoodStatus | 'activos'>('activos');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mode, setMode] = useState<'detail' | 'counter'>('detail');
  const [counter, setCounter] = useState({ items: '', total: '', where: 'Barra' });
  const [msg, setMsg] = useState<string | null>(null);

  const visible = orders
    .filter((o) => (filter === 'activos' ? o.status !== 'entregado' : o.status === filter))
    .sort((a, b) => FLOW.indexOf(a.status) - FLOW.indexOf(b.status) || a.at.localeCompare(b.at));
  const selected = orders.find((o) => o.id === selectedId) ?? null;

  const setStatus = (id: string, status: FoodStatus) =>
    update((d) => ({ ...d, foodOrders: d.foodOrders.map((o) => (o.id === id ? { ...o, status, paid: status === 'entregado' ? true : o.paid } : o)) }));

  const columns: Column<AdminFoodOrder>[] = [
    {
      key: 'id',
      header: 'Folio',
      primary: true,
      render: (o) => (
        <button type="button" className={styles.linkBtn} onClick={() => pick(o.id)}>
          {o.id}
        </button>
      ),
    },
    { key: 'client', header: 'Cliente', render: (o) => <span className={styles.stack}><strong>{o.client}</strong><span>{when(o.at)}</span></span> },
    { key: 'items', header: 'Pedido', render: (o) => o.items, hideOnMobile: true },
    { key: 'where', header: 'Entrega', render: (o) => o.where },
    { key: 'total', header: 'Total', strong: true, render: (o) => money(o.total) },
    { key: 'status', header: 'Estado', render: (o) => <StatusBadge status={o.status} /> },
  ];

  function pick(id: string) {
    setSelectedId(id);
    setMode('detail');
    setMsg(null);
    revealEditor();
  }

  return (
    <>
      <title>Alimentos · Panel PCP</title>
      <AdminPageHeader
        title="Alimentos y bebidas"
        subtitle="Pedidos entrantes por estado y venta en mostrador"
        actions={
          <Button iconStart={<IconPlus size={16} />} onClick={() => { setMode('counter'); setMsg(null); revealEditor(); }}>
            Venta en mostrador
          </Button>
        }
      />
      <AdminBody>
        <ChipGroup label="Filtrar por estado">
          <Chip selected={filter === 'activos'} onClick={() => setFilter('activos')}>
            En curso ({orders.filter((o) => o.status !== 'entregado').length})
          </Chip>
          {FLOW.map((s) => (
            <Chip key={s} selected={filter === s} onClick={() => setFilter(s)}>
              {STATUS[s].label} ({orders.filter((o) => o.status === s).length})
            </Chip>
          ))}
        </ChipGroup>
        <Split
          main={
            <div className={styles.tableCard}>
              <DataTable
                caption="Pedidos de alimentos"
                columns={columns}
                rows={visible}
                rowKey={(o) => o.id}
                onRowClick={(o) => pick(o.id)}
                selectedKey={selectedId}
                empty={<p className={styles.empty}>No hay pedidos aquí.</p>}
              />
            </div>
          }
          aside={
            mode === 'counter' ? (
              <Panel title="Venta en mostrador" id="editor">
                {msg && <FormAlert tone="success">{msg}</FormAlert>}
                <TextField label="Productos" placeholder="Ej. Agua natural ×2, Papas" value={counter.items} onChange={(e) => setCounter({ ...counter, items: e.target.value })} />
                <div className={styles.grid2}>
                  <TextField label="Total (MXN)" inputMode="numeric" value={counter.total} onChange={(e) => setCounter({ ...counter, total: e.target.value.replace(/\D/g, '') })} />
                  <SelectField
                    label="Entrega"
                    value={counter.where}
                    onChange={(e) => setCounter({ ...counter, where: e.target.value })}
                    options={['Barra', ...Array.from({ length: 15 }, (_, i) => `Línea ${i + 1}`)].map((v) => ({ value: v, label: v }))}
                  />
                </div>
                <Button
                  size="lg"
                  fullWidth
                  disabled={!counter.items.trim() || !counter.total}
                  onClick={() => {
                    const o: AdminFoodOrder = { id: newId('AB'), client: 'Mostrador', items: counter.items, total: Number(counter.total), where: counter.where, status: 'entregado', paid: true, at: new Date().toISOString().slice(0, 16) };
                    update((d) => ({ ...d, foodOrders: [o, ...d.foodOrders] }));
                    setCounter({ items: '', total: '', where: 'Barra' });
                    setMsg(`Venta ${o.id} registrada por ${money(o.total)}.`);
                  }}
                >
                  Registrar venta
                </Button>
                <p className={styles.muted}>Al integrar la API, cada venta descuenta existencias del inventario.</p>
              </Panel>
            ) : selected ? (
              <Panel title={`Pedido ${selected.id}`} id="editor" actions={<StatusBadge status={selected.status} />}>
                <div>
                  <p className={styles.big}>{selected.client}</p>
                  <p className={styles.muted}>Entrega: {selected.where} · {when(selected.at)}</p>
                </div>
                <div className={styles.box}>
                  <span>{selected.items}</span>
                  <strong>{money(selected.total)}</strong>
                </div>
                <p className={styles.muted}>
                  {selected.paid ? 'Pagado.' : FOOD_PAYMENT_MODE === 'en_club' ? 'Se cobra al entregar.' : 'Pendiente de pago en línea.'}
                </p>
                {FLOW.indexOf(selected.status) < FLOW.length - 1 && (
                  <Button size="lg" fullWidth onClick={() => setStatus(selected.id, FLOW[FLOW.indexOf(selected.status) + 1]!)}>
                    Marcar como {STATUS[FLOW[FLOW.indexOf(selected.status) + 1]!].label.toLowerCase()}
                  </Button>
                )}
                <fieldset className={styles.radios}>
                  <legend className={styles.muted}>Cambiar estado manualmente</legend>
                  {FLOW.map((s) => (
                    <label key={s} className={styles.radio}>
                      <input type="radio" name="estado-ab" checked={selected.status === s} onChange={() => setStatus(selected.id, s)} />
                      <span>{STATUS[s].label}</span>
                    </label>
                  ))}
                </fieldset>
              </Panel>
            ) : (
              <Panel title="Pedido" id="editor">
                <p className={styles.muted}>Elige un pedido para cambiar su estado: recibido → en preparación → listo → entregado.</p>
              </Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
