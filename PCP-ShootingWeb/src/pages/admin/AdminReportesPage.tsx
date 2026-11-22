import { useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, Chip, ChipGroup, DataTable, KpiCard, KpiGrid, TextField, type Column } from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { BarChart } from '@/features/admin/BarChart';
import { money, packageName, shortDayYear } from '@/features/admin/format';
import { todayISO } from '@/features/admin/seed';
import { downloadCsv, useAdmin } from '@/features/admin/store';
import { AdminBody, Section } from '@/features/admin/ui';
import { DATA_SOURCE } from '@/lib/api';
import AdminReportesApi from './AdminReportesApi';
import styles from './AdminShared.module.css';

type Cat = 'reservas' | 'tienda' | 'alimentos' | 'membresias' | 'eventos' | 'inventario';
const CATS: { id: Cat; label: string }[] = [
  { id: 'reservas', label: 'Reservas' },
  { id: 'tienda', label: 'Tienda' },
  { id: 'alimentos', label: 'Alimentos y bebidas' },
  { id: 'membresias', label: 'Membresías' },
  { id: 'eventos', label: 'Eventos' },
  { id: 'inventario', label: 'Inventario' },
];

const daysAgo = (n: number) => {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - n);
  return d.toISOString().slice(0, 10);
};
const short = (iso: string) => iso.slice(8, 10) + '/' + iso.slice(5, 7);

type Row = { key: string; cells: (string | number)[] };

/** Reportes (CLAUDE.md §7, sin diseño): 6 categorías con filtro de fechas, KPIs, gráfica, tabla y CSV. */
export default function AdminReportesPage() {
  return DATA_SOURCE === 'api' ? <AdminReportesApi /> : <AdminReportesDemo />;
}

