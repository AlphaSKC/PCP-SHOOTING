import { useCallback, useEffect, useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import {
  Button,
  Chip,
  ChipGroup,
  DataTable,
  EmptyState,
  FormAlert,
  KpiCard,
  KpiGrid,
  SelectField,
  StatusBadge,
  Switch,
  TextField,
  type Column,
} from '@/components/ui';
import { IconDownload, IconPlus } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { when } from '@/features/admin/format';
import {
  CATEGORY_LABEL,
  getSizeStock,
  listMovements,
  listStock,
  movementLabel,
  registerMovement,
  setMinimum,
  type Movement,
  type MovementKind,
  type StockItem,
} from '@/features/admin/inventarioApi';
import { createConsumable } from '@/features/admin/paquetesApi';
import { setProductActive } from '@/features/admin/productosApi';
import { revealEditor } from '@/features/admin/scroll';
import { downloadCsv } from '@/features/admin/store';
import { AdminBody, Panel, SegmentedTabs, Section, Split } from '@/features/admin/ui';
import { AdminMovimientos } from './AdminMovimientos';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Filter = 'todos' | 'consumibles' | 'tienda' | 'alimentos';
const FILTERS: { id: Filter; label: string; match: (i: StockItem) => boolean }[] = [
  { id: 'todos', label: 'Todo', match: () => true },
  { id: 'consumibles', label: 'Consumibles', match: (i) => i.category === 4 },
  { id: 'tienda', label: 'Tienda', match: (i) => i.category === 0 || i.category === 2 },
  { id: 'alimentos', label: 'Alimentos y bebidas', match: (i) => i.category === 1 },
];
const MOVES: { value: string; label: string }[] = [
  { value: '0', label: 'Entrada (compra, reposición)' },
  { value: '1', label: 'Salida (merma, uso interno)' },
  { value: '2', label: 'Ajuste (conteo físico, ±)' },
];

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

function alertBadge(i: StockItem) {
  if (!i.active) return <StatusBadge status="pausado" />;
  if (i.stock === 0) return <StatusBadge status="agotado" />;
  if (i.low) return <StatusBadge status="bajo_minimo" />;
  return <StatusBadge tone="success">En orden</StatusBadge>;
}

/** Inventario conectado a la API: existencias, alertas, movimientos, mínimos y consumibles. */
export default function AdminInventarioApi() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'admin';
  const [items, setItems] = useState<StockItem[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<Filter>('todos');
  const [onlyLow, setOnlyLow] = useState(false);
  const [q, setQ] = useState('');
  const [view, setView] = useState<'existencias' | 'historial'>('existencias');

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [history, setHistory] = useState<Movement[] | null>(null);
  const [move, setMove] = useState({ kind: '0', qty: '', reason: '', size: 'M', op: crypto.randomUUID() });
  const [sizes, setSizes] = useState<{ size: string; stock: number }[] | null>(null);
  const [min, setMin] = useState('');
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState<'move' | 'min' | 'active' | 'create' | null>(null);
  const [creating, setCreating] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const list = await listStock();
      setItems(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar el inventario.'));
      setStatus('error');
      return null;
    }
  }, []);

  const loadHistory = useCallback(async (id: string, usesSizes?: boolean) => {
    setHistory(null);
    setSizes(null);
    if (usesSizes) void getSizeStock(id).then(setSizes, () => setSizes(null));
    try {
      setHistory(await listMovements(id));
    } catch {
      setHistory([]);
    }
  }, []);

  useEffect(() => {
    void load().then((list) => {
      const first = list?.[0];
      if (first) {
        setSelectedId(first.id);
        setMin(first.min != null ? String(first.min) : '');
        void loadHistory(first.id, first.usesSizes);
      }
    });
  }, [load, loadHistory]);

  const selected = items.find((i) => i.id === selectedId) ?? null;
  const visible = useMemo(() => {
    const match = FILTERS.find((f) => f.id === filter)!.match;
    const t = q.trim().toLowerCase();
    return items.filter((i) => match(i) && (!onlyLow || (i.active && (i.low || i.stock === 0))) && (!t || `${i.name} ${i.sku}`.toLowerCase().includes(t)));
  }, [items, filter, onlyLow, q]);
  const alerts = items.filter((i) => i.active && (i.low || i.stock === 0));

  function select(i: StockItem) {
    setSelectedId(i.id);
    setCreating(null);
    setMin(i.min != null ? String(i.min) : '');
    setMove({ kind: '0', qty: '', reason: '', size: 'M', op: crypto.randomUUID() });
    setMsg(null);
    void loadHistory(i.id, i.usesSizes);
    revealEditor();
  }

  async function act(kind: NonNullable<typeof busy>, action: () => Promise<unknown>, ok: string) {
    setBusy(kind);
    setMsg(null);
    try {
      await action();
      await load();
      if (selectedId) void loadHistory(selectedId, items.find((x) => x.id === selectedId)?.usesSizes);
      setMsg({ tone: 'success', text: ok });
      return true;
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos completar la acción.') });
      return false;
    } finally {
      setBusy(null);
    }
  }

  async function onMove() {
    if (!selected) return;
    const kind = Number(move.kind) as MovementKind;
    const qty = Number(move.qty);
    if (!Number.isInteger(qty) || qty === 0 || (kind !== 2 && qty < 0)) return setMsg({ tone: 'error', text: kind === 2 ? 'Escribe la diferencia (ej. 5 o -3).' : 'Escribe una cantidad mayor a 0.' });
    if (!move.reason.trim()) return setMsg({ tone: 'error', text: 'Escribe el motivo (proveedor, merma, conteo…).' });
    const ok = await act('move', () => registerMovement(selected.id, kind, qty, move.reason, move.op, selected.usesSizes ? move.size : null), `${movementLabel(String(kind))} registrada${selected.usesSizes ? ` (talla ${move.size})` : ''}.`);
    // Un id nuevo solo tras el éxito: si falló, reintentar no duplica el movimiento.
    if (ok) setMove({ kind: move.kind, qty: '', reason: '', size: move.size, op: crypto.randomUUID() });
  }

  const cols: Column<StockItem>[] = [
    {
      key: 'name',
      header: 'Artículo',
      primary: true,
      render: (i) => (
        <span className={styles.stack}>
          <strong>{i.name}</strong>
          <span>
            {CATEGORY_LABEL[i.category]} · {i.sku}
          </span>
        </span>
      ),
    },
    { key: 'stock', header: 'Existencia', strong: true, render: (i) => `${i.stock}` },
    { key: 'min', header: 'Mínimo', render: (i) => (i.min != null ? String(i.min) : '—') },
    { key: 'alert', header: 'Estado', render: alertBadge },
  ];

  const movCols: Column<Movement>[] = [
    { key: 'at', header: 'Fecha', render: (m) => when(m.at) },
    {
      key: 'type',
      header: 'Tipo',
      primary: true,
      render: (m) => {
        const label = movementLabel(m.type);
        return <StatusBadge tone={label === 'Entrada' ? 'success' : label === 'Salida' ? 'warning' : 'neutral'}>{label}</StatusBadge>;
      },
    },
    { key: 'qty', header: 'Cantidad', strong: true, render: (m) => (m.qty > 0 ? `+${m.qty}` : String(m.qty)) },
    { key: 'size', header: 'Talla', render: (m) => m.size ?? '—', hideOnMobile: true },
    { key: 'who', header: 'Usuario', hideOnMobile: true, render: (m) => m.actorName ?? '—' },
    { key: 'why', header: 'Motivo', render: (m) => m.reason ?? '—' },
  ];

  return (
    <>
      <title>Inventario · Panel PCP</title>
      <AdminPageHeader
        title="Inventario"
        subtitle="Existencias de tienda, alimentos y consumibles"
        actions={
          <>
            <Button
              variant="secondary"
              iconStart={<IconDownload size={16} />}
              disabled={items.length === 0}
              onClick={() =>
                downloadCsv('inventario.csv', [
                  ['Artículo', 'SKU', 'Categoría', 'Existencia', 'Mínimo', 'Activo'],
                  ...items.map((i) => [i.name, i.sku, CATEGORY_LABEL[i.category], i.stock, i.min ?? '', i.active ? 'Sí' : 'No']),
                ])
              }
            >
              Exportar
            </Button>
            {isAdmin && (
              <Button iconStart={<IconPlus size={16} />} onClick={() => { setCreating(''); setMsg(null); revealEditor(); }}>
                Nuevo consumible
              </Button>
            )}
          </>
        }
      />
      <AdminBody>
        {isAdmin && (
          <SegmentedTabs
            label="Vista de inventario"
            value={view}
            onChange={setView}
            options={[
              { value: 'existencias', label: 'Existencias' },
              { value: 'historial', label: 'Historial general' },
            ]}
          />
        )}
        {view === 'historial' && isAdmin ? (
          <AdminMovimientos />
        ) : status === 'error' ? (
          <EmptyState title="No pudimos cargar el inventario" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <>
            <KpiGrid>
              <KpiCard label="Artículos" value={String(items.length)} hint="Con control de existencias" highlight />
              <KpiCard label="Bajo mínimo" value={String(items.filter((i) => i.active && i.low && i.stock > 0).length)} hint={alerts.slice(0, 2).map((i) => i.name).join(', ') || 'Todo en orden'} />
              <KpiCard label="Agotados" value={String(items.filter((i) => i.active && i.stock === 0).length)} hint="Activos sin existencia" />
              <KpiCard label="Inactivos" value={String(items.filter((i) => !i.active).length)} hint="Ocultos o pausados" />
            </KpiGrid>

            <Split
              main={
                <>
                  <div className={styles.toolbar}>
                    <ChipGroup label="Categoría">
                      {FILTERS.map((f) => (
                        <Chip key={f.id} selected={filter === f.id} onClick={() => setFilter(f.id)}>
                          {f.label}
                        </Chip>
                      ))}
                      <Chip selected={onlyLow} onClick={() => setOnlyLow((v) => !v)}>
                        Solo alertas ({alerts.length})
                      </Chip>
                    </ChipGroup>
                    <TextField label="Buscar" placeholder="Nombre o SKU" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
                  </div>
                  <div className={styles.tableCard}>
                    <DataTable
                      caption="Existencias"
                      columns={cols}
                      rows={visible}
                      rowKey={(i) => i.id}
                      onRowClick={select}
                      selectedKey={selectedId ?? undefined}
                      empty={<p className={styles.empty}>{status === 'loading' ? 'Cargando inventario…' : 'No hay artículos con este filtro.'}</p>}
                    />
                  </div>
                  {selected && (
                    <Section title={`Historial · ${selected.name}`}>
                      <DataTable
                        caption="Movimientos"
                        columns={movCols}
                        rows={(history ?? []).slice(0, 30)}
                        rowKey={(m) => m.id}
                        empty={<p className={styles.muted}>{history ? 'Sin movimientos todavía.' : 'Cargando historial…'}</p>}
                      />
                    </Section>
                  )}
                </>
              }
              aside={
                creating !== null ? (
                  <Panel title="Nuevo consumible" id="editor">
                    <p className={styles.muted}>
                      Material interno que se usa en los paquetes (diábolos, blancos…). No se vende. Para productos de la tienda usa Productos.
                    </p>
                    {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                    <TextField label="Nombre" maxLength={200} value={creating} onChange={(e) => setCreating(e.target.value)} />
                    <Button
                      size="lg"
                      fullWidth
                      loading={busy === 'create'}
                      disabled={!creating.trim()}
                      onClick={async () => {
                        setBusy('create');
                        setMsg(null);
                        try {
                          const id = await createConsumable(creating);
                          const list = await load();
                          const item = list?.find((i) => i.id === id);
                          setCreating(null);
                          if (item) select(item);
                          setMsg({ tone: 'success', text: 'Consumible creado. Registra su entrada inicial.' });
                        } catch (e) {
                          setMsg({ tone: 'error', text: errorText(e, 'No pudimos crear el consumible.') });
                        } finally {
                          setBusy(null);
                        }
                      }}
                    >
                      Crear consumible
                    </Button>
                    <Button variant="secondary" fullWidth onClick={() => setCreating(null)}>
                      Cancelar
                    </Button>
                  </Panel>
                ) : selected ? (
                  <Panel title={selected.name} id="editor" actions={alertBadge(selected)}>
                    <div className={styles.box}>
                      <span>Existencia actual{selected.usesSizes ? ' (todas las tallas)' : ''}</span>
                      <strong>{selected.stock}</strong>
                    </div>
                    {selected.usesSizes && (
                      <div className={styles.sizes}>
                        {(sizes ?? []).map((t) => (
                          <div key={t.size} className={styles.box}>
                            <span>{t.size}</span>
                            <strong>{t.stock}</strong>
                          </div>
                        ))}
                        {!sizes && <span className={styles.muted}>Cargando tallas…</span>}
                      </div>
                    )}
                    {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                    <SelectField label="Movimiento" value={move.kind} onChange={(e) => setMove({ ...move, kind: e.target.value })} options={MOVES} />
                    {selected.usesSizes && (
                      <SelectField
                        label="Talla"
                        value={move.size}
                        onChange={(e) => setMove({ ...move, size: e.target.value })}
                        options={['S', 'M', 'L', 'XL'].map((t) => ({ value: t, label: t }))}
                      />
                    )}
                    <TextField
                      label={move.kind === '2' ? 'Diferencia (+/−)' : 'Cantidad'}
                      hint={move.kind === '2' ? 'Ej. 5 si sobran, -3 si faltan.' : undefined}
                      inputMode={move.kind === '2' ? 'text' : 'numeric'}
                      value={move.qty}
                      onChange={(e) => setMove({ ...move, qty: e.target.value.replace(move.kind === '2' ? /[^\d-]/g : /\D/g, '') })}
                    />
                    <TextField label="Motivo" maxLength={500} placeholder="Proveedor, merma, conteo físico…" value={move.reason} onChange={(e) => setMove({ ...move, reason: e.target.value })} />
                    <Button size="lg" fullWidth loading={busy === 'move'} disabled={busy !== null} onClick={() => void onMove()}>
                      Registrar movimiento
                    </Button>
                    <div className={styles.grid2}>
                      <TextField label="Mínimo (alerta)" inputMode="numeric" value={min} onChange={(e) => setMin(e.target.value.replace(/\D/g, ''))} />
                      <Button
                        variant="secondary"
                        loading={busy === 'min'}
                        disabled={busy !== null || min === '' || Number(min) === selected.min}
                        onClick={() => void act('min', () => setMinimum(selected.id, Number(min)), 'Mínimo actualizado.')}
                      >
                        Guardar mínimo
                      </Button>
                    </div>
                    {isAdmin && (
                      <Switch
                        label={selected.category === 4 ? 'Activo (se puede usar en paquetes)' : 'Activo'}
                        showLabel
                        checked={selected.active}
                        disabled={busy !== null}
                        onChange={(active) => void act('active', () => setProductActive(selected.id, active), active ? 'Artículo activado.' : 'Artículo desactivado.')}
                      />
                    )}
                    <p className={styles.muted}>
                      Las ventas pagadas de la tienda descuentan solas. Los consumibles se descontarán al registrar llegadas (pendiente en backend).
                    </p>
                  </Panel>
                ) : (
                  <Panel title="Artículo" id="editor">
                    <p className={styles.muted}>{status === 'loading' ? 'Cargando…' : 'Elige un artículo.'}</p>
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
