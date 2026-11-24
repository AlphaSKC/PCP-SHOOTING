import { useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, Chip, ChipGroup, DataTable, FormAlert, KpiCard, KpiGrid, SelectField, StatusBadge, TextField, type Column } from '@/components/ui';
import { IconDownload, IconPlus } from '@/components/icons/icons';
import { when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { InventoryCategory, InventoryItem, InventoryMovement, MovementType } from '@/features/admin/seed';
import { addMovement, downloadCsv, newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, Section, Split } from '@/features/admin/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { DATA_SOURCE } from '@/lib/api';
import AdminInventarioApi from './AdminInventarioApi';
import styles from './AdminShared.module.css';

const CATS: { id: InventoryCategory | 'todos'; label: string }[] = [
  { id: 'todos', label: 'Todo' },
  { id: 'consumible', label: 'Consumibles' },
  { id: 'producto', label: 'Productos' },
  { id: 'alimento', label: 'Alimentos' },
  { id: 'bebida', label: 'Bebidas' },
];
const MOVES: { value: MovementType; label: string }[] = [
  { value: 'entrada', label: 'Entrada (compra, reposición)' },
  { value: 'salida', label: 'Salida (merma, uso interno)' },
  { value: 'ajuste', label: 'Ajuste (conteo físico, ±)' },
];

/** Inventario (CLAUDE.md §7, sin diseño): existencias, mínimos, alertas, movimientos e historial. */
export default function AdminInventarioPage() {
  return DATA_SOURCE === 'api' ? <AdminInventarioApi /> : <AdminInventarioDemo />;
}

function AdminInventarioDemo() {
  const { user } = useAuth();
  const items = useAdmin((d) => d.inventory);
  const movements = useAdmin((d) => d.movements);
  const [cat, setCat] = useState<InventoryCategory | 'todos'>('todos');
  const [onlyLow, setOnlyLow] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(items[0]?.id ?? null);
  const [move, setMove] = useState({ type: 'entrada' as MovementType, qty: '', note: '' });
  const [min, setMin] = useState('');
  const [msg, setMsg] = useState<string | null>(null);
  const [creating, setCreating] = useState<InventoryItem | null>(null);

  const visible = useMemo(() => items.filter((i) => (cat === 'todos' || i.category === cat) && (!onlyLow || i.stock <= i.min)), [items, cat, onlyLow]);
  const selected = items.find((i) => i.id === selectedId) ?? null;
  const low = items.filter((i) => i.stock <= i.min);
  const itemName = (id: string) => items.find((i) => i.id === id)?.name ?? id;

  const cols: Column<InventoryItem>[] = [
    { key: 'name', header: 'Artículo', primary: true, render: (i) => <span className={styles.stack}><strong>{i.name}</strong><span>{CATS.find((c) => c.id === i.category)?.label}</span></span> },
    { key: 'stock', header: 'Existencia', strong: true, render: (i) => `${i.stock} ${i.unit}` },
    { key: 'min', header: 'Mínimo', render: (i) => `${i.min} ${i.unit}` },
    { key: 'alert', header: 'Alerta', render: (i) => (i.stock === 0 ? <StatusBadge status="agotado" /> : i.stock <= i.min ? <StatusBadge status="bajo_minimo" /> : <StatusBadge tone="success">En orden</StatusBadge>) },
  ];
  const movCols: Column<InventoryMovement>[] = [
    { key: 'at', header: 'Fecha', render: (m) => when(m.at) },
    { key: 'item', header: 'Artículo', primary: true, render: (m) => <strong>{itemName(m.itemId)}</strong> },
    { key: 'type', header: 'Tipo', render: (m) => <StatusBadge tone={m.type === 'entrada' ? 'success' : m.type === 'salida' ? 'warning' : 'neutral'}>{m.type === 'entrada' ? 'Entrada' : m.type === 'salida' ? 'Salida' : 'Ajuste'}</StatusBadge> },
    { key: 'qty', header: 'Cantidad', strong: true, render: (m) => (m.qty > 0 ? `+${m.qty}` : String(m.qty)) },
    { key: 'note', header: 'Nota', hideOnMobile: true, render: (m) => m.note },
    { key: 'by', header: 'Usuario', hideOnMobile: true, render: (m) => m.by },
  ];

  return (
    <>
      <title>Inventario · Panel PCP</title>
      <AdminPageHeader
        title="Inventario"
        subtitle="Existencias de productos, alimentos, bebidas y consumibles"
        actions={
          <>
            <Button variant="secondary" iconStart={<IconDownload size={16} />} onClick={() => downloadCsv('inventario.csv', [['Artículo', 'Categoría', 'Existencia', 'Unidad', 'Mínimo'], ...items.map((i) => [i.name, i.category, i.stock, i.unit, i.min])])}>
              Exportar
            </Button>
            <Button iconStart={<IconPlus size={16} />} onClick={() => { setCreating({ id: '', name: '', category: 'consumible', unit: 'pzas', stock: 0, min: 0 }); setMsg(null); revealEditor(); }}>
              Nuevo artículo
            </Button>
          </>
        }
      />
      <AdminBody>
        <KpiGrid>
          <KpiCard label="Artículos" value={String(items.length)} hint="En inventario" highlight />
          <KpiCard label="Bajo mínimo" value={String(low.length)} hint={low.slice(0, 2).map((i) => i.name).join(', ') || 'Todo en orden'} />
          <KpiCard label="Agotados" value={String(items.filter((i) => i.stock === 0).length)} hint="Sin existencia" />
          <KpiCard label="Movimientos" value={String(movements.length)} hint="Historial registrado" />
        </KpiGrid>

        <Split
          main={
            <>
              <div className={styles.toolbar}>
                <ChipGroup label="Categoría">
                  {CATS.map((c) => <Chip key={c.id} selected={cat === c.id} onClick={() => setCat(c.id)}>{c.label}</Chip>)}
                  <Chip selected={onlyLow} onClick={() => setOnlyLow((v) => !v)}>Solo alertas ({low.length})</Chip>
                </ChipGroup>
              </div>
              <div className={styles.tableCard}>
                <DataTable caption="Existencias" columns={cols} rows={visible} rowKey={(i) => i.id} onRowClick={(i) => { setSelectedId(i.id); setCreating(null); setMin(String(i.min)); setMsg(null); revealEditor(); }} selectedKey={selectedId} />
              </div>
              <Section title="Historial de movimientos">
                <DataTable caption="Movimientos" columns={movCols} rows={movements.slice(0, 15)} rowKey={(m) => m.id} />
              </Section>
            </>
          }
          aside={
            creating ? (
              <Panel title="Nuevo artículo" id="editor">
                <TextField label="Nombre" value={creating.name} onChange={(e) => setCreating({ ...creating, name: e.target.value })} />
                <div className={styles.grid2}>
                  <SelectField label="Categoría" value={creating.category} onChange={(e) => setCreating({ ...creating, category: e.target.value as InventoryCategory })} options={CATS.slice(1).map((c) => ({ value: c.id, label: c.label }))} />
                  <TextField label="Unidad" value={creating.unit} onChange={(e) => setCreating({ ...creating, unit: e.target.value })} />
                  <TextField label="Existencia inicial" inputMode="numeric" value={creating.stock || ''} onChange={(e) => setCreating({ ...creating, stock: Number(e.target.value.replace(/\D/g, '')) })} />
                  <TextField label="Mínimo" inputMode="numeric" value={creating.min || ''} onChange={(e) => setCreating({ ...creating, min: Number(e.target.value.replace(/\D/g, '')) })} />
                </div>
                <Button
                  size="lg"
                  fullWidth
                  disabled={!creating.name.trim()}
                  onClick={() => {
                    const item = { ...creating, id: newId('I') };
                    update((d) => ({ ...d, inventory: [...d.inventory, item] }));
                    setSelectedId(item.id);
                    setCreating(null);
                    setMsg('Artículo creado.');
                  }}
                >
                  Crear artículo
                </Button>
              </Panel>
            ) : selected ? (
              <Panel title={selected.name} id="editor">
                <div className={styles.box}>
                  <span>Existencia</span>
                  <strong>{selected.stock} {selected.unit}</strong>
                </div>
                {msg && <FormAlert tone="success">{msg}</FormAlert>}
                <SelectField label="Movimiento" value={move.type} onChange={(e) => setMove({ ...move, type: e.target.value as MovementType })} options={MOVES} />
                <TextField
                  label={move.type === 'ajuste' ? 'Diferencia (+/−)' : 'Cantidad'}
                  inputMode={move.type === 'ajuste' ? 'text' : 'numeric'}
                  value={move.qty}
                  onChange={(e) => setMove({ ...move, qty: e.target.value.replace(move.type === 'ajuste' ? /[^\d-]/g : /\D/g, '') })}
                />
                <TextField label="Nota" placeholder="Proveedor, motivo, folio…" value={move.note} onChange={(e) => setMove({ ...move, note: e.target.value })} />
                <Button
                  size="lg"
                  fullWidth
                  disabled={!Number(move.qty)}
                  onClick={() => {
                    addMovement(selected.id, move.type, Number(move.qty), move.note || (move.type === 'ajuste' ? 'Ajuste manual' : ''), user?.name ?? 'Staff');
                    setMsg('Movimiento registrado.');
                    setMove({ ...move, qty: '', note: '' });
                  }}
                >
                  Registrar movimiento
                </Button>
                <div className={styles.row}>
                  <TextField label={`Mínimo (${selected.unit})`} inputMode="numeric" value={min} onChange={(e) => setMin(e.target.value.replace(/\D/g, ''))} />
                  <Button variant="secondary" disabled={min === '' || Number(min) === selected.min} onClick={() => { update((d) => ({ ...d, inventory: d.inventory.map((i) => (i.id === selected.id ? { ...i, min: Number(min) } : i)) })); setMsg('Mínimo actualizado.'); }}>
                    Guardar mínimo
                  </Button>
                </div>
                <p className={styles.muted}>Los consumibles se descuentan solos al registrar llegadas, según cada paquete.</p>
              </Panel>
            ) : (
              <Panel title="Artículo" id="editor"><p className={styles.muted}>Elige un artículo.</p></Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
