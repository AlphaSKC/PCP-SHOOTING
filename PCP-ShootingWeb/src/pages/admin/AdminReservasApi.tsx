import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import {
  Button,
  ButtonAnchor,
  Chip,
  ChipGroup,
  DataTable,
  EmptyState,
  FormAlert,
  KpiCard,
  KpiGrid,
  LineMap,
  SelectField,
  StatusBadge,
  TextField,
  type Column,
  type ShootingLine,
} from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { money, when } from '@/features/admin/format';
import { DAY_NAMES, getConfig, todayInClub, type OperatingConfig } from '@/features/admin/operacionApi';
import {
  STATE_VIEW,
  createCounterReservation,
  getDetails,
  getReservationDetail,
  listReservationsByDate,
  type AdminReservation,
  type AdminReservationDetail,
  searchCounterClients,
  type CounterClient,
  type CounterClientPage,
} from '@/features/admin/reservasAdminApi';
import { revealEditor } from '@/features/admin/scroll';
import { waLink } from '@/features/admin/store';
import { AdminBody, Panel, SegmentedTabs, Split } from '@/features/admin/ui';
import {
  ADULT_AGE,
  ageOn,
  clubTime,
  getApiPackages,
  getAvailability,
  participantProblem,
  type ApiDay,
  type ApiPackage,
  type Participant,
} from '@/features/reservas/reservasApi';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import shared from './AdminShared.module.css';
import styles from './AdminReservasApi.module.css';

type StateFilter = 'todas' | 'PendientePago' | 'Pagada' | 'Vencida';
type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);
const minutes = (t: string) => {
  const [h = 0, m = 0] = t.split(':').map(Number);
  return h * 60 + m;
};
const hhmm = (n: number) => `${String(Math.floor(n / 60)).padStart(2, '0')}:${String(n % 60).padStart(2, '0')}`;

/** Turnos del día según la configuración operativa (para el mapa). */
function turnsFor(config: OperatingConfig | null, date: string): string[] {
  if (!config) return [];
  const dow = new Date(`${date}T12:00:00Z`).getUTCDay();
  const h = config.horarios.find((x) => x.diaSemana === dow);
  if (!h || h.cerrado) return [];
  const step = config.duracionActividadMinutos + config.margenMinutos;
  const out: string[] = [];
  for (let t = minutes(h.primerTurno); t <= minutes(h.ultimoTurno) && step > 0; t += step) out.push(hhmm(t));
  return out;
}

/* ---------------- Reserva de mostrador ---------------- */

