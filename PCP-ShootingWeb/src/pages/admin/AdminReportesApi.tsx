import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, Chip, ChipGroup, DataTable, EmptyState, FormAlert, KpiCard, KpiGrid, StatusBadge, TextField, type Column } from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { BarChart } from '@/features/admin/BarChart';
import { STATE_VIEW } from '@/features/admin/reservasAdminApi';
import {
  activeMembershipsCount,
  clubDay,
  dailySeries,
  downloadReportCsv,
  foodReport,
  membershipsReport,
  ordersReport,
  paymentsReport,
  reservationsReport,
  stockReport,
  type ReportKind,
} from '@/features/admin/reportesApi';
import { money, shortDayYear, when } from '@/features/admin/format';
import { AdminBody, DemoNote, Section } from '@/features/admin/ui';
import { folioOf } from '@/features/tienda/api';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Cat = ReportKind | 'reservas' | 'alimentos' | 'eventos';
const CATS: { id: Cat; label: string; api: boolean }[] = [
  { id: 'pedidos', label: 'Ventas de tienda', api: true },
  { id: 'pagos', label: 'Ingresos (pagos)', api: true },
  { id: 'membresias', label: 'Membresías', api: true },
  { id: 'inventario', label: 'Inventario', api: true },
  { id: 'reservas', label: 'Reservas', api: true },
  { id: 'alimentos', label: 'Alimentos y bebidas', api: true },
  { id: 'eventos', label: 'Eventos', api: false },
];
const ORDER_STATE = ['Pendiente de pago', 'Pagado', 'Recogido', 'Cancelado'];
const PAYMENT_STATE = ['Pendiente', 'Confirmado', 'Rechazado', 'Cancelado'];
const CATEGORY = ['Artículo', 'Alimento', 'Playera', 'Catálogo de armas', 'Consumible'];

