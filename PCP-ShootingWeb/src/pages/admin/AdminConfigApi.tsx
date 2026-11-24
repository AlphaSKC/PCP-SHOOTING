import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, DataTable, EmptyState, FormAlert, SelectField, TextareaField, TextField, type Column } from '@/components/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import {
  DAY_NAMES,
  WEEK_ORDER,
  blockRange,
  cancelBlock,
  checkDay,
  createBlock,
  getConfig,
  getLanes,
  listStaff,
  listUpcomingBlocks,
  saveConfig,
  saveLane,
  searchCustomers,
  setAccess,
  todayInClub,
  turnsOf,
  type ApiRole,
  type Block,
  type DaySchedule,
  type Lane,
  type OperatingConfig,
  type StaffProfile,
} from '@/features/admin/operacionApi';
import { resetDemo, update, useAdmin } from '@/features/admin/store';
import { AdminBody, DemoNote, Section } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import shared from './AdminShared.module.css';
import styles from './AdminConfigPage.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError ? (e.status === 409 ? 'Alguien más cambió estos datos. Recargamos la información: revisa y vuelve a guardar.' : e.message) : fallback;
const num = (v: string) => Number(v.replace(/\D/g, '') || 0);
const ROLE_LABEL: Record<ApiRole, string> = { Administrador: 'Administrador', Recepcion: 'Recepción', Cliente: 'Cliente' };

/* ---------------- Horario y reglas ---------------- */

function ScheduleSection() {
  const [saved, setSaved] = useState<OperatingConfig | null>(null);
  const [c, setC] = useState<OperatingConfig | null>(null);
  const [loadError, setLoadError] = useState('');
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      const cfg = await getConfig();
      setSaved(cfg);
      setC(cfg);
      setLoadError('');
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar la configuración.'));
    }
  }, []);
  useEffect(() => void load(), [load]);

  if (loadError) return <EmptyState title="No pudimos cargar el horario" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;
  if (!c || !saved) return <p className={shared.muted}>Cargando horario…</p>;

  const interval = c.duracionActividadMinutos + c.margenMinutos;
  const setDay = (dia: number, patch: Partial<DaySchedule>) =>
    setC({ ...c, horarios: c.horarios.map((h) => (h.diaSemana === dia ? { ...h, ...patch } : h)) });
  const problems = c.horarios
    .map((h) => ({ dia: h.diaSemana, text: checkDay(h, interval, c.duracionActividadMinutos, c.margenMinutos) }))
    .filter((p) => p.text);
  const dirty = JSON.stringify(c) !== JSON.stringify(saved);

  async function onSave() {
    if (!c) return;
    if (c.anticipacionMinimaMinutos >= c.anticipacionMaximaDias * 1440)
      return setMsg({ tone: 'error', text: 'La anticipación mínima debe ser menor que la máxima.' });
    if (problems.length) return setMsg({ tone: 'error', text: `${DAY_NAMES[problems[0]!.dia]}: ${problems[0]!.text}` });
    setSaving(true);
    setMsg(null);
    try {
      const fresh = await saveConfig(c);
      setSaved(fresh);
      setC(fresh);
      setMsg({ tone: 'success', text: 'Horario guardado. La disponibilidad de reservas ya usa estos valores.' });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) await load();
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el horario.') });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Section title="Horario y turnos">
      <p className={shared.muted}>
        Hora del club ({c.zonaHoraria}). Turnos cada <strong>{interval} min</strong> ({c.duracionActividadMinutos} de actividad + {c.margenMinutos} de margen).
      </p>
      <ul className={styles.days}>
        {WEEK_ORDER.map((dia) => {
          const h = c.horarios.find((x) => x.diaSemana === dia);
          if (!h) return null;
          const name = DAY_NAMES[dia]!;
          const problem = problems.find((p) => p.dia === dia)?.text;
          return (
            <li key={dia} className={styles.dayRow}>
              <strong>{name}</strong>
              {h.cerrado ? (
                <span className={cx(shared.muted, styles.dayClosed)}>Cerrado</span>
              ) : (
                <>
                  <span className={styles.range}>
                    <span className={styles.rangeLabel}>Abre – cierra</span>
                    <span className={styles.times}>
                      <TextField label={`${name} abre`} hideLabel type="time" value={h.apertura} onChange={(e) => setDay(dia, { apertura: e.target.value })} />
                      <span>–</span>
                      <TextField label={`${name} cierra`} hideLabel type="time" value={h.cierre} onChange={(e) => setDay(dia, { cierre: e.target.value })} />
                    </span>
                  </span>
                  <span className={styles.range}>
                    <span className={styles.rangeLabel}>Turnos: primero – último ({turnsOf(h, interval)})</span>
                    <span className={styles.times}>
                      <TextField label={`${name} primer turno`} hideLabel type="time" step={60} value={h.primerTurno} onChange={(e) => setDay(dia, { primerTurno: e.target.value })} />
                      <span>–</span>
                      <TextField label={`${name} último turno`} hideLabel type="time" step={60} value={h.ultimoTurno} onChange={(e) => setDay(dia, { ultimoTurno: e.target.value })} />
                    </span>
                  </span>
                </>
              )}
              <button type="button" className={shared.linkBtn} onClick={() => setDay(dia, { cerrado: !h.cerrado })}>
                {h.cerrado ? 'Abrir' : 'Cerrar'}
              </button>
              {problem && <span className={styles.dayProblem}>{problem}</span>}
            </li>
          );
        })}
      </ul>

      <div className={shared.grid2}>
        <TextField label="Actividad (min)" inputMode="numeric" value={c.duracionActividadMinutos} onChange={(e) => setC({ ...c, duracionActividadMinutos: num(e.target.value) })} />
        <TextField label="Margen entre turnos (min)" inputMode="numeric" value={c.margenMinutos} onChange={(e) => setC({ ...c, margenMinutos: num(e.target.value) })} />
        <TextField
          label="Tiempo para pagar (min)"
          hint="Cuánto dura apartada la línea mientras el cliente paga."
          inputMode="numeric"
          value={c.apartadoPagoMinutos}
          onChange={(e) => setC({ ...c, apartadoPagoMinutos: num(e.target.value) })}
        />
        <TextField
          label="Reservar con al menos (min)"
          hint="Anticipación mínima en línea."
          inputMode="numeric"
          value={c.anticipacionMinimaMinutos}
          onChange={(e) => setC({ ...c, anticipacionMinimaMinutos: num(e.target.value) })}
        />
        <TextField label="Reservar con hasta (días)" inputMode="numeric" value={c.anticipacionMaximaDias} onChange={(e) => setC({ ...c, anticipacionMaximaDias: num(e.target.value) })} />
        <TextField label="Llegada anticipada (min)" inputMode="numeric" value={c.llegadaAnticipadaMinutos} onChange={(e) => setC({ ...c, llegadaAnticipadaMinutos: num(e.target.value) })} />
        <TextField label="Tolerancia de llegada (min)" inputMode="numeric" value={c.toleranciaLlegadaMinutos} onChange={(e) => setC({ ...c, toleranciaLlegadaMinutos: num(e.target.value) })} />
      </div>

      {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
      <div className={shared.actions}>
        <Button size="lg" loading={saving} disabled={!dirty} onClick={() => void onSave()}>
          Guardar horario
        </Button>
        {dirty && (
          <Button size="lg" variant="secondary" disabled={saving} onClick={() => { setC(saved); setMsg(null); }}>
            Descartar
          </Button>
        )}
      </div>
    </Section>
  );
}

