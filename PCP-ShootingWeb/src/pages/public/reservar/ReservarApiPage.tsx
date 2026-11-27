import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { Button, ButtonLink, Checkbox, ChoiceCard, EmptyState, FormAlert, LineMap, PageHero, QuantityStepper, StepBar, TextField, type ShootingLine } from '@/components/ui';
import { IconFile } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { ReservationSummary } from '@/features/reservas/components/ReservationSummary';
import { DayPicker, TimePicker } from '@/features/reservas/components/SlotPickers';
import { WizardActions } from '@/features/reservas/components/WizardActions';
import { peopleLabel, shortDay } from '@/features/reservas/format';
import {
  ADULT_AGE,
  MAX_PEOPLE_API,
  ageOn,
  clubTime,
  createHold,
  createReservation,
  getApiPackages,
  getBookableDays,
  laneNumbersOf,
  markdownItems,
  msUntil,
  participantProblem,
  type ApiDay,
  type ApiHold,
  type ApiPackage,
  type ApiReservation,
  type ApiSlot,
  type LaneState,
  type Participant,
} from '@/features/reservas/reservasApi';
import { useAsync } from '@/features/reservas/useAsync';
import { ApiError } from '@/lib/api';
import { formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './ReservarPage.module.css';

const STEPS = ['Actividad', 'Fecha y líneas', 'Participantes', 'Pago'];
const KEY = 'pcp.reserva.api';

type Draft = {
  step: number;
  packageId: string | null;
  people: number;
  date: string | null;
  time: string | null;
  /** Líneas elegidas: id → número (la API solo devuelve ids en la reserva). */
  lanes: { id: string; numero: number }[];
  holdOp: string | null;
  hold: ApiHold | null;
  participants: Participant[];
  reservationOp: string | null;
  reservation: ApiReservation | null;
};

const EMPTY: Draft = { step: 0, packageId: null, people: 1, date: null, time: null, lanes: [], holdOp: null, hold: null, participants: [], reservationOp: null, reservation: null };

function loadDraft(): Draft {
  try {
    const raw = sessionStorage.getItem(KEY);
    return raw ? { ...EMPTY, ...(JSON.parse(raw) as Draft) } : EMPTY;
  } catch {
    return EMPTY;
  }
}

const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);
const priceOf = (p?: ApiPackage) => p?.precioPorPersona ?? null;

/** Segundos restantes hasta `iso` (se actualiza cada segundo). */
function useCountdown(iso: string | null) {
  const [ms, setMs] = useState(() => (iso ? msUntil(iso) : 0));
  useEffect(() => {
    if (!iso) return;
    setMs(msUntil(iso));
    const t = window.setInterval(() => setMs(msUntil(iso)), 1000);
    return () => window.clearInterval(t);
  }, [iso]);
  return iso ? Math.max(0, Math.floor(ms / 1000)) : 0;
}

/** Texto de cada línea no disponible según la API (el motivo de un bloqueo es privado: se muestra "Bloqueada"). */
const LANE_LABEL: Record<LaneState, string | undefined> = {
  Libre: undefined,
  Inactiva: 'Inactiva',
  Bloqueada: 'Bloqueada',
  Apartada: 'Ocupada',
  FueraDeAnticipacion: 'No disp.',
};

/**
 * Motivo de un turno no disponible, según el estado de sus líneas (API):
 * fuera de anticipación → "No disponible"; menos líneas libres que personas → "Solo N libres";
 * sin líneas por apartados → "Lleno"; por bloqueo → "Bloqueado".
 */
function slotReason(h: ApiSlot, people: number): string {
  const states = h.lineas.map((l) => l.estado);
  if (states.length > 0 && states.every((e) => e === 'FueraDeAnticipacion' || e === 'Inactiva')) return 'No disponible';
  if (h.lineasLibres > 0 && h.lineasLibres < people) return `Solo ${h.lineasLibres} ${h.lineasLibres === 1 ? 'libre' : 'libres'}`;
  if (states.includes('Apartada')) return 'Lleno';
  if (states.includes('Bloqueada')) return 'Bloqueado';
  return 'No disponible';
}