function AdminReportesDemo() {
  const sales = useAdmin((d) => d.sales);
  const reservations = useAdmin((d) => d.reservations);
  const inventory = useAdmin((d) => d.inventory);
  const movements = useAdmin((d) => d.movements);
  const events = useAdmin((d) => d.events);
  const clients = useAdmin((d) => d.clients);
  const [cat, setCat] = useState<Cat>('reservas');
  const [from, setFrom] = useState(daysAgo(29));
  const [to, setTo] = useState(todayISO());

  const range = useMemo(() => sales.filter((s) => s.date >= from && s.date <= to), [sales, from, to]);
  const moneyKey = cat === 'inventario' ? null : cat;
  const series = moneyKey ? range.map((s) => ({ label: shortDayYear(s.date), value: s[moneyKey] })) : [];
  const total = series.reduce((s, p) => s + p.value, 0);
  const days = Math.max(1, series.length);

  const report = useMemo((): { headers: string[]; rows: Row[]; kpis: [string, string, string][] } => {
    switch (cat) {
      case 'reservas': {
        const inRange = reservations.filter((r) => r.date >= from && r.date <= to && r.status !== 'cancelada');
        const byPkg = Object.entries(
          inRange.reduce<Record<string, { n: number; people: number }>>((acc, r) => {
            const a = (acc[r.packageId] ??= { n: 0, people: 0 });
            a.n += 1;
            a.people += r.people;
            return acc;
          }, {}),
        );
        return {
          headers: ['Paquete', 'Reservas', 'Personas'],
          rows: byPkg.map(([id, v]) => ({ key: id, cells: [packageName(id), v.n, v.people] })),
          kpis: [
            ['Ingresos', money(total), `${days} días`],
            ['Reservas', String(inRange.length), `${inRange.filter((r) => r.status === 'utilizada').length} con llegada`],
            ['Promedio diario', money(Math.round(total / days)), 'Ingresos por día'],
          ],
        };
      }
      case 'inventario':
        return {
          headers: ['Artículo', 'Existencia', 'Mínimo', 'Movimientos'],
          rows: inventory.map((i) => ({ key: i.id, cells: [i.name, `${i.stock} ${i.unit}`, i.min, movements.filter((m) => m.itemId === i.id).length] })),
          kpis: [
            ['Artículos', String(inventory.length), 'En inventario'],
            ['Bajo mínimo', String(inventory.filter((i) => i.stock <= i.min).length), 'Requieren compra'],
            ['Movimientos', String(movements.length), 'Historial'],
          ],
        };
      case 'eventos':
        return {
          headers: ['Evento', 'Fecha', 'Inscritos', 'Cupo', 'Ingresos'],
          rows: events.map((e) => ({ key: e.id, cells: [e.name, e.date, e.enrollees.length, e.capacity, money(e.enrollees.filter((x) => x.paid).length * e.price)] })),
          kpis: [
            ['Ingresos', money(total), `${days} días`],
            ['Eventos', String(events.length), `${events.filter((e) => e.status === 'publicado').length} publicados`],
            ['Inscritos', String(events.reduce((s, e) => s + e.enrollees.length, 0)), 'Total'],
          ],
        };
      case 'membresias':
        return {
          headers: ['Plan', 'Socios'],
          rows: ['Tirador', 'Socio', 'Socio Oro'].map((p) => ({ key: p, cells: [p, clients.filter((c) => c.plan === p).length] })),
          kpis: [
            ['Ingresos', money(total), `${days} días`],
            ['Socios', String(clients.filter((c) => c.plan).length), 'Con membresía'],
            ['Promedio diario', money(Math.round(total / days)), 'Ingresos por día'],
          ],
        };
      default:
        return {
          headers: ['Fecha', 'Ingresos'],
          rows: range.map((s) => ({ key: s.date, cells: [short(s.date), money(s[cat])] })),
          kpis: [
            ['Ingresos', money(total), `${days} días`],
            ['Promedio diario', money(Math.round(total / days)), 'Ingresos por día'],
            ['Mejor día', money(Math.max(0, ...series.map((p) => p.value))), 'En el periodo'],
          ],
        };
    }
  }, [cat, reservations, inventory, movements, events, clients, range, from, to, total, days, series]);

  const cols: Column<Row>[] = report.headers.map((h, i) => ({
    key: h,
    header: h,
    primary: i === 0,
    strong: i === 0,
    render: (r) => r.cells[i],
  }));

  const label = CATS.find((c) => c.id === cat)!.label;

  return (
    <>
      <title>Reportes · Panel PCP</title>
      <AdminPageHeader
        title="Reportes"
        subtitle="Ventas, reservas, membresías, eventos e inventario por periodo"
        actions={
          <Button
            variant="secondary"
            iconStart={<IconDownload size={16} />}
            onClick={() => downloadCsv(`reporte-${cat}-${from}-a-${to}.csv`, [report.headers, ...report.rows.map((r) => r.cells)])}
          >
            Exportar CSV
          </Button>
        }
      />
      <AdminBody>
        {/* Filtros en una sola fila arriba de la gráfica. */}
        <div className={styles.toolbar}>
          <ChipGroup label="Categoría del reporte">
            {CATS.map((c) => (
              <Chip key={c.id} selected={cat === c.id} onClick={() => setCat(c.id)}>
                {c.label}
              </Chip>
            ))}
          </ChipGroup>
          <div className={styles.row}>
            <TextField label="Desde" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
            <TextField label="Hasta" type="date" value={to} min={from} max={todayISO()} onChange={(e) => setTo(e.target.value)} />
          </div>
        </div>

        <KpiGrid>
          {report.kpis.map(([l, v, h], i) => (
            <KpiCard key={l} label={l} value={v} hint={h} highlight={i === 0} />
          ))}
        </KpiGrid>

        {series.length > 0 && (
          <Section title={`Ingresos por día · ${label}`}>
            <BarChart data={series} format={money} title={`Ingresos por día de ${label}`} />
          </Section>
        )}

        <Section title={`Detalle · ${label}`}>
          <DataTable caption={`Detalle de ${label}`} columns={cols} rows={report.rows} rowKey={(r) => r.key} empty={<p className={styles.muted}>Sin datos en el periodo.</p>} />
          <p className={styles.muted}>Datos de demostración. Con la API: GET /api/reportes/{cat}?desde&hasta[&formato=csv].</p>
        </Section>
      </AdminBody>
    </>
  );
}