/* ---------------- Líneas ---------------- */

function LanesSection({ lanes, reload }: { lanes: Lane[]; reload: () => Promise<void> }) {
  const [selected, setSelected] = useState<Lane | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);

  async function save(l: Lane, ok: string) {
    setSaving(true);
    setMsg(null);
    try {
      const fresh = await saveLane(l);
      await reload();
      setSelected(fresh);
      setMsg({ tone: 'success', text: ok });
    } catch (e) {
      await reload();
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar la línea.') });
    } finally {
      setSaving(false);
    }
  }

  const active = lanes.filter((l) => l.activa).length;
  return (
    <Section title="Líneas de tiro">
      <p className={shared.muted}>
        {active} de {lanes.length} activas · una persona por línea. Toca una línea para editarla.
      </p>
      <div className={styles.lines} role="group" aria-label="Líneas de tiro">
        {lanes.map((l) => (
          <button
            key={l.id}
            type="button"
            aria-pressed={selected?.id === l.id}
            title={`${l.nombre} · ${l.activa ? 'activa' : 'inactiva'}`}
            className={cx(styles.line, l.activa && styles.lineOn, selected?.id === l.id && styles.lineSelected)}
            onClick={() => { setSelected(l); setMsg(null); }}
          >
            {l.numero}
          </button>
        ))}
      </div>
      {selected && (
        <div className={shared.grid2}>
          <TextField label={`Nombre de la línea ${selected.numero}`} maxLength={80} value={selected.nombre} onChange={(e) => setSelected({ ...selected, nombre: e.target.value })} />
          <SelectField
            label="Estado"
            value={selected.activa ? 'si' : 'no'}
            onChange={(e) => setSelected({ ...selected, activa: e.target.value === 'si' })}
            options={[{ value: 'si', label: 'Activa (se puede reservar)' }, { value: 'no', label: 'Inactiva' }]}
          />
        </div>
      )}
      {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
      {selected && (
        <div>
          <Button
            loading={saving}
            disabled={!selected.nombre.trim()}
            onClick={() => void save(selected, `Línea ${selected.numero} guardada.`)}
          >
            Guardar línea {selected.numero}
          </Button>
        </div>
      )}
    </Section>
  );
}

