import { useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonAnchor, ButtonLink, Chip, ChipGroup, DataTable, FormAlert, STATUS, StatusBadge, type Column } from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { money, when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminOrder, OrderStatus } from '@/features/admin/seed';
import { downloadCsv, update, useAdmin, waLink } from '@/features/admin/store';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import styles from './AdminShared.module.css';

const FLOW: OrderStatus[] = ['pagado', 'preparando', 'listo_para_recoger', 'entregado'];

export default function AdminPedidosPage() {
  const orders = useAdmin((d) => d.orders);
  const template = useAdmin((d) => d.settings.waOrderReady);
  const [filter, setFilter] = useState<OrderStatus | 'todos'>('todos');
  const [selectedId, setSelectedId] = useState<string | null>(orders[0]?.id ?? null);
  const [next, setNext] = useState<OrderStatus | null>(null);
  const [saved, setSaved] = useState<string | null>(null);

  const visible = orders.filter((o) => filter === 'todos' || o.status === filter);
  const selected = orders.find((o) => o.id === selectedId) ?? null;
  const target = next ?? (selected ? FLOW[Math.min(FLOW.indexOf(selected.status) + 1, FLOW.length - 1)]! : null);

  const message = (o: AdminOrder) =>
    o.status === 'listo_para_recoger'
      ? template.replace('[nombre]', o.client.split(' ')[0] ?? '').replace('[número]', `#${o.id}`)
      : `Hola ${o.client.split(' ')[0]}, tu pedido #${o.id} de PCP Shooting está: ${STATUS[o.status].label}.`;

  const columns: Column<AdminOrder>[] = [
    {
      key: 'id',
      header: 'Pedido',
      strong: true,
      primary: true,
      render: (o) => (
        <button type="button" className={styles.linkBtn} onClick={() => select(o.id)}>
          #{o.id}
        </button>
      ),
    },
    {
      key: 'client',
      header: 'Cliente',
      render: (o) => (
        <span className={styles.stack}>
          <strong>{o.client}</strong>
          <span>{when(o.createdAt)}</span>
        </span>
      ),
    },
    { key: 'items', header: 'Productos', render: (o) => o.items, hideOnMobile: true },
    { key: 'total', header: 'Total', strong: true, render: (o) => money(o.total) },
    { key: 'status', header: 'Estado', render: (o) => <StatusBadge status={o.status} /> },
  ];

  function select(id: string) {
    setSelectedId(id);
    setNext(null);
    setSaved(null);
    revealEditor();
  }

  return (
    <>
      <title>Pedidos · Panel PCP</title>
      <AdminPageHeader
        title="Pedidos"
        subtitle="Pedidos de la tienda de ropa · cambia el estado y avisa al cliente"
        actions={
          <>
            <ButtonLink to="/" variant="secondary">
              Ver sitio
            </ButtonLink>
            <Button
              variant="secondary"
              iconStart={<IconDownload size={16} />}
              onClick={() =>
                downloadCsv('pedidos-tienda.csv', [
                  ['Pedido', 'Cliente', 'Teléfono', 'Productos', 'Total', 'Fecha', 'Estado'],
                  ...orders.map((o) => [o.id, o.client, o.phone, o.items, o.total, o.createdAt, STATUS[o.status].label]),
                ])
              }
            >
              Exportar
            </Button>
          </>
        }
      />
      <AdminBody>
        <ChipGroup label="Filtrar por estado">
          <Chip selected={filter === 'todos'} onClick={() => setFilter('todos')}>
            Todos ({orders.length})
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
                caption="Pedidos de la tienda"
                columns={columns}
                rows={visible}
                rowKey={(o) => o.id}
                onRowClick={(o) => select(o.id)}
                selectedKey={selectedId}
                empty={<p className={styles.empty}>No hay pedidos con este estado.</p>}
              />
            </div>
          }
          aside={
            selected ? (
              <Panel title={`Pedido #${selected.id}`} id="editor" actions={<span className={styles.muted}>{when(selected.createdAt)}</span>}>
                <div>
                  <p className={styles.big}>{selected.client}</p>
                  <p className={styles.muted}>{selected.phone} · Recoger en el club</p>
                </div>
                <div className={styles.box}>
                  <span>{selected.items}</span>
                  <strong>{money(selected.total)}</strong>
                </div>
                <fieldset className={styles.radios}>
                  <legend className="visually-hidden">Estado del pedido</legend>
                  {FLOW.map((s) => (
                    <label key={s} className={styles.radio}>
                      <input
                        type="radio"
                        name="estado"
                        checked={(next ?? selected.status) === s}
                        onChange={() => setNext(s)}
                      />
                      <span>
                        {STATUS[s].label}
                        {selected.status === s && <strong> · actual</strong>}
                      </span>
                    </label>
                  ))}
                </fieldset>
                {saved && <FormAlert tone="success">{saved}</FormAlert>}
                <Button
                  size="lg"
                  fullWidth
                  disabled={!target || target === selected.status}
                  onClick={() => {
                    if (!target) return;
                    update((d) => ({ ...d, orders: d.orders.map((o) => (o.id === selected.id ? { ...o, status: target } : o)) }));
                    setSaved(`Estado cambiado a "${STATUS[target].label}".`);
                    setNext(null);
                  }}
                >
                  {target && target !== selected.status ? `Cambiar a: ${STATUS[target].label}` : 'Sin cambios'}
                </Button>
                <p className={styles.muted}>Cada cambio de estado enviará un correo automático al cliente (al integrar la API).</p>
                <div className={styles.waBox}>
                  <strong>Mensaje de WhatsApp</strong>
                  <p>{message(selected)}</p>
                  <ButtonAnchor href={waLink(selected.phone, message(selected))} target="_blank" rel="noreferrer" variant="dark" fullWidth>
                    Enviar por WhatsApp
                  </ButtonAnchor>
                </div>
              </Panel>
            ) : (
              <Panel title="Pedido" id="editor">
                <p className={styles.muted}>Elige un pedido de la lista.</p>
              </Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