function CounterBooking({ onDone, onCancel, defaultDate }: { onDone: (id: string) => void; onCancel: () => void; defaultDate: string }) {
  const { user } = useAuth();
  const isAdmin = user?.role === 'admin';
  const [packages, setPackages] = useState<ApiPackage[]>([]);
  const [q, setQ] = useState('');
  const [found, setFound] = useState<CounterClientPage | null>(null);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState('');
  /** F18 aún no publicado en el servidor y el usuario es Recepción. */
  const [searchMissing, setSearchMissing] = useState(false);
  const searchSeq = useRef(0);
  const [client, setClient] = useState<CounterClient | null>(null);
  const [pkgId, setPkgId] = useState('');
  const [date, setDate] = useState(defaultDate);
  const [people, setPeople] = useState(1);
  const [day, setDay] = useState<ApiDay | null>(null);
  const [time, setTime] = useState('');
  const [lanes, setLanes] = useState<{ id: string; numero: number }[]>([]);
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [op, setOp] = useState<string | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void getApiPackages().then((list) => {
      setPackages(list);
      if (list[0]) setPkgId((p) => p || list[0]!.id);
    }, () => setPackages([]));
  }, []);

  // Turnos del día elegido (disponibilidad pública; ver nota F19 sobre la anticipación).
  useEffect(() => {
    setDay(null);
    setTime('');
    setLanes([]);
    if (!pkgId || !date) return;
    let alive = true;
    void getAvailability(pkgId, date, people).then(
      (d) => alive && setDay(d),
      () => alive && setDay({ fecha: date, diaAbierto: false, horarios: [] }),
    );
    return () => {
      alive = false;
    };
  }, [pkgId, date, people]);

  const now = Date.now();
  const utc = (s: string) => new Date(/Z$|[+-]\d\d:\d\d$/.test(s) ? s : `${s}Z`).getTime();
  const slots = (day?.horarios ?? []).filter((h) => utc(h.inicioUtc) > now);
  const slot = slots.find((h) => h.hora.slice(0, 5) === time);
  /** En mostrador se puede reservar dentro de la anticipación web: esas líneas se muestran «por confirmar» y la API valida. */
  const laneSelectable = (estado: string) => estado === 'Libre' || estado === 'FueraDeAnticipacion';
  const lineList: ShootingLine[] = (slot?.lineas ?? []).map((l) => ({
    number: l.numero,
    status: laneSelectable(l.estado) ? 'available' : 'occupied',
    label: l.estado === 'Inactiva' ? 'Inactiva' : l.estado === 'Bloqueada' ? 'Bloqueada' : l.estado === 'Apartada' ? 'Ocupada' : undefined,
  }));
  const insideWindow = slot?.lineas.some((l) => l.estado === 'FueraDeAnticipacion') ?? false;

  function toggleLane(numero: number) {
    const lane = slot?.lineas.find((l) => l.numero === numero);
    if (!lane || !laneSelectable(lane.estado)) return;
    if (lanes.some((l) => l.id === lane.id)) {
      setLanes(lanes.filter((l) => l.id !== lane.id));
      return;
    }
    if (lanes.length >= people) return setMsg({ tone: 'error', text: `Ya elegiste ${people} ${people === 1 ? 'línea' : 'líneas'}.` });
    setMsg(null);
    setLanes([...lanes, { id: lane.id, numero: lane.numero }].sort((a, b) => a.numero - b.numero));
  }

  // Un participante por línea elegida (conserva lo escrito).
  useEffect(() => {
    setParticipants((prev) =>
      lanes.map((l, i) => prev.find((p) => p.laneId === l.id) ?? prev[i] ?? { laneId: l.id, name: i === 0 && client ? client.nombre : '', birth: '', tutorName: '', tutorBirth: '' }).map((p, i) => ({ ...p, laneId: lanes[i]!.id })),
    );
  }, [lanes, client]);

  /** Busca al escribir (espera breve entre teclas); `page > 1` agrega resultados («Ver más»). */
  const runSearch = useCallback(async (term: string, page: number) => {
    const seq = ++searchSeq.current;
    setSearching(true);
    setSearchError('');
    try {
      const r = await searchCounterClients(term, page, isAdmin);
      if (seq !== searchSeq.current) return;
      if (!r) {
        setSearchMissing(true);
        setFound(null);
        return;
      }
      setFound((prev) => (page > 1 && prev ? { ...r, items: [...prev.items, ...r.items.filter((c) => !prev.items.some((p) => p.id === c.id))] } : r));
    } catch (e) {
      if (seq === searchSeq.current) setSearchError(errorText(e, 'No pudimos buscar clientes.'));
    } finally {
      if (seq === searchSeq.current) setSearching(false);
    }
  }, [isAdmin]);

  useEffect(() => {
    const term = q.trim();
    if (client || term.length < 2) {
      searchSeq.current++;
      setFound(null);
      setSearching(false);
      setSearchError('');
      return;
    }
    const t = setTimeout(() => void runSearch(term, 1), 350);
    return () => clearTimeout(t);
  }, [q, client, runSearch]);

  async function onSubmit() {
    if (!client) return setMsg({ tone: 'error', text: 'Elige al cliente registrado.' });
    if (!pkgId || !time) return setMsg({ tone: 'error', text: 'Elige paquete, fecha y horario.' });
    if (lanes.length !== people) return setMsg({ tone: 'error', text: `Elige ${people} ${people === 1 ? 'línea' : 'líneas'}.` });
    for (const [i, p] of participants.entries()) {
      const problem = participantProblem(p, date, i);
      if (problem) return setMsg({ tone: 'error', text: problem });
    }
    setBusy(true);
    setMsg(null);
    const operationId = op ?? crypto.randomUUID();
    setOp(operationId);
    try {
      const r = await createCounterReservation({ operationId, clientId: client.id, packageId: pkgId, date, time, participants });
      onDone(r.id);
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        setOp(null);
        setLanes([]);
        void getAvailability(pkgId, date, people).then(setDay, () => undefined);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos crear la reserva.') });
    } finally {
      setBusy(false);
    }
  }

  const setP = (i: number, patch: Partial<Participant>) => setParticipants(participants.map((p, j) => (j === i ? { ...p, ...patch } : p)));

  return (
    <Panel title="Reserva de mostrador" id="editor" actions={<Button variant="text" onClick={onCancel}>Cerrar</Button>}>
      <p className={shared.muted}>Para un cliente registrado (si no tiene cuenta, que se registre en el sitio primero). Queda «Pendiente de pago» hasta cobrarse.</p>
      {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}

      <div className={shared.subsection}>
        <h3 className={shared.subTitle}>1. Cliente</h3>
        {client ? (
          <div className={shared.row}>
            <span className={shared.stack}>
              <strong>{client.nombre}</strong>
              <span>{client.telefono ?? 'Sin teléfono'}</span>
            </span>
            <Button variant="text" onClick={() => setClient(null)}>Cambiar</Button>
          </div>
        ) : !searchMissing ? (
          <>
            <TextField
              label="Buscar cliente"
              placeholder="Nombre o teléfono (mínimo 2 letras)"
              value={q}
              onChange={(e) => setQ(e.target.value)}
              hint="Solo clientes con cuenta activa. Si no tiene cuenta, pídele que se registre en el sitio."
            />
            {searching && !found && <p className={shared.muted} role="status">Buscando…</p>}
            {searchError && <FormAlert inline>{searchError}</FormAlert>}
            {found && (found.items.length === 0 ? (
              <p className={shared.muted}>Sin resultados para «{q.trim()}». Si no tiene cuenta, pídele que se registre en el sitio.</p>
            ) : (
              <>
                <p className={shared.muted}>{found.total === 1 ? '1 cliente encontrado' : `${found.total} clientes encontrados`}</p>
                <ul className={shared.list}>
                  {found.items.map((c) => (
                    <li key={c.id} className={shared.row}>
                      <span className={shared.stack}>
                        <strong>{c.nombre}</strong>
                        <span>{c.telefono ?? 'Sin teléfono'}</span>
                      </span>
                      <Button onClick={() => { setClient(c); setFound(null); }}>Elegir</Button>
                    </li>
                  ))}
                </ul>
                {found.items.length < found.total && (
                  <Button variant="secondary" loading={searching} onClick={() => void runSearch(q.trim(), found.page + 1)}>
                    Ver más clientes
                  </Button>
                )}
              </>
            ))}
          </>
        ) : (
          <FormAlert inline>Pendiente de publicar en el servidor: Recepción todavía no puede buscar clientes (F18). Por ahora, la reserva de mostrador la hace un Administrador.</FormAlert>
        )}
      </div>

      <div className={shared.subsection}>
        <h3 className={shared.subTitle}>2. Actividad, fecha y horario</h3>
        <SelectField
          label="Paquete"
          value={pkgId}
          onChange={(e) => setPkgId(e.target.value)}
          options={packages.map((p) => ({ value: p.id, label: `${p.nombre}${p.precioPorPersona != null ? ` · ${money(p.precioPorPersona)} por persona` : ''}` }))}
          placeholder={packages.length ? undefined : 'No hay paquetes visibles'}
        />
        <div className={shared.grid2}>
          <TextField label="Fecha" type="date" min={todayInClub()} value={date} onChange={(e) => setDate(e.target.value)} />
          <SelectField
            label="Personas"
            value={String(people)}
            onChange={(e) => { setPeople(Number(e.target.value)); setLanes([]); }}
            options={Array.from({ length: 15 }, (_, i) => ({ value: String(i + 1), label: String(i + 1) }))}
          />
        </div>
        {day && slots.length === 0 && (
          <p className={shared.muted}>
            {day.diaAbierto ? 'No hay turnos disponibles ese día (o la fecha está fuera de la ventana que publica la API).' : 'El club está cerrado ese día.'}
          </p>
        )}
        {slots.length > 0 && (
          <SelectField
            label="Horario"
            value={time}
            placeholder="Elige un horario…"
            onChange={(e) => { setTime(e.target.value); setLanes([]); }}
            options={slots.map((h) => ({ value: h.hora.slice(0, 5), label: h.hora.slice(0, 5) }))}
          />
        )}
      </div>

      {slot && (
        <div className={shared.subsection}>
          <h3 className={shared.subTitle}>3. Líneas ({lanes.length}/{people})</h3>
          {insideWindow && (
            <p className={shared.muted}>Turno próximo: la API no publica qué líneas están libres para turnos a menos de 1 hora (F19). Elige las líneas; si alguna está ocupada, la API lo rechaza.</p>
          )}
          <LineMap title="Elige las líneas" lines={lineList} selected={lanes.map((l) => l.numero)} onSelect={toggleLane} />
        </div>
      )}

      {lanes.length > 0 && (
        <div className={shared.subsection}>
          <h3 className={shared.subTitle}>4. Participantes</h3>
          <p className={shared.muted}>Edad mínima 9 años; menores de {ADULT_AGE} con tutor adulto.</p>
          {participants.map((p, i) => {
            const minor = p.birth ? ageOn(p.birth, date) < ADULT_AGE : false;
            return (
              <div key={p.laneId} className={shared.stack}>
                <strong>Línea {lanes[i]?.numero}</strong>
                <div className={shared.grid2}>
                  <TextField label="Nombre completo" maxLength={150} value={p.name} onChange={(e) => setP(i, { name: e.target.value })} />
                  <TextField label="Nacimiento" type="date" max={date} value={p.birth} onChange={(e) => setP(i, { birth: e.target.value })} />
                  {minor && (
                    <>
                      <TextField label="Tutor" maxLength={150} value={p.tutorName} onChange={(e) => setP(i, { tutorName: e.target.value })} />
                      <TextField label="Nacimiento del tutor" type="date" max={date} value={p.tutorBirth} onChange={(e) => setP(i, { tutorBirth: e.target.value })} />
                    </>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      <Button size="lg" fullWidth loading={busy} disabled={!client || lanes.length !== people} onClick={() => void onSubmit()}>
        Crear reserva de mostrador
      </Button>
    </Panel>
  );
}

/* ---------------- Página ---------------- */

/** Reservas del día conectadas a la API (lista, mapa de líneas, detalle y mostrador). */
export default function AdminReservasApi() {
  // Enlace desde la ficha del cliente: ?fecha=AAAA-MM-DD&reserva=<id>
  const [params] = useSearchParams();
  const linkedDate = params.get('fecha');
  const linkedId = params.get('reserva');
  const [date, setDate] = useState(linkedDate && /^\d{4}-\d{2}-\d{2}$/.test(linkedDate) ? linkedDate : todayInClub());
  const [filter, setFilter] = useState<StateFilter>('todas');
  const [view, setView] = useState<'lista' | 'mapa'>('lista');
  const [list, setList] = useState<AdminReservation[]>([]);
  const [details, setDetails] = useState<AdminReservationDetail[] | null>(null);
  const [config, setConfig] = useState<OperatingConfig | null>(null);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [selected, setSelected] = useState<AdminReservationDetail | null>(null);
  const [counter, setCounter] = useState(false);
  const [flash, setFlash] = useState<string | null>(null);

  useEffect(() => {
    void getConfig().then(setConfig, () => setConfig(null));
  }, []);

  const load = useCallback(async () => {
    setStatus('loading');
    setDetails(null);
    try {
      const l = await listReservationsByDate(date);
      setList(l);
      setStatus('ready');
      return l;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar las reservas.'));
      setStatus('error');
      return null;
    }
  }, [date]);

  useEffect(() => {
    void load();
  }, [load]);

  // Abre una sola vez la reserva enlazada.
  const [linkedOpened, setLinkedOpened] = useState(false);
  useEffect(() => {
    if (!linkedId || linkedOpened || status !== 'ready') return;
    setLinkedOpened(true);
    void open(linkedId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [linkedId, linkedOpened, status]);

  // El mapa necesita las líneas de cada reserva (detalle).
  useEffect(() => {
    if (view !== 'mapa' || status !== 'ready' || details) return;
    void getDetails(list.filter((r) => r.state !== 'Vencida').map((r) => r.id)).then(setDetails);
  }, [view, status, list, details]);

  const visible = useMemo(() => list.filter((r) => filter === 'todas' || r.state === filter), [list, filter]);
  const active = list.filter((r) => r.state !== 'Vencida');

  async function open(id: string) {
    setCounter(false);
    setFlash(null);
    try {
      setSelected(await getReservationDetail(id));
      revealEditor();
    } catch (e) {
      setFlash(errorText(e, 'No pudimos abrir la reserva.'));
    }
  }

  const cols: Column<AdminReservation>[] = [
    { key: 'time', header: 'Hora', strong: true, primary: true, render: (r) => r.time },
    {
      key: 'client',
      header: 'Cliente',
      render: (r) => (
        <span className={shared.stack}>
          <strong>{r.clientName}</strong>
          <span>{r.packageName}</span>
        </span>
      ),
    },
    { key: 'people', header: 'Personas', render: (r) => r.people },
    { key: 'total', header: 'Total', hideOnMobile: true, render: (r) => money(r.total) },
    { key: 'state', header: 'Estado', render: (r) => <StatusBadge status={STATE_VIEW[r.state].status} /> },
  ];

  const turns = turnsFor(config, date);
  const cellOf = (time: string, lane: number) =>
    (details ?? []).find((d) => d.time === time && d.participants.some((p) => p.lane === lane));
  const dow = DAY_NAMES[new Date(`${date}T12:00:00Z`).getUTCDay()];

  return (
    <>
      <title>Reservas · Panel PCP</title>
      <AdminPageHeader
        title="Reservas"
        subtitle={`${dow} · reservas del día y mostrador`}
        actions={
          <Button iconStart={<IconPlus size={16} />} onClick={() => { setSelected(null); setCounter(true); revealEditor(); }}>
            Reserva de mostrador
          </Button>
        }
      />
      <AdminBody>
        <div className={shared.toolbar}>
          <ChipGroup label="Estado">
            {(['todas', 'PendientePago', 'Pagada', 'Vencida'] as StateFilter[]).map((s) => (
              <Chip key={s} selected={filter === s} onClick={() => setFilter(s)}>
                {s === 'todas' ? `Todas (${list.length})` : `${STATE_VIEW[s].label} (${list.filter((r) => r.state === s).length})`}
              </Chip>
            ))}
          </ChipGroup>
          <div className={styles.viewBar}>
            <TextField label="Fecha" type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} />
            <SegmentedTabs label="Vista" value={view} onChange={setView} options={[{ value: 'lista', label: 'Lista' }, { value: 'mapa', label: 'Mapa de líneas' }]} />
          </div>
        </div>

        <KpiGrid>
          <KpiCard label="Reservas" value={String(active.length)} hint="Sin contar vencidas" highlight />
          <KpiCard label="Personas" value={String(active.reduce((s, r) => s + r.people, 0))} hint="Líneas ocupadas en el día" />
          <KpiCard label="Pagadas" value={String(list.filter((r) => r.state === 'Pagada').length)} hint={money(list.filter((r) => r.state === 'Pagada').reduce((s, r) => s + r.total, 0))} />
          <KpiCard label="Por cobrar" value={String(list.filter((r) => r.state === 'PendientePago').length)} hint="Pendientes de pago" />
        </KpiGrid>

        {flash && <FormAlert tone="success">{flash}</FormAlert>}

        {status === 'error' ? (
          <EmptyState title="No pudimos cargar las reservas" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <Split
            asideWidth={440}
            main={
              view === 'lista' ? (
                <div className={shared.tableCard}>
                  <DataTable
                    caption="Reservas del día"
                    columns={cols}
                    rows={visible}
                    rowKey={(r) => r.id}
                    onRowClick={(r) => void open(r.id)}
                    selectedKey={selected?.id}
                    empty={<p className={shared.empty}>{status === 'loading' ? 'Cargando reservas…' : 'No hay reservas con ese filtro en esta fecha.'}</p>}
                  />
                </div>
              ) : turns.length === 0 ? (
                <EmptyState title="Sin turnos este día" description="El club está cerrado o la configuración no tiene turnos." />
              ) : (
                <div className={styles.mapWrap}>
                  {!details && <p className={shared.muted}>Cargando líneas…</p>}
                  <table className={styles.map}>
                    <caption className="visually-hidden">Mapa de líneas por horario</caption>
                    <thead>
                      <tr>
                        <th scope="col">Hora</th>
                        {Array.from({ length: 15 }, (_, i) => (
                          <th key={i} scope="col">{i + 1}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {turns.map((t) => (
                        <tr key={t}>
                          <th scope="row">{t}</th>
                          {Array.from({ length: 15 }, (_, i) => {
                            const r = cellOf(t, i + 1);
                            return (
                              <td key={i}>
                                {r ? (
                                  <button
                                    type="button"
                                    className={cx(styles.cell, r.state === 'Pagada' ? styles.paid : styles.pending)}
                                    title={`${r.clientName} · ${STATE_VIEW[r.state].label}`}
                                    onClick={() => void open(r.id)}
                                  >
                                    {r.clientName.split(' ')[0]?.slice(0, 6)}
                                  </button>
                                ) : (
                                  <span className={styles.free} aria-label="Libre" />
                                )}
                              </td>
                            );
                          })}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <p className={shared.muted}>
                    <span className={cx(styles.legend, styles.paid)} /> Pagada · <span className={cx(styles.legend, styles.pending)} /> Pendiente de pago. Los bloqueos se ven en Configuración.
                  </p>
                </div>
              )
            }
            aside={
              counter ? (
                <CounterBooking
                  defaultDate={date}
                  onCancel={() => setCounter(false)}
                  onDone={(id) => {
                    setCounter(false);
                    void load().then(() => open(id));
                    setFlash('Reserva de mostrador creada. Queda pendiente de pago hasta cobrarla.');
                  }}
                />
              ) : selected ? (
                <Panel title={`${selected.time} · ${selected.packageName}`} id="editor" actions={<StatusBadge status={STATE_VIEW[selected.state].status} />}>
                  <div>
                    <p className={shared.big}>{selected.clientName}</p>
                    <p className={shared.muted}>{selected.clientPhone ?? 'Sin teléfono'}</p>
                  </div>
                  <div className={shared.box}>
                    <span>
                      {selected.people} {selected.people === 1 ? 'persona' : 'personas'} · {money(selected.pricePerPerson)} c/u
                    </span>
                    <strong>{money(selected.total)}</strong>
                  </div>
                  <ul className={shared.list}>
                    {selected.participants.map((p) => (
                      <li key={p.laneId} className={shared.row}>
                        <span className={shared.stack}>
                          <strong>Línea {p.lane} · {p.name}</strong>
                          <span>
                            {ageOn(p.birth, selected.date)} años{p.tutorName ? ` · tutor: ${p.tutorName}` : ''}
                          </span>
                        </span>
                      </li>
                    ))}
                  </ul>
                  {selected.state === 'PendientePago' && (
                    <FormAlert inline>Pendiente de pago: vence a las {clubTime(selected.payDeadlineUtc)}. El cobro con Mercado Pago se conecta en el paso de pagos.</FormAlert>
                  )}
                  <p className={shared.muted}>Creada {when(selected.createdUtc)}. Registro de llegada con QR: pendiente (R06).</p>
                  {selected.clientPhone && (
                    <ButtonAnchor
                      href={waLink(selected.clientPhone, `Hola ${selected.clientName.split(' ')[0]}, te esperamos el ${dow?.toLowerCase()} a las ${selected.time} en PCP Shooting.`)}
                      target="_blank"
                      rel="noreferrer"
                      variant="dark"
                      fullWidth
                    >
                      Escribir por WhatsApp
                    </ButtonAnchor>
                  )}
                </Panel>
              ) : (
                <Panel title="Reserva" id="editor">
                  <p className={shared.muted}>Elige una reserva de la lista o del mapa, o crea una de mostrador.</p>
                </Panel>
              )
            }
          />
        )}
      </AdminBody>
    </>
  );
}