/* ---------------- Bloqueos ---------------- */

function BlocksSection({ lanes }: { lanes: Lane[] }) {
  const [blocks, setBlocks] = useState<Block[] | null>(null);
  const [loadError, setLoadError] = useState('');
  const [form, setForm] = useState({ lineaId: '', date: todayInClub(), from: '11:00', to: '12:00', allDay: false, motivo: '' });
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      setBlocks(await listUpcomingBlocks());
      setLoadError('');
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los bloqueos.'));
    }
  }, []);
  useEffect(() => void load(), [load]);

  const laneName = (id: string | null) => (id ? `Línea ${lanes.find((l) => l.id === id)?.numero ?? '?'}` : 'Todo el club');

  async function onCreate() {
    if (!form.motivo.trim()) return setMsg({ tone: 'error', text: 'Escribe el motivo del bloqueo.' });
    const from = form.allDay ? '00:00' : form.from;
    const to = form.allDay ? '24:00' : form.to;
    if (!form.allDay && from >= to) return setMsg({ tone: 'error', text: 'La hora final debe ser después de la inicial.' });
    setSaving(true);
    setMsg(null);
    try {
      await createBlock({ lineaId: form.lineaId || null, date: form.date, from, to, motivo: form.motivo });
      await load();
      setForm({ ...form, motivo: '' });
      setMsg({ tone: 'success', text: 'Bloqueo creado. Esas líneas ya no se pueden reservar en ese horario.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos crear el bloqueo.') });
    } finally {
      setSaving(false);
    }
  }

  async function onCancel(b: Block) {
    if (!window.confirm(`¿Quitar el bloqueo "${b.motivo}"?`)) return;
    setMsg(null);
    try {
      await cancelBlock(b);
      await load();
      setMsg({ tone: 'success', text: 'Bloqueo quitado.' });
    } catch (e) {
      await load();
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos quitar el bloqueo.') });
    }
  }

  const cols: Column<Block>[] = [
    { key: 'when', header: 'Cuándo', primary: true, render: (b) => <strong>{blockRange(b)}</strong> },
    { key: 'lane', header: 'Línea', render: (b) => laneName(b.lineaId) },
    { key: 'why', header: 'Motivo', render: (b) => b.motivo },
    {
      key: 'x',
      header: 'Acción',
      render: (b) => (
        <button type="button" className={shared.linkBtn} onClick={() => void onCancel(b)}>
          Quitar
        </button>
      ),
    },
  ];

  return (
    <Section title="Bloqueos de líneas">
      <p className={shared.muted}>Para mantenimiento, eventos o cierres especiales. Mientras dure, nadie puede reservar esas líneas.</p>
      <div className={shared.grid2}>
        <SelectField
          label="Línea"
          value={form.lineaId}
          onChange={(e) => setForm({ ...form, lineaId: e.target.value })}
          options={[{ value: '', label: 'Todo el club' }, ...lanes.map((l) => ({ value: l.id, label: `Línea ${l.numero}` }))]}
        />
        <TextField label="Fecha" type="date" min={todayInClub()} value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} />
        {!form.allDay && (
          <>
            <TextField label="Desde" type="time" value={form.from} onChange={(e) => setForm({ ...form, from: e.target.value })} />
            <TextField label="Hasta" type="time" value={form.to} onChange={(e) => setForm({ ...form, to: e.target.value })} />
          </>
        )}
      </div>
      <label className={shared.radio}>
        <input type="checkbox" checked={form.allDay} onChange={(e) => setForm({ ...form, allDay: e.target.checked })} />
        <span>Todo el día</span>
      </label>
      <TextField label="Motivo" maxLength={300} placeholder="Mantenimiento, evento privado…" value={form.motivo} onChange={(e) => setForm({ ...form, motivo: e.target.value })} />
      {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
      <div>
        <Button loading={saving} onClick={() => void onCreate()}>
          Bloquear
        </Button>
      </div>
      {loadError ? (
        <FormAlert>{loadError}</FormAlert>
      ) : (
        <DataTable
          caption="Próximos bloqueos"
          columns={cols}
          rows={blocks ?? []}
          rowKey={(b) => b.id}
          empty={<p className={shared.muted}>{blocks ? 'No hay bloqueos próximos.' : 'Cargando bloqueos…'}</p>}
        />
      )}
    </Section>
  );
}

