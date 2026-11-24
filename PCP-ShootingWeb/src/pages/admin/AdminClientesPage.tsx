import { useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonAnchor, Chip, ChipGroup, DataTable, FormAlert, KpiCard, KpiGrid, SelectField, StatusBadge, TextField, type Column } from '@/components/ui';
import { IconDownload } from '@/components/icons/icons';
import { PLANS } from '@/features/membresias/plans';
import { money, shortDayYear } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import { todayISO, type AdminClient } from '@/features/admin/seed';
import { downloadCsv, update, useAdmin, waLink } from '@/features/admin/store';
import { AdminBody, Panel, Section, Split } from '@/features/admin/ui';
import { DATA_SOURCE } from '@/lib/api';
import AdminClientesApi from './AdminClientesApi';
import styles from './AdminShared.module.css';

type Filter = 'todos' | 'activos' | 'por_vencer' | 'sin' | 'carta';
const DAY = 86400000;

function membershipState(c: AdminClient): 'activo' | 'por_vencer' | 'vencida' | null {
  if (!c.plan || !c.validUntil) return null;
  const left = (new Date(c.validUntil).getTime() - new Date(todayISO()).getTime()) / DAY;
  return left < 0 ? 'vencida' : left <= 30 ? 'por_vencer' : 'activo';
}

export default function AdminClientesPage() {
  return DATA_SOURCE === 'api' ? <AdminClientesApi /> : <AdminClientesDemo />;
}

