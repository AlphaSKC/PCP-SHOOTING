import { useEffect, useState } from 'react';
import { Button, Chip, ChipGroup, DataTable, EmptyState, StatusBadge, TextField, type Column } from '@/components/ui';
import { when } from '@/features/admin/format';
import { listAllMovements, movementLabel, type Movement, type MovementFilters, type MovementKind } from '@/features/admin/inventarioApi';
import { Section } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

const PAGE_SIZE = 25;
const KINDS: { value: '' | MovementKind; label: string }[] = [
  { value: '', label: 'Todos' },
  { value: 0, label: 'Entradas' },
  { value: 1, label: 'Salidas' },
  { value: 2, label: 'Ajustes' },
];

/** Historial global de movimientos con autor (F13). Solo Administrador. */
export function AdminMovimientos() {
  const [filters, setFilters] = useState<MovementFilters>({ search: '', kind: '', from: '', to: '' });
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<Movement[]>([]);
  const [total, setTotal] = useState(0);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [error, setError] = useState('');
  const [reload, setReload] = useState(0);

  // La búsqueda espera a que el usuario deje de escribir.
  useEffect(() => {
    const t = window.setTimeout(() => {
      setFilters((f) => (f.search === search ? f : { ...f, search }));
      setPage(1);
    }, 350);
    return () => window.clearTimeout(t);
  }, [search]);

  useEffect(() => {
    let alive = true;
    setStatus('loading');
    listAllMovements(filters, page, PAGE_SIZE).then(
      (p) => {
        if (!alive) return;
        setRows(p.elementos);
        setTotal(p.total);
        setStatus('ready');
      },
      (e: unknown) => {
        if (!alive) return;
        setError(e instanceof ApiError ? e.message : 'No pudimos cargar el historial.');
        setStatus('error');
      },
    );
    return () => {
      alive = false;
    };
  }, [filters, page, reload]);

  const set = (patch: Partial<MovementFilters>) => {
    setFilters((f) => ({ ...f, ...patch }));
    setPage(1);
  };
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const cols: Column<Movement>[] = [
    { key: 'at', header: 'Fecha', render: (m) => when(m.at) },
    {
      key: 'item',
      header: 'Artículo',
      primary: true,
      render: (m) => (
        <span className={styles.stack}>
          <strong>
            {m.productName ?? 'Artículo'}
            {m.size ? ` · talla ${m.size}` : ''}
          </strong>
          <span>{m.sku ?? ''}</span>
        </span>
      ),
    },
    {
      key: 'type',
      header: 'Tipo',
      render: (m) => {
        const label = movementLabel(m.type);
        return <StatusBadge tone={label === 'Entrada' ? 'success' : label === 'Salida' ? 'warning' : 'neutral'}>{label}</StatusBadge>;
      },
    },
    { key: 'qty', header: 'Cantidad', strong: true, render: (m) => (m.qty > 0 ? `+${m.qty}` : String(m.qty)) },
    { key: 'why', header: 'Motivo', hideOnMobile: true, render: (m) => m.reason ?? '—' },
    { key: 'who', header: 'Registró', render: (m) => m.actorName ?? <span className={styles.muted}>Sistema / sin registro</span> },
  ];

  return (
    <Section title="Historial general de movimientos">
      <div className={styles.toolbar}>
        <ChipGroup label="Tipo de movimiento">
          {KINDS.map((k) => (
            <Chip key={k.label} selected={filters.kind === k.value} onClick={() => set({ kind: k.value })}>
              {k.label}
            </Chip>
          ))}
        </ChipGroup>
        <div className={styles.row}>
          <TextField label="Buscar artículo" placeholder="Nombre o SKU" value={search} onChange={(e) => setSearch(e.target.value)} />
          <TextField label="Desde" type="date" value={filters.from} max={filters.to || undefined} onChange={(e) => set({ from: e.target.value })} />
          <TextField label="Hasta" type="date" value={filters.to} min={filters.from || undefined} onChange={(e) => set({ to: e.target.value })} />
        </div>
      </div>
      {status === 'error' ? (
        <EmptyState title="No pudimos cargar el historial" description={error} action={<Button onClick={() => setReload((n) => n + 1)}>Reintentar</Button>} />
      ) : (
        <>
          <DataTable
            caption="Movimientos de inventario"
            columns={cols}
            rows={rows}
            rowKey={(m) => m.id}
            empty={<p className={styles.muted}>{status === 'loading' ? 'Cargando movimientos…' : 'No hay movimientos con estos filtros.'}</p>}
          />
          <div className={styles.row}>
            <span className={styles.muted}>
              {total} {total === 1 ? 'movimiento' : 'movimientos'} · página {page} de {pages}
            </span>
            <span className={styles.row}>
              <Button variant="secondary" disabled={page <= 1 || status === 'loading'} onClick={() => setPage((p) => p - 1)}>
                Anterior
              </Button>
              <Button variant="secondary" disabled={page >= pages || status === 'loading'} onClick={() => setPage((p) => p + 1)}>
                Siguiente
              </Button>
            </span>
          </div>
        </>
      )}
      <p className={styles.muted}>Las ventas pagadas aparecen como salidas automáticas «Pedido pagado». El autor es el nombre actual del perfil.</p>
    </Section>
  );
}