/* ---------------- Staff ---------------- */

function StaffSection() {
  const { user } = useAuth();
  const [staff, setStaff] = useState<StaffProfile[] | null>(null);
  const [loadError, setLoadError] = useState('');
  const [q, setQ] = useState('');
  const [results, setResults] = useState<StaffProfile[] | null>(null);
  const [role, setRole] = useState<ApiRole>('Recepcion');
  const [msg, setMsg] = useState<Msg>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [searching, setSearching] = useState(false);

  const load = useCallback(async () => {
    try {
      setStaff(await listStaff());
      setLoadError('');
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar el staff.'));
    }
  }, []);
  useEffect(() => void load(), [load]);

  async function change(p: StaffProfile, rol: ApiRole, ok: string) {
    setBusyId(p.id);
    setMsg(null);
    try {
      await setAccess(p.id, rol);
      await load();
      setResults((r) => r?.filter((x) => x.id !== p.id) ?? null);
      setMsg({ tone: 'success', text: ok });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar el acceso.') });
    } finally {
      setBusyId(null);
    }
  }

  async function onSearch() {
    if (q.trim().length < 2) return setMsg({ tone: 'error', text: 'Escribe al menos 2 letras del nombre o teléfono.' });
    setSearching(true);
    setMsg(null);
    try {
      setResults(await searchCustomers(q));
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos buscar.') });
    } finally {
      setSearching(false);
    }
  }

  const cols: Column<StaffProfile>[] = [
    { key: 'name', header: 'Nombre', primary: true, render: (p) => <span className={shared.stack}><strong>{p.nombre}</strong><span>{p.telefono ?? 'Sin teléfono'}</span></span> },
    { key: 'role', header: 'Rol', render: (p) => ROLE_LABEL[p.rol] },
    {
      key: 'x',
      header: 'Acción',
      render: (p) =>
        p.id === user?.id ? (
          <span className={shared.muted}>Tú</span>
        ) : (
          <span className={shared.stack}>
            <button
              type="button"
              className={shared.linkBtn}
              disabled={busyId === p.id}
              onClick={() => void change(p, p.rol === 'Administrador' ? 'Recepcion' : 'Administrador', `${p.nombre} ahora es ${p.rol === 'Administrador' ? 'Recepción' : 'Administrador'}.`)}
            >
              Hacer {p.rol === 'Administrador' ? 'Recepción' : 'Administrador'}
            </button>
            <button
              type="button"
              className={shared.linkBtn}
              disabled={busyId === p.id}
              onClick={() => {
                if (window.confirm(`¿Quitar el acceso al panel a ${p.nombre}? Su cuenta queda como cliente.`))
                  void change(p, 'Cliente', `${p.nombre} ya no tiene acceso al panel.`);
              }}
            >
              Quitar acceso
            </button>
          </span>
        ),
    },
  ];

  return (
    <Section title="Usuarios del staff">
      {loadError ? (
        <FormAlert>{loadError}</FormAlert>
      ) : (
        <DataTable caption="Usuarios del staff" columns={cols} rows={staff ?? []} rowKey={(p) => p.id} empty={<p className={shared.muted}>Cargando…</p>} />
      )}
      <p className={shared.muted}>
        <strong>Administrador:</strong> ve y edita todo. <strong>Recepción:</strong> pedidos, alimentos, bloqueos y (próximamente) reservas y llegadas.
      </p>
      <p className={shared.muted}>
        Para dar acceso, la persona primero <strong>crea su cuenta</strong> en el sitio y confirma su correo. Después búscala aquí.
      </p>
      <div className={shared.grid2}>
        <TextField label="Buscar cliente" placeholder="Nombre o teléfono" value={q} onChange={(e) => setQ(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && void onSearch()} />
        <SelectField
          label="Darle el rol"
          value={role}
          onChange={(e) => setRole(e.target.value as ApiRole)}
          options={[{ value: 'Recepcion', label: 'Recepción' }, { value: 'Administrador', label: 'Administrador' }]}
        />
      </div>
      <div>
        <Button variant="secondary" loading={searching} onClick={() => void onSearch()}>
          Buscar
        </Button>
      </div>
      {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
      {results && (
        results.length === 0 ? (
          <p className={shared.muted}>No encontramos clientes con ese nombre o teléfono.</p>
        ) : (
          <ul className={shared.list}>
            {results.map((p) => (
              <li key={p.id} className={shared.row}>
                <span className={shared.stack}>
                  <strong>{p.nombre}</strong>
                  <span>{p.telefono ?? 'Sin teléfono'}</span>
                </span>
                <Button size="md" loading={busyId === p.id} onClick={() => void change(p, role, `${p.nombre} ahora es ${ROLE_LABEL[role]}.`)}>
                  Dar acceso
                </Button>
              </li>
            ))}
          </ul>
        )
      )}
    </Section>
  );
}

/* ---------------- Página ---------------- */

/** Configuración conectada a la API (horario, líneas, bloqueos y staff). Avisos/plantillas siguen en demo. */
export default function AdminConfigApi() {
  const settings = useAdmin((d) => d.settings);
  const [s, setS] = useState(settings);
  const [lanes, setLanes] = useState<Lane[] | null>(null);
  const [lanesError, setLanesError] = useState('');
  const [msg, setMsg] = useState<string | null>(null);

  const loadLanes = useCallback(async () => {
    try {
      setLanes(await getLanes());
      setLanesError('');
    } catch (e) {
      setLanesError(errorText(e, 'No pudimos cargar las líneas.'));
    }
  }, []);
  useEffect(() => void loadLanes(), [loadLanes]);

  return (
    <>
      <title>Configuración · Panel PCP</title>
      <AdminPageHeader title="Configuración" subtitle="Horarios, líneas, bloqueos, staff y avisos" actions={<ButtonLink to="/" variant="secondary">Ver sitio</ButtonLink>} />
      <AdminBody>
        <div className={styles.grid}>
          <div className={styles.col}>
            <ScheduleSection />
            {lanesError ? (
              <EmptyState title="No pudimos cargar las líneas" description={lanesError} action={<Button onClick={() => void loadLanes()}>Reintentar</Button>} />
            ) : lanes ? (
              <>
                <LanesSection lanes={lanes} reload={loadLanes} />
                <BlocksSection lanes={lanes} />
              </>
            ) : (
              <p className={shared.muted}>Cargando líneas…</p>
            )}
          </div>

          <div className={styles.col}>
            <StaffSection />

            <Section title="Actividades y precios">
              <p className={shared.muted}>
                Los paquetes se venden por actividad o tirada (sin duración). Precios, condiciones y consumibles se administran en{' '}
                <Link to="/admin/paquetes">Paquetes</Link>.
              </p>
            </Section>

            <Section title="Pagos y avisos">
              <div className={styles.mp}>
                <strong>Mercado Pago · pendiente de conectar</strong>
                <span>Lo integra el backend (R05): credenciales y notificaciones de pago.</span>
              </div>
              <DemoNote>Las plantillas y el correo de avisos aún no existen en la API: se guardan solo en este navegador.</DemoNote>
              {msg && <FormAlert tone="success">{msg}</FormAlert>}
              <TextField label="Correo que envía los avisos" type="email" value={s.notifyEmail} onChange={(e) => setS({ ...s, notifyEmail: e.target.value })} />
              <TextareaField label="Plantilla de WhatsApp · pedido listo" rows={3} hint="Usa [nombre] y [número]." value={s.waOrderReady} onChange={(e) => setS({ ...s, waOrderReady: e.target.value })} />
              <TextareaField label="Plantilla de WhatsApp · recordatorio de reserva" rows={3} hint="Usa [nombre], [hora] y [línea]." value={s.waReminder} onChange={(e) => setS({ ...s, waReminder: e.target.value })} />
              <div>
                <Button size="lg" onClick={() => { update((d) => ({ ...d, settings: { ...d.settings, notifyEmail: s.notifyEmail, waOrderReady: s.waOrderReady, waReminder: s.waReminder } })); setMsg('Plantillas guardadas en este navegador.'); }}>
                  Guardar
                </Button>
              </div>
            </Section>

            <Section title="Datos de demostración">
              <p className={shared.muted}>Las pantallas que aún no usan la API (reservas, inventario, eventos, etc.) usan datos de ejemplo de este navegador.</p>
              <div>
                <Button variant="secondary" onClick={() => { if (window.confirm('¿Restaurar los datos de demostración?')) { resetDemo(); window.location.reload(); } }}>
                  Restaurar datos de demostración
                </Button>
              </div>
            </Section>
          </div>
        </div>
      </AdminBody>
    </>
  );
}