const mmss = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;

/** Reservación conectada a la API (paquetes, disponibilidad, apartado y reserva con participantes). */
export default function ReservarApiPage() {
  const navigate = useNavigate();
  const { user, status: authStatus } = useAuth();
  const [d, setD] = useState<Draft>(loadDraft);
  const packages = useAsync(getApiPackages, []);

  useEffect(() => {
    try {
      sessionStorage.setItem(KEY, JSON.stringify(d));
    } catch {
      /* sin almacenamiento */
    }
  }, [d]);

  const update = useCallback((c: Partial<Draft>) => setD((s) => ({ ...s, ...c })), []);
  const goTo = useCallback((step: number) => {
    setD((s) => ({ ...s, step }));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);
  const restart = useCallback(() => {
    setD((s) => ({ ...EMPTY, packageId: s.packageId, people: s.people }));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  const pkg = packages.data?.find((p) => p.id === d.packageId);
  const price = priceOf(pkg);
  const total = (price ?? 0) * d.people;
  const laneNumbers = d.lanes.map((l) => l.numero);

  const summary = [pkg?.nombre, peopleLabel(d.people, true), d.date && shortDay(d.date), d.time].filter(Boolean).join(' · ');
  const summaryData = { packageName: pkg?.nombre, people: d.people, date: d.date, time: d.time, lines: laneNumbers, total, totalText: price == null && pkg ? 'Pendiente' : undefined };

  let body;
  if (packages.loading && !packages.data) body = <LoadingScreen />;
  else if (packages.error)
    body = <EmptyState title="No pudimos cargar las actividades" description="Revisa tu conexión e intenta de nuevo." action={<Button onClick={() => window.location.reload()}>Reintentar</Button>} />;
  else if (!packages.data?.length)
    body = <EmptyState title="Aún no hay actividades disponibles" description="Pronto podrás reservar en línea. Mientras tanto, contáctanos por WhatsApp." action={<ButtonLink to="/contacto">Contactar</ButtonLink>} />;
  else if (d.step === 0) body = <StepActivity packages={packages.data} d={d} update={update} onNext={() => goTo(1)} summary={summary} total={total} />;
  else if (d.step === 1)
    body = (
      <StepSchedule
        d={d}
        update={update}
        summary={summary}
        total={total}
        loggedIn={Boolean(user)}
        authLoading={authStatus === 'loading'}
        onBack={() => goTo(0)}
        onLogin={() => navigate('/login?next=/reservar')}
        onHeld={(hold) => {
          const prev = d.participants;
          // F04: la API confirma número y nombre de cada línea apartada.
          const lanes = hold.lineasDetalle?.length
            ? hold.lineasDetalle.map((l) => ({ id: l.lineaId, numero: l.numeroLinea }))
            : d.lanes;
          const participants = lanes.map((l, i) => prev[i] ? { ...prev[i]!, laneId: l.id } : { laneId: l.id, name: i === 0 ? user?.name ?? '' : '', birth: '', tutorName: '', tutorBirth: '' });
          update({ hold, lanes, participants, reservationOp: null, reservation: null, step: 2 });
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }}
      />
    );
  else if (d.step === 2 && d.hold)
    body = (
      <StepParticipants
        d={d}
        update={update}
        summary={summary}
        total={total}
        onExpired={restart}
        onBack={() => goTo(1)}
        onReserved={(reservation) => {
          update({ reservation, step: 3 });
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }}
      />
    );
  else if (d.step === 3 && d.reservation)
    body = (
      <StepPayment
        reservation={d.reservation}
        laneNumbers={laneNumbersOf(d.reservation).length ? laneNumbersOf(d.reservation) : laneNumbers}
        onDone={() => {
          const id = d.reservation!.id;
          setD(EMPTY);
          navigate(`/reservar/confirmacion/${id}`, { replace: true });
        }}
      />
    );
  else body = <StepActivity packages={packages.data} d={{ ...d, step: 0 }} update={update} onNext={() => goTo(1)} summary={summary} total={total} />;

  return (
    <>
      <title>Reservar línea de tiro · PCP Shooting</title>
      <meta name="description" content="Reserva tu línea de tiro en PCP Shooting: elige actividad, fecha, horario y tus líneas de tiro." />
      <PageHero
        compact
        eyebrow="Reservas en línea"
        title="Reserva tu línea de tiro"
        lead="15 líneas de tiro · una persona por línea · Martes a domingo"
        hideLeadOnMobile
        aside={<StepBar steps={STEPS} current={Math.min(d.step, 3)} />}
      />
      <div className={`container ${styles.layout}`}>
        <div className={styles.main}>{body}</div>
        <aside className={styles.aside}>
          <ReservationSummary data={summaryData} showMemberNote={!user?.membership} />
        </aside>
      </div>
    </>
  );
}

/* ---------------- Paso 1: actividad y personas ---------------- */

function StepActivity({ packages, d, update, onNext, summary, total }: { packages: ApiPackage[]; d: Draft; update: (c: Partial<Draft>) => void; onNext: () => void; summary: string; total: number }) {
  const [error, setError] = useState<string | null>(null);
  const selected = packages.find((p) => p.id === d.packageId);

  const next = () => {
    if (!selected) return setError('Elige una actividad para continuar.');
    setError(null);
    onNext();
  };

  return (
    <section className={styles.step} aria-labelledby="paso-1">
      <div className={styles.stepHead}>
        <h2 id="paso-1" className={styles.stepTitle}>
          1. Elige tu actividad
        </h2>
        <p className={styles.stepLead}>Se paga por persona. Cada persona usa su propia línea de tiro.</p>
      </div>
      {error && <FormAlert>{error}</FormAlert>}
      <div role="radiogroup" aria-labelledby="paso-1" className={styles.packages}>
        {packages.map((p) => {
          const items = markdownItems(p.incluyeMarkdown);
          return (
            <ChoiceCard
              key={p.id}
              selected={p.id === d.packageId}
              onSelect={() => update({ packageId: p.id, date: null, time: null, lanes: [], hold: null, holdOp: null })}
              title={p.nombre}
              description={p.descripcion ?? undefined}
              meta={`${items.slice(0, 3).join(' · ')}${items.length ? ' · ' : ''}${p.precioPorPersona != null ? `${formatMXN(p.precioPorPersona).replace(' MXN', '')} por persona` : 'Precio pendiente'}`}
            />
          );
        })}
      </div>
      <div className={styles.options}>
        <div className={styles.people}>
          <span className={styles.legend}>Personas</span>
          <QuantityStepper label="personas" value={d.people} min={1} max={MAX_PEOPLE_API} onChange={(people) => update({ people, lanes: [], time: null, hold: null, holdOp: null })} />
        </div>
      </div>
      <WizardActions summary={summary} total={total} primary={<Button size="lg" onClick={next}>Continuar: fecha y líneas</Button>} />
    </section>
  );
}

/* ---------------- Paso 2: fecha, hora y líneas → apartado ---------------- */

function StepSchedule(props: {
  d: Draft;
  update: (c: Partial<Draft>) => void;
  summary: string;
  total: number;
  loggedIn: boolean;
  authLoading: boolean;
  onBack: () => void;
  onLogin: () => void;
  onHeld: (hold: ApiHold) => void;
}) {
  const { d, update, summary, total, loggedIn, authLoading, onBack, onLogin, onHeld } = props;
  const [error, setError] = useState<string | null>(null);
  const [holding, setHolding] = useState(false);
  const [refresh, setRefresh] = useState(0);
  const days = useAsync(d.packageId ? () => getBookableDays(d.packageId!, d.people) : null, [d.packageId, d.people, refresh]);

  const day: ApiDay | undefined = days.data?.find((x) => x.fecha === d.date);
  const slot = day?.horarios.find((h) => h.hora.slice(0, 5) === d.time);

  // Si el día u horario guardado ya no existe, se descarta.
  useEffect(() => {
    if (!days.data) return;
    if (d.date && !day) update({ date: null, time: null, lanes: [] });
    else if (day && d.time && !slot) update({ time: null, lanes: [] });
  }, [days.data, day, slot, d.date, d.time, update]);

  const pickerDays = useMemo(
    () =>
      (days.data ?? []).map((x) => ({
        date: x.fecha,
        slots: x.horarios.map((h) => ({ time: h.hora.slice(0, 5), full: !h.disponible, label: h.disponible ? undefined : slotReason(h, d.people) })),
      })),
    [days.data, d.people],
  );

  const lineList: ShootingLine[] = (slot?.lineas ?? []).map((l) => ({
    number: l.numero,
    status: l.disponible ? 'available' : 'occupied',
    label: LANE_LABEL[l.estado],
  }));
  const chosen = d.lanes.filter((l) => slot?.lineas.some((x) => x.id === l.id && x.disponible));

  function toggleLane(numero: number) {
    const lane = slot?.lineas.find((l) => l.numero === numero);
    if (!lane || !lane.disponible) return;
    if (chosen.some((l) => l.id === lane.id)) return update({ lanes: chosen.filter((l) => l.id !== lane.id) });
    if (chosen.length >= d.people) return setError(`Ya elegiste ${d.people} ${d.people === 1 ? 'línea' : 'líneas'}. Quita una para cambiarla.`);
    setError(null);
    update({ lanes: [...chosen, { id: lane.id, numero: lane.numero }] });
  }

  async function next() {
    if (!d.date) return setError('Elige un día.');
    if (!d.time) return setError('Elige un horario.');
    if (chosen.length !== d.people) return setError(`Elige ${d.people} ${d.people === 1 ? 'línea' : 'líneas'} (una por persona).`);
    if (!loggedIn) return onLogin();
    setError(null);
    setHolding(true);
    // Mismo id de operación mientras la selección no cambie: un reintento no duplica el apartado.
    const signature = `${d.packageId}|${d.date}|${d.time}|${chosen.map((l) => l.id).sort().join(',')}`;
    const op = d.holdOp && d.holdOp.startsWith(signature + '#') ? d.holdOp.split('#')[1]! : crypto.randomUUID();
    update({ holdOp: `${signature}#${op}` });
    try {
      const hold = await createHold({ operationId: op, packageId: d.packageId!, date: d.date, time: d.time, laneIds: chosen.map((l) => l.id) });
      if (hold.estado !== 'Vigente' || msUntil(hold.venceEnUtc) <= 0) {
        update({ holdOp: null, lanes: [] });
        setRefresh((n) => n + 1);
        return setError('Ese apartado ya venció. Elige de nuevo tus líneas.');
      }
      onHeld(hold);
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        update({ holdOp: null, lanes: [] });
        setRefresh((n) => n + 1);
      }
      setError(errorText(e, 'No pudimos apartar tus líneas. Intenta de nuevo.'));
    } finally {
      setHolding(false);
    }
  }

  if (days.loading && !days.data) return <LoadingScreen />;

  return (
    <section className={styles.step} aria-labelledby="paso-2">
      <h2 id="paso-2" className={styles.stepTitle}>
        2. Fecha, hora y líneas
      </h2>
      {error && <FormAlert>{error}</FormAlert>}
      {days.error && <FormAlert inline>No pudimos consultar la disponibilidad. Intenta de nuevo.</FormAlert>}
      {days.data && pickerDays.length === 0 && (
        <EmptyState title="No hay horarios disponibles" description="Por ahora no hay turnos en los próximos días para esta actividad. Prueba con otra o contáctanos." />
      )}

      {pickerDays.length > 0 && (
        <div className={styles.group}>
          <h3 className={styles.groupTitle}>Día</h3>
          <DayPicker days={pickerDays} value={d.date} onChange={(date) => update({ date, time: null, lanes: [] })} />
        </div>
      )}

      {day && (
        <div className={styles.group}>
          <h3 className={styles.groupTitle}>Hora</h3>
          <TimePicker slots={pickerDays.find((x) => x.date === day.fecha)?.slots ?? []} value={d.time} onChange={(time) => update({ time, lanes: [] })} />
        </div>
      )}

      {slot && (
        <div className={styles.group} aria-busy={days.loading}>
          <LineMap
            title={`Elige ${d.people} ${d.people === 1 ? 'línea' : 'líneas'} (${chosen.length}/${d.people})`}
            lines={lineList}
            selected={chosen.map((l) => l.numero)}
            onSelect={toggleLane}
          />
        </div>
      )}

      {!loggedIn && !authLoading && slot && (
        <p className={styles.stepLead}>
          Para apartar tus líneas necesitas una cuenta. <Link to="/login?next=/reservar">Inicia sesión</Link> o <Link to="/registro">crea tu cuenta</Link>: tu selección se conserva.
        </p>
      )}

      <WizardActions
        summary={summary}
        total={total}
        onBack={onBack}
        primary={
          <Button size="lg" loading={holding} onClick={() => void next()}>
            {loggedIn ? 'Apartar y continuar' : 'Iniciar sesión para apartar'}
          </Button>
        }
      />
    </section>
  );
}

/* ---------------- Paso 3: participantes ---------------- */

function HoldTimer({ iso, onExpired }: { iso: string; onExpired?: () => void }) {
  const left = useCountdown(iso);
  useEffect(() => {
    if (left === 0) onExpired?.();
  }, [left, onExpired]);
  return (
    <FormAlert tone={left > 60 ? 'success' : 'error'} inline>
      {left > 0 ? (
        <>
          Tus líneas están apartadas <strong>{mmss(left)}</strong> min (hasta las {clubTime(iso)}).
        </>
      ) : (
        'El apartado venció y las líneas se liberaron.'
      )}
    </FormAlert>
  );
}

function StepParticipants(props: {
  d: Draft;
  update: (c: Partial<Draft>) => void;
  summary: string;
  total: number;
  onExpired: () => void;
  onBack: () => void;
  onReserved: (r: ApiReservation) => void;
}) {
  const { d, update, summary, total, onExpired, onBack, onReserved } = props;
  const [error, setError] = useState<string | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [sending, setSending] = useState(false);
  const hold = d.hold!;
  const expired = useCountdown(hold.venceEnUtc) === 0;

  const setP = (i: number, patch: Partial<Participant>) => update({ participants: d.participants.map((p, j) => (j === i ? { ...p, ...patch } : p)) });
  const laneOf = (laneId: string) => d.lanes.find((l) => l.id === laneId)?.numero;

  async function submit() {
    for (const [i, p] of d.participants.entries()) {
      const problem = participantProblem(p, hold.fecha, i);
      if (problem) return setError(problem);
    }
    if (!accepted) return setError('Debes aceptar el reglamento y la carta responsiva.');
    setError(null);
    setSending(true);
    const op = d.reservationOp ?? crypto.randomUUID();
    update({ reservationOp: op });
    try {
      const r = await createReservation({ operationId: op, holdId: hold.id, participants: d.participants });
      if (r.estado !== 'PendientePago') return setError('El apartado ya venció. Vuelve a elegir tus líneas.');
      onReserved(r);
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) update({ reservationOp: null });
      setError(errorText(e, 'No pudimos registrar tu reserva. Intenta de nuevo.'));
    } finally {
      setSending(false);
    }
  }

  return (
    <section className={styles.step} aria-labelledby="paso-3">
      <h2 id="paso-3" className={styles.stepTitle}>
        3. Participantes
      </h2>
      <HoldTimer iso={hold.venceEnUtc} />
      {expired ? (
        <div>
          <Button size="lg" onClick={onExpired}>
            Elegir de nuevo
          </Button>
        </div>
      ) : (
        <>
          <p className={styles.stepLead}>
            Una persona por línea. Edad mínima 9 años; los menores de {ADULT_AGE} necesitan los datos de su tutor (adulto).
          </p>
          {error && <FormAlert>{error}</FormAlert>}
          {d.participants.map((p, i) => {
            const minor = p.birth ? ageOn(p.birth, hold.fecha) < ADULT_AGE : false;
            return (
              <fieldset key={p.laneId} className={styles.fieldset}>
                <legend className={styles.legend}>
                  Participante {i + 1} · Línea {laneOf(p.laneId) ?? '—'}
                </legend>
                <div className={styles.contactGrid}>
                  <TextField label="Nombre completo" maxLength={150} value={p.name} onChange={(e) => setP(i, { name: e.target.value })} />
                  <TextField label="Fecha de nacimiento" type="date" max={hold.fecha} value={p.birth} onChange={(e) => setP(i, { birth: e.target.value })} />
                  {minor && (
                    <>
                      <TextField label="Nombre del tutor" maxLength={150} value={p.tutorName} onChange={(e) => setP(i, { tutorName: e.target.value })} />
                      <TextField label="Nacimiento del tutor" type="date" max={hold.fecha} value={p.tutorBirth} onChange={(e) => setP(i, { tutorBirth: e.target.value })} />
                    </>
                  )}
                </div>
              </fieldset>
            );
          })}

          <div className={styles.waiver}>
            <div className={styles.waiverHead}>
              <IconFile size={22} className={styles.waiverIcon} />
              <h3 className={styles.waiverTitle}>Reglamento y carta responsiva</h3>
            </div>
            {/* TEXTO PENDIENTE: el club entrega la carta oficial. La firma y su validación llegan con R06. */}
            <div tabIndex={0} className={styles.waiverText} aria-label="Texto del reglamento">
              Declaro que conozco y acepto el reglamento del campo de tiro de PCP Shooting, que seguiré las indicaciones del
              personal y usaré el equipo de seguridad. Cada participante firma su carta responsiva en el club; los menores, por
              medio de su tutor. [TEXTO DE EJEMPLO: el club entrega la versión oficial.]
            </div>
            <Checkbox label="Acepto el reglamento y la carta responsiva" checked={accepted} onChange={(e) => setAccepted(e.target.checked)} />
          </div>
        </>
      )}

      <WizardActions
        summary={summary}
        total={total}
        onBack={onBack}
        primary={
          <Button size="lg" loading={sending} disabled={expired} onClick={() => void submit()}>
            Continuar: pago
          </Button>
        }
      />
    </section>
  );
}

/* ---------------- Paso 4: pago (pendiente de Mercado Pago) ---------------- */

function StepPayment({ reservation, laneNumbers, onDone }: { reservation: ApiReservation; laneNumbers: number[]; onDone: () => void }) {
  return (
    <section className={styles.step} aria-labelledby="paso-4">
      <h2 id="paso-4" className={styles.stepTitle}>
        4. Pago
      </h2>
      <HoldTimer iso={reservation.vencePagoEnUtc} />
      <FormAlert tone="error">
        El pago en línea con Mercado Pago todavía no está activo. Tu reserva de {formatMXN(reservation.total)} quedó registrada como
        «Pendiente de pago»; si no se paga, las líneas {laneNumbers.join(', ')} se liberan a las {clubTime(reservation.vencePagoEnUtc)}.
      </FormAlert>
      <div>
        <Button size="lg" onClick={onDone}>
          Ver mi reserva
        </Button>
      </div>
    </section>
  );
}