function AdminClientesDemo() {
  const clients = useAdmin((d) => d.clients);
  const [filter, setFilter] = useState<Filter>('todos');
  const [q, setQ] = useState('');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [renewPlan, setRenewPlan] = useState(PLANS[0]!.name);
  const [renewMonths, setRenewMonths] = useState('1');
  const [msg, setMsg] = useState<string | null>(null);

  const visible = useMemo(
    () =>
      clients
        .filter((c) => `${c.name} ${c.email} ${c.phone}`.toLowerCase().includes(q.toLowerCase()))
        .filter((c) => {
          const s = membershipState(c);
          return filter === 'todos' ? true : filter === 'activos' ? s === 'activo' || s === 'por_vencer' : filter === 'por_vencer' ? s === 'por_vencer' : filter === 'sin' ? !c.plan || s === 'vencida' : c.waiver === 'pendiente';
        }),
    [clients, q, filter],
  );
  const selected = clients.find((c) => c.id === selectedId) ?? null;
  const active = clients.filter((c) => membershipState(c) === 'activo' || membershipState(c) === 'por_vencer');

  const cols: Column<AdminClient>[] = [
    { key: 'name', header: 'Cliente', primary: true, render: (c) => <span className={styles.stack}><strong>{c.name}</strong><span>{c.email} · {c.phone}</span></span> },
    { key: 'plan', header: 'Membresía', render: (c) => (c.plan ? <StatusBadge tone={c.plan === 'Socio Oro' ? 'gold' : 'neutral'}>{c.plan}</StatusBadge> : <span className={styles.muted}>Sin membresía</span>) },
    {
      key: 'vence',
      header: 'Vence',
      render: (c) => {
        const s = membershipState(c);
        return s === 'por_vencer' || s === 'vencida' ? <StatusBadge status={s} /> : c.validUntil ? shortDayYear(c.validUntil) : '—';
      },
    },
    { key: 'waiver', header: 'Carta responsiva', render: (c) => <StatusBadge status={c.waiver} /> },
    { key: 'visit', header: 'Última visita', hideOnMobile: true, render: (c) => (c.lastVisit ? shortDayYear(c.lastVisit) : '—') },
    { key: 'ficha', header: 'Acción', hideOnMobile: true, render: (c) => <button type="button" className={styles.linkBtn} onClick={() => pick(c.id)}>Ver ficha</button> },
  ];

  function pick(id: string) {
    setSelectedId(id);
    setMsg(null);
    revealEditor();
  }

  return (
    <>
      <title>Clientes y socios · Panel PCP</title>
      <AdminPageHeader
        title="Clientes y socios"
        subtitle="Membresías CARTAQ, vencimientos y cartas responsivas firmadas"
        actions={
          <Button
            variant="secondary"
            iconStart={<IconDownload size={16} />}
            onClick={() => downloadCsv('clientes.csv', [['Nombre', 'Correo', 'Teléfono', 'Membresía', 'Vence', 'Carta'], ...clients.map((c) => [c.name, c.email, c.phone, c.plan ?? '', c.validUntil ?? '', c.waiver])])}
          >
            Exportar
          </Button>
        }
      />
      <AdminBody>
        <KpiGrid>
          <KpiCard label="Clientes registrados" value={String(clients.length)} hint="En el sitio web" highlight />
          <KpiCard label="Socios activos" value={String(active.length)} hint="Tirador, Socio y Socio Oro" />
          <KpiCard label="Membresías por vencer" value={String(clients.filter((c) => membershipState(c) === 'por_vencer').length)} hint="En los próximos 30 días" />
          <KpiCard label="Cartas pendientes" value={String(clients.filter((c) => c.waiver === 'pendiente').length)} hint="Clientes sin firmar" />
        </KpiGrid>

        <Split
          main={
            <Section>
              <div className={styles.toolbar}>
                <ChipGroup label="Filtrar clientes">
                  {([['todos', 'Todos'], ['activos', 'Socios activos'], ['por_vencer', 'Por vencer'], ['sin', 'Sin membresía'], ['carta', 'Carta pendiente']] as [Filter, string][]).map(([id, label]) => (
                    <Chip key={id} selected={filter === id} onClick={() => setFilter(id)}>{label}</Chip>
                  ))}
                </ChipGroup>
                <TextField label="Buscar cliente" placeholder="Nombre, correo o teléfono" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
              </div>
              <DataTable caption="Clientes" columns={cols} rows={visible} rowKey={(c) => c.id} onRowClick={(c) => pick(c.id)} selectedKey={selectedId} empty={<p className={styles.muted}>Sin resultados.</p>} />
            </Section>
          }
          aside={
            selected ? (
              <Panel title="Ficha del cliente" id="editor">
                <div>
                  <p className={styles.big}>{selected.name}</p>
                  <p className={styles.muted}>{selected.email} · {selected.phone}</p>
                </div>
                <div className={styles.box}>
                  <span>{selected.plan ?? 'Sin membresía'}</span>
                  <strong>{selected.validUntil ? `Vence ${shortDayYear(selected.validUntil)}` : '—'}</strong>
                </div>
                <div className={styles.row}>
                  <span>Carta responsiva</span>
                  <StatusBadge status={selected.waiver} />
                </div>
                {/* PENDIENTE (CLAUDE.md §8): mecanismo legal de la carta; el PDF lo generará la API. */}
                <Button variant="text" disabled className={styles.linkBtn}>Descargar carta (PDF)</Button>
                {msg && <FormAlert tone="success">{msg}</FormAlert>}
                <p className={styles.muted}>Renovación manual (pago único por periodo). Usa esto para pagos en mostrador.</p>
                <div className={styles.grid2}>
                  <SelectField label="Plan" value={renewPlan} onChange={(e) => setRenewPlan(e.target.value)} options={PLANS.map((p) => ({ value: p.name, label: p.name }))} />
                  <SelectField label="Periodo" value={renewMonths} onChange={(e) => setRenewMonths(e.target.value)} options={[{ value: '1', label: '1 mes' }, { value: '12', label: '12 meses' }]} />
                </div>
                <Button
                  size="lg"
                  fullWidth
                  onClick={() => {
                    const base = selected.validUntil && selected.validUntil > todayISO() ? new Date(selected.validUntil) : new Date();
                    base.setMonth(base.getMonth() + Number(renewMonths));
                    const validUntil = base.toISOString().slice(0, 10);
                    update((d) => ({ ...d, clients: d.clients.map((c) => (c.id === selected.id ? { ...c, plan: renewPlan, validUntil } : c)) }));
                    setMsg(`Membresía ${renewPlan} registrada hasta ${shortDayYear(validUntil)}.`);
                  }}
                >
                  Registrar pago de membresía
                </Button>
                <ButtonAnchor href={waLink(selected.phone, `Hola ${selected.name.split(' ')[0]}, te escribimos de PCP Shooting.`)} target="_blank" rel="noreferrer" variant="secondary" fullWidth>
                  WhatsApp
                </ButtonAnchor>
              </Panel>
            ) : (
              <Panel title="Ficha del cliente" id="editor">
                <p className={styles.muted}>Elige un cliente para ver su membresía, carta responsiva y registrar renovaciones.</p>
              </Panel>
            )
          }
        />

        <Section title="Planes de membresía">
          <div className={styles.grid3}>
            {PLANS.map((p) => (
              <div key={p.id} className={styles.box}>
                <span className={styles.stack}>
                  <StatusBadge tone={p.variant === 'gold' ? 'gold' : 'neutral'}>{p.name}</StatusBadge>
                  <span>{clients.filter((c) => c.plan === p.name).length} socios · {money(p.monthly)} por mes</span>
                </span>
              </div>
            ))}
          </div>
          <p className={styles.muted}>Nombres, precios y beneficios están pendientes de definir con el club (se editarán aquí al integrar la API).</p>
        </Section>
      </AdminBody>
    </>
  );
}