const today = () => clubDay(new Date().toISOString());
const daysAgo = (n: number) => {
  const d = new Date(`${today()}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() - n);
  return d.toISOString().slice(0, 10);
};

type Row = { key: string; cells: ReactNode[] };
type View = { headers: string[]; rows: Row[]; kpis: [string, string, string][]; series: { label: string; value: number }[]; seriesTitle: string };

/** Reportes con datos reales de la API (solo Administrador). */
export default function AdminReportesApi() {
  const [cat, setCat] = useState<Cat>('pedidos');
  const [from, setFrom] = useState(daysAgo(29));
  const [to, setTo] = useState(today());
  const [view, setView] = useState<View | null>(null);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  const [downloading, setDownloading] = useState(false);
  const [csvError, setCsvError] = useState<string | null>(null);

  const meta = CATS.find((c) => c.id === cat)!;
  /** Nombre actual del perfil que manda la API (null si el perfil ya no existe). */
  const nameOf = (name: string | null | undefined) => name ?? 'Sin perfil';

  useEffect(() => {
    if (!meta.api) return;
    let alive = true;
    setStatus('loading');
    setCsvError(null);
    (async (): Promise<View> => {
      const chart = (s: { date: string; value: number }[]) => s.map((p) => ({ label: shortDayYear(p.date), value: p.value }));
      if (cat === 'pedidos') {
        const { rows, total } = await ordersReport(from, to);
        const sold = rows.filter((o) => o.estado === 1 || o.estado === 2);
        const amount = sold.reduce((s, o) => s + o.total, 0);
        return {
          headers: ['Pedido', 'Fecha', 'Cliente', 'Productos', 'Estado', 'Total'],
          rows: rows.map((o) => ({
            key: o.id,
            cells: [<strong key="f">#{o.folio ?? folioOf(o.id)}</strong>, when(o.creadoEnUtc), nameOf(o.nombreCliente), o.productos?.length ? o.productos.join(', ') : '—', ORDER_STATE[o.estado] ?? o.estado, money(o.total)],
          })),
          kpis: [
            ['Vendido', money(amount), `${sold.length} pedidos pagados o recogidos`],
            ['Pedidos', String(total), 'Creados en el periodo'],
            ['Pendientes de pago', String(rows.filter((o) => o.estado === 0).length), `${rows.filter((o) => o.estado === 3).length} cancelados`],
          ],
          series: chart(dailySeries(sold, from, to, (o) => clubDay(o.creadoEnUtc), (o) => o.total)),
          seriesTitle: 'Ventas por día (pedidos pagados)',
        };
      }
      if (cat === 'pagos') {
        const { rows, total, confirmed } = await paymentsReport(from, to);
        const amount = confirmed ?? rows.reduce((s, p) => s + p.importe, 0);
        return {
          headers: ['Fecha', 'Cliente', 'Referencia', 'Estado', 'Importe'],
          rows: rows.map((p) => ({ key: p.id, cells: [p.confirmadoEnUtc ? when(p.confirmadoEnUtc) : '—', nameOf(p.nombreCliente), p.referenciaExterna ?? '—', PAYMENT_STATE[p.estado] ?? p.estado, money(p.importe)] })),
          kpis: [
            ['Ingresos confirmados', money(amount), 'Pagos confirmados en el periodo'],
            ['Pagos', String(total), 'Confirmados'],
            ['Ticket promedio', money(total ? Math.round((amount / total) * 100) / 100 : 0), 'Por pago'],
          ],
          series: chart(dailySeries(rows, from, to, (p) => (p.confirmadoEnUtc ? clubDay(p.confirmadoEnUtc) : null), (p) => p.importe)),
          seriesTitle: 'Ingresos por día',
        };
      }
      if (cat === 'membresias') {
        const [{ rows, total, confirmed }, active] = await Promise.all([membershipsReport(from, to), activeMembershipsCount(today())]);
        return {
          headers: ['Cliente', 'Inicio', 'Vence (último día)', 'Pago', 'Importe'],
          rows: rows.map((m) => {
            const end = new Date(`${m.finVigenciaExclusivo}T12:00:00Z`);
            end.setUTCDate(end.getUTCDate() - 1);
            return { key: m.id, cells: [nameOf(m.nombreCliente), shortDayYear(m.inicioVigencia), shortDayYear(end.toISOString().slice(0, 10)), PAYMENT_STATE[m.estadoPago] ?? m.estadoPago, money(m.importe)] };
          }),
          kpis: [
            ['Ingresos', money(confirmed ?? 0), 'Membresías iniciadas en el periodo'],
            ['Membresías', String(total), 'Iniciadas en el periodo'],
            ['Socios vigentes hoy', String(active), 'Con membresía activa'],
          ],
          series: chart(dailySeries(rows, from, to, (m) => m.inicioVigencia, (m) => m.importe)),
          seriesTitle: 'Ingresos por membresías (por fecha de inicio)',
        };
      }
      if (cat === 'reservas') {
        const { rows, total, confirmed } = await reservationsReport(from, to);
        const live = rows.filter((r) => r.estado !== 'Vencida');
        const paid = rows.filter((r) => r.estado === 'Pagada');
        return {
          headers: ['Folio', 'Actividad', 'Cliente', 'Paquete', 'Personas', 'Estado', 'Total'],
          rows: rows.map((r) => ({
            key: r.id,
            cells: [
              <strong key="f">#{r.folio}</strong>,
              `${shortDayYear(r.fecha)} · ${r.hora.slice(0, 5)}`,
              nameOf(r.nombreCliente),
              r.nombrePaquete,
              r.cantidadParticipantes,
              <StatusBadge key="s" status={STATE_VIEW[r.estado]?.status ?? 'pendiente_pago'} />,
              money(r.total),
            ],
          })),
          kpis: [
            ['Reservas pagadas', money(confirmed ?? 0), `${paid.length} ${paid.length === 1 ? 'reserva' : 'reservas'} con pago confirmado`],
            ['Reservas', String(total), `${live.reduce((s, r) => s + r.cantidadParticipantes, 0)} personas (sin vencidas)`],
            ['Pendientes de pago', String(rows.filter((r) => r.estado === 'PendientePago').length), `${rows.filter((r) => r.estado === 'Vencida').length} vencidas`],
          ],
          series: chart(dailySeries(paid, from, to, (r) => r.fecha, (r) => r.total)),
          seriesTitle: 'Reservas pagadas por día de actividad',
        };
      }
      if (cat === 'alimentos') {
        const { rows, confirmed } = await foodReport(from, to);
        const orders = new Set(rows.map((r) => r.pedidoId)).size;
        return {
          headers: ['Producto', 'Pedido', 'Fecha', 'Cliente', 'Cantidad', 'Precio', 'Total'],
          rows: rows.map((r) => ({
            key: r.id,
            cells: [<strong key="n">{r.nombreProducto}</strong>, `#${r.folioPedido}`, when(r.creadoEnUtc), nameOf(r.nombreCliente), r.cantidad, money(r.precioUnitario), money(r.total)],
          })),
          kpis: [
            ['Vendido', money(confirmed ?? rows.reduce((s, r) => s + r.total, 0)), 'Alimentos y bebidas pagados o recogidos'],
            ['Piezas', String(rows.reduce((s, r) => s + r.cantidad, 0)), `${rows.length} partidas`],
            ['Pedidos', String(orders), 'Con al menos un alimento'],
          ],
          series: chart(dailySeries(rows, from, to, (r) => clubDay(r.creadoEnUtc), (r) => r.total)),
          seriesTitle: 'Ventas de alimentos por día',
        };
      }
      const { rows } = await stockReport();
      const items = rows.filter((r) => r.categoria !== 3);
      return {
        headers: ['Artículo', 'Categoría', 'Existencia', 'Mínimo', 'Estado'],
        rows: items.map((r) => ({
          key: r.productoId,
          cells: [
            <strong key="n">{r.nombre}</strong>,
            CATEGORY[r.categoria] ?? r.categoria,
            r.existencia,
            r.stockMinimo ?? '—',
            !r.activo ? <StatusBadge key="s" status="pausado" /> : r.existencia === 0 ? <StatusBadge key="s" status="agotado" /> : r.bajoMinimo ? <StatusBadge key="s" status="bajo_minimo" /> : <StatusBadge key="s" tone="success">En orden</StatusBadge>,
          ],
        })),
        kpis: [
          ['Artículos', String(items.length), 'Con control de existencias'],
          ['Bajo mínimo', String(items.filter((r) => r.activo && r.bajoMinimo && r.existencia > 0).length), 'Requieren compra'],
          ['Agotados', String(items.filter((r) => r.activo && r.existencia === 0).length), 'Activos sin existencia'],
        ],
        series: [],
        seriesTitle: '',
      };
    })()
      .then((v) => {
        if (!alive) return;
        setView(v);
        setStatus('ready');
      })
      .catch((e: unknown) => {
        if (!alive) return;
        setError(e instanceof ApiError ? e.message : 'No pudimos generar el reporte.');
        setStatus('error');
      });
    return () => {
      alive = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cat, from, to, reload, meta.api]);

  const cols: Column<Row>[] = useMemo(
    () => (view?.headers ?? []).map((h, i) => ({ key: h, header: h, primary: i === 0, render: (r: Row) => r.cells[i] })),
    [view],
  );

  async function onCsv() {
    if (!meta.api) return;
    setDownloading(true);
    setCsvError(null);
    try {
      await downloadReportCsv(cat as ReportKind, from, to);
    } catch (e) {
      setCsvError(e instanceof ApiError ? e.message : 'No pudimos descargar el CSV.');
    } finally {
      setDownloading(false);
    }
  }

  const usesDates = cat !== 'inventario';

  return (
    <>
      <title>Reportes · Panel PCP</title>
      <AdminPageHeader
        title="Reportes"
        subtitle="Ventas, ingresos, reservas, alimentos, membresías e inventario con datos reales"
        actions={
          <Button variant="secondary" iconStart={<IconDownload size={16} />} loading={downloading} disabled={!meta.api} onClick={() => void onCsv()}>
            Exportar CSV
          </Button>
        }
      />
      <AdminBody>
        <div className={styles.toolbar}>
          <ChipGroup label="Categoría del reporte">
            {CATS.map((c) => (
              <Chip key={c.id} selected={cat === c.id} onClick={() => setCat(c.id)}>
                {c.label}
                {!c.api && ' · pendiente'}
              </Chip>
            ))}
          </ChipGroup>
          {usesDates && meta.api && (
            <div className={styles.row}>
              <TextField label="Desde" type="date" value={from} max={to} onChange={(e) => e.target.value && setFrom(e.target.value)} />
              <TextField label="Hasta" type="date" value={to} min={from} max={today()} onChange={(e) => e.target.value && setTo(e.target.value)} />
            </div>
          )}
        </div>

        {csvError && <FormAlert>{csvError}</FormAlert>}

        {!meta.api ? (
          <EmptyState
            title={`Reporte de ${meta.label.toLowerCase()}: pendiente`}
            description="La API todavía no tiene este reporte (el módulo de eventos con inscripciones está pendiente en el backend). No mostramos datos de ejemplo para no confundir."
          />
        ) : status === 'error' ? (
          <EmptyState title="No pudimos generar el reporte" description={error} action={<Button onClick={() => setReload((n) => n + 1)}>Reintentar</Button>} />
        ) : !view || status === 'loading' ? (
          <p className={styles.muted}>Generando reporte…</p>
        ) : (
          <>
            <KpiGrid>
              {view.kpis.map(([l, v, h], i) => (
                <KpiCard key={l} label={l} value={v} hint={h} highlight={i === 0} />
              ))}
            </KpiGrid>
            {view.series.length > 0 && view.series.some((p) => p.value > 0) && (
              <Section title={view.seriesTitle}>
                <BarChart data={view.series} format={money} title={view.seriesTitle} />
              </Section>
            )}
            <Section title={`Detalle · ${meta.label}`}>
              <DataTable caption={`Detalle de ${meta.label}`} columns={cols} rows={view.rows} rowKey={(r) => r.key} empty={<p className={styles.muted}>Sin datos en el periodo.</p>} />
              {cat === 'inventario' && <p className={styles.muted}>Existencias actuales (no depende de las fechas).</p>}
              {cat === 'reservas' && (
                <p className={styles.muted}>
                  Las fechas filtran el <strong>día de la actividad</strong>, no el de la compra. «Pagada» no significa que el cliente asistió: llegadas y ausencias llegan con el registro de llegadas (R06).
                </p>
              )}
              {cat === 'alimentos' && (
                <p className={styles.muted}>Una fila por producto vendido. Solo pedidos pagados o recogidos; las fechas filtran la creación del pedido.</p>
              )}
            </Section>
          </>
        )}
        <DemoNote>
          Los importes vienen del backend. No sumes reportes entre sí: pueden repetir importes («Ingresos (pagos)» y «Membresías» comparten pagos; «Alimentos» ya está dentro de «Ventas de tienda»).
        </DemoNote>
      </AdminBody>
    </>
  );
}
