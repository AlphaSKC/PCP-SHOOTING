import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import {
  Button,
  ButtonAnchor,
  ButtonLink,
  Checkbox,
  Chip,
  ChipGroup,
  FormAlert,
  SelectField,
  StatusBadge,
  TextField,
} from '@/components/ui';
import { IconChevronLeft, IconChevronRight, IconPlus, IconQr } from '@/components/icons/icons';
import { packageName, reservationBadge, shortDayYear, timeToMinutes } from '@/features/admin/format';
import { QrScanner } from '@/features/admin/QrScanner';
import { todayISO, type AdminReservation } from '@/features/admin/seed';
import { checkIn, newId, update, useAdmin, waLink } from '@/features/admin/store';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import { useAuth } from '@/features/auth/AuthProvider';
import { TOTAL_LINES } from '@/features/reservas/config';
import { cx } from '@/lib/format';
import styles from './AdminReservasPage.module.css';

type Mode = 'detail' | 'new' | 'block';
type Filter = 'todas' | 'por_llegar' | 'llegaron' | 'canceladas';

const shift = (iso: string, days: number) => {
  const d = new Date(`${iso}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
};

const pad = (n: number) => String(n).padStart(2, '0');
const toTime = (min: number) => `${Math.floor(min / 60)}:${pad(min % 60)}`;

export default function AdminReservasPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const reservations = useAdmin((d) => d.reservations);
  const blocks = useAdmin((d) => d.blocks);
  const settings = useAdmin((d) => d.settings);
  const packages = useAdmin((d) => d.packages);

  const [date, setDate] = useState(todayISO());
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mode, setMode] = useState<Mode>(params.get('nueva') ? 'new' : 'detail');
  const [code, setCode] = useState('');
  const [notice, setNotice] = useState<{ tone: 'error' | 'success'; text: string } | null>(null);
  const [scanning, setScanning] = useState(false);
  const [filter, setFilter] = useState<Filter>('todas');
  const [hour, setHour] = useState<string | null>(null);

  // Horarios del día según la configuración (apertura, cierre e intervalo).
  const times = useMemo(() => {
    const day = settings.schedule[(new Date(`${date}T12:00:00Z`).getUTCDay() + 6) % 7];
    if (!day?.open) return [];
    const out: string[] = [];
    for (let m = timeToMinutes(day.from); m + settings.intervalMinutes <= timeToMinutes(day.to); m += settings.intervalMinutes) out.push(toTime(m));
    return out;
  }, [date, settings]);

  const dayRes = useMemo(() => reservations.filter((r) => r.date === date && r.status !== 'cancelada'), [reservations, date]);
  const dayBlocks = useMemo(() => blocks.filter((b) => b.date === date), [blocks, date]);
  const selected = reservations.find((r) => r.id === selectedId) ?? null;
  const inactive = (line: number) => !settings.activeLines.includes(line);
  const blockFor = (line: number, time: string) =>
    dayBlocks.find((b) => b.line === line && (b.time === null || b.time === time)) ?? (inactive(line) ? { reason: 'Inactiva' } : null);

  useEffect(() => {
    if (params.get('nueva')) {
      setMode('new');
      params.delete('nueva');
      setParams(params, { replace: true });
    }
  }, [params, setParams]);

  const doCheckIn = useCallback(
    (value: string) => {
      setScanning(false);
      const result = checkIn(value, user?.name ?? 'Staff');
      if (result.ok) {
        setSelectedId(result.reservation.id);
        setMode('detail');
        setNotice({ tone: 'success', text: `Llegada registrada: ${result.reservation.client} · Línea ${result.reservation.line}. Consumibles descontados del inventario.` });
        setCode('');
      } else setNotice({ tone: 'error', text: result.error });
    },
    [user],
  );

  const select = (r: AdminReservation) => {
    setSelectedId(r.id);
    setMode('detail');
    setNotice(null);
    if (window.matchMedia('(max-width: 1023px)').matches) {
      requestAnimationFrame(() => document.getElementById('detalle')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }
  };

  const listItems = useMemo(
    () =>
      reservations
        .filter((r) => r.date === date)
        .filter((r) =>
          filter === 'todas'
            ? r.status !== 'cancelada'
            : filter === 'por_llegar'
              ? r.status === 'confirmada' || r.status === 'pendiente_pago'
              : filter === 'llegaron'
                ? r.status === 'utilizada'
                : r.status === 'cancelada',
        )
        .sort((a, b) => timeToMinutes(a.time) - timeToMinutes(b.time) || a.line - b.line),
    [reservations, date, filter],
  );

  const mapHour = hour ?? times.find((t) => dayRes.some((r) => r.time === t)) ?? times[0] ?? '9:00';

  return (
    <>
      <title>Reservas · Panel PCP</title>
      <AdminPageHeader
        title="Reservas"
        subtitle="Calendario de las 15 líneas de tiro · da clic en una reserva para ver el detalle"
        actions={
          <>
            <ButtonLink to="/" variant="secondary" className={styles.hideMobile}>
              Ver sitio
            </ButtonLink>
            <Button
              iconStart={<IconPlus size={16} />}
              onClick={() => {
                setMode('new');
                setNotice(null);
              }}
            >
              Nueva reserva
            </Button>
          </>
        }
      />
      <AdminBody>
        <div className={styles.toolbar}>
          <div className={styles.dateNav}>
            <button type="button" className={styles.navBtn} aria-label="Día anterior" onClick={() => setDate((d) => shift(d, -1))}>
              <IconChevronLeft size={20} />
            </button>
            <span className={styles.datePill}>{shortDayYear(date)}</span>
            <button type="button" className={styles.navBtn} aria-label="Día siguiente" onClick={() => setDate((d) => shift(d, 1))}>
              <IconChevronRight size={20} />
            </button>
            <Button variant="secondary" onClick={() => setDate(todayISO())} className={styles.hideMobile}>
              Hoy
            </Button>
          </div>

          <form
            id="llegada"
            className={styles.checkin}
            onSubmit={(e) => {
              e.preventDefault();
              if (code.trim()) doCheckIn(code);
            }}
          >
            <Button size="lg" iconStart={<IconQr size={20} />} onClick={() => setScanning(true)} className={styles.scan}>
              Escanear QR de llegada
            </Button>
            <TextField
              label="Registrar llegada"
              placeholder="Código PCP-… o escanea el QR"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              className={styles.codeField}
            />
            <Button type="submit" variant="dark" className={styles.codeSubmit}>
              Registrar
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                setMode('block');
                setNotice(null);
              }}
              className={styles.hideMobile}
            >
              Bloquear líneas
            </Button>
          </form>
        </div>

        {notice && <FormAlert tone={notice.tone}>{notice.text}</FormAlert>}

        <Split
          asideWidth={320}
          main={
            <>
              {/* Escritorio y tableta: calendario de líneas × horario */}
              <div className={styles.calendarWrap}>
                <ul className={styles.legend} aria-label="Leyenda">
                  <li><span className={cx(styles.sw, styles.swBooked)} />Reservada</li>
                  <li><span className={cx(styles.sw, styles.swArrived)} />Ya llegó</li>
                  <li><span className={cx(styles.sw, styles.swSelected)} />Seleccionada</li>
                  <li><span className={cx(styles.sw, styles.swFree)} />Libre</li>
                  <li><span className={cx(styles.sw, styles.swBlocked)} />Bloqueada</li>
                </ul>
                {times.length === 0 ? (
                  <p className={styles.closed}>El club está cerrado este día.</p>
                ) : (
                  <div className={styles.scroll}>
                    <table className={styles.calendar}>
                      <caption className="visually-hidden">Reservas del {shortDayYear(date)} por línea y horario</caption>
                      <thead>
                        <tr>
                          <th scope="col">Línea</th>
                          {times.map((t) => (
                            <th key={t} scope="col">{t}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).map((line) => (
                          <tr key={line}>
                            <th scope="row">Línea {line}</th>
                            {times.map((t) => {
                              const r = dayRes.find((x) => x.line === line && x.time === t);
                              const b = !r && blockFor(line, t);
                              return (
                                <td key={t}>
                                  {r ? (
                                    <button
                                      type="button"
                                      className={cx(
                                        styles.cell,
                                        r.status === 'utilizada' ? styles.arrived : styles.booked,
                                        r.status === 'pendiente_pago' && styles.unpaid,
                                        r.id === selectedId && styles.selected,
                                      )}
                                      onClick={() => select(r)}
                                      aria-pressed={r.id === selectedId}
                                    >
                                      <strong>{r.client}</strong>
                                      <span>{r.status === 'utilizada' ? 'Llegó' : packageName(r.packageId)}</span>
                                    </button>
                                  ) : b ? (
                                    <span className={cx(styles.cell, styles.blocked)}>{b.reason}</span>
                                  ) : (
                                    <span className={cx(styles.cell, styles.free)} aria-label="Libre" />
                                  )}
                                </td>
                              );
                            })}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Celular: mapa de líneas por hora + lista (MAdminReservas) */}
              <div className={styles.mobileView}>
                {times.length > 0 && (
                  <div className={styles.miniMap}>
                    <div className={styles.miniHead}>
                      <SelectField
                        label="Líneas a las"
                        value={mapHour}
                        onChange={(e) => setHour(e.target.value)}
                        options={times.map((t) => ({ value: t, label: t }))}
                      />
                      <span>
                        {dayRes.filter((r) => r.time === mapHour).length} ocupadas ·{' '}
                        {Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).filter((l) => blockFor(l, mapHour)).length} bloqueadas
                      </span>
                    </div>
                    <ol className={styles.miniLines}>
                      {Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).map((l) => {
                        const busy = dayRes.some((r) => r.line === l && r.time === mapHour);
                        const blocked = !busy && blockFor(l, mapHour);
                        return (
                          <li key={l} className={cx(busy && styles.miniBusy, blocked && styles.miniBlocked)} aria-label={`Línea ${l}${busy ? ' ocupada' : blocked ? ' bloqueada' : ' libre'}`}>
                            {l}
                          </li>
                        );
                      })}
                    </ol>
                  </div>
                )}
                <ChipGroup label="Filtrar reservas">
                  {(
                    [
                      ['todas', 'Todas'],
                      ['por_llegar', 'Por llegar'],
                      ['llegaron', 'Llegaron'],
                      ['canceladas', 'Canceladas'],
                    ] as [Filter, string][]
                  ).map(([id, label]) => (
                    <Chip key={id} selected={filter === id} onClick={() => setFilter(id)}>
                      {label}
                    </Chip>
                  ))}
                </ChipGroup>
                <ul className={styles.cards}>
                  {listItems.length === 0 && <li className={styles.closed}>Sin reservas con este filtro.</li>}
                  {listItems.map((r) => (
                    <li key={r.id}>
                      <button type="button" className={cx(styles.card, r.id === selectedId && styles.cardSelected)} onClick={() => select(r)}>
                        <strong className={styles.cardTime}>{r.time}</strong>
                        <span className={styles.cardText}>
                          <strong>{r.client}</strong>
                          <span>
                            {packageName(r.packageId)} · {r.people} pers. · Línea {r.line}
                          </span>
                        </span>
                        <StatusBadge status={reservationBadge(r)} />
                      </button>
                    </li>
                  ))}
                </ul>
                <Button variant="secondary" size="lg" fullWidth onClick={() => setMode('block')}>
                  Bloquear líneas
                </Button>
              </div>
            </>
          }
          aside={
            mode === 'new' ? (
              <NewReservationPanel
                date={date}
                times={times}
                packages={packages.filter((p) => p.active)}
                isTaken={(line, time) => dayRes.some((r) => r.line === line && r.time === time) || Boolean(blockFor(line, time))}
                onDone={(r) => {
                  setSelectedId(r.id);
                  setMode('detail');
                  setNotice({ tone: 'success', text: `Reserva ${r.code} creada para ${r.client}.` });
                }}
                onCancel={() => setMode('detail')}
              />
            ) : mode === 'block' ? (
              <BlockPanel
                date={date}
                times={times}
                onDone={(text) => {
                  setMode('detail');
                  setNotice({ tone: 'success', text });
                }}
                onCancel={() => setMode('detail')}
              />
            ) : (
              <DetailPanel
                reservation={selected}
                onCheckIn={() => selected && doCheckIn(selected.code)}
                onCancelReservation={() => {
                  if (!selected) return;
                  if (!window.confirm(`¿Cancelar la reserva ${selected.code} de ${selected.client}?`)) return;
                  update((d) => ({ ...d, reservations: d.reservations.map((r) => (r.id === selected.id ? { ...r, status: 'cancelada' } : r)) }));
                  setNotice({ tone: 'success', text: `Reserva ${selected.code} cancelada.` });
                }}
              />
            )
          }
        />
      </AdminBody>
      <QrScanner open={scanning} onClose={() => setScanning(false)} onCode={doCheckIn} />
    </>
  );
}

function DetailPanel({
  reservation: r,
  onCheckIn,
  onCancelReservation,
}: {
  reservation: AdminReservation | null;
  onCheckIn: () => void;
  onCancelReservation: () => void;
}) {
  if (!r) {
    return (
      <Panel title="Reserva seleccionada" id="detalle">
        <p className={styles.muted}>Elige una reserva del calendario o registra una llegada con su código QR.</p>
      </Panel>
    );
  }
  const canCheckIn = r.status === 'confirmada';
  return (
    <Panel title="Reserva seleccionada" id="detalle">
      <div>
        <p className={styles.client}>{r.client}</p>
        <p className={styles.muted}>
          Código {r.code} · {r.phone}
        </p>
      </div>
      <dl className={styles.facts}>
        <div><dt>Actividad</dt><dd>{packageName(r.packageId)}</dd></div>
        <div><dt>Personas</dt><dd>{r.people}</dd></div>
        <div><dt>Línea</dt><dd>Línea {r.line}</dd></div>
        <div><dt>Hora</dt><dd>{r.time}</dd></div>
        <div><dt>Pago</dt><dd><StatusBadge status={r.paid ? 'pagado' : 'pendiente_pago'} /></dd></div>
        <div><dt>Carta responsiva</dt><dd><StatusBadge status={r.waiver} /></dd></div>
      </dl>
      {r.status === 'utilizada' && <FormAlert tone="success">Llegada registrada.</FormAlert>}
      {r.status === 'cancelada' && <FormAlert>Reserva cancelada.</FormAlert>}
      {r.waiver === 'pendiente' && canCheckIn && <FormAlert>Pide al cliente firmar la carta responsiva antes de disparar.</FormAlert>}
      <Button size="lg" fullWidth disabled={!canCheckIn} onClick={onCheckIn}>
        Registrar llegada
      </Button>
      <div className={styles.row2}>
        {/* PENDIENTE (CLAUDE.md §8): políticas de cambio y cancelación. */}
        <Button variant="secondary" disabled title="Pendiente: política de reprogramación">
          Cambiar horario
        </Button>
        <Button variant="secondary" disabled={r.status !== 'confirmada' && r.status !== 'pendiente_pago'} onClick={onCancelReservation}>
          Cancelar
        </Button>
      </div>
      <ButtonAnchor
        href={waLink(r.phone, `Hola ${r.client.split(' ')[0]}, te escribimos de PCP Shooting sobre tu reserva ${r.code} (${r.time}, línea ${r.line}).`)}
        target="_blank"
        rel="noreferrer"
        variant="text"
        className={styles.wa}
      >
        Escribir por WhatsApp
      </ButtonAnchor>
    </Panel>
  );
}

function NewReservationPanel({
  date,
  times,
  packages,
  isTaken,
  onDone,
  onCancel,
}: {
  date: string;
  times: string[];
  packages: { id: string; name: string; price: number }[];
  isTaken: (line: number, time: string) => boolean;
  onDone: (r: AdminReservation) => void;
  onCancel: () => void;
}) {
  const [f, setF] = useState({ client: '', phone: '', packageId: packages[0]?.id ?? '', people: '2', time: times[0] ?? '', line: '', paid: false });
  const [error, setError] = useState<string | null>(null);
  const freeLines = Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).filter((l) => !isTaken(l, f.time));

  const save = () => {
    if (f.client.trim().length < 2) return setError('Escribe el nombre del cliente.');
    if (!/^\d{10}$/.test(f.phone.replace(/\D/g, ''))) return setError('Escribe un teléfono de 10 dígitos.');
    if (!f.time || !f.line) return setError('Elige horario y línea.');
    if (isTaken(Number(f.line), f.time)) return setError('Esa línea ya está ocupada en ese horario.');
    const id = newId('R');
    const r: AdminReservation = {
      id,
      code: `PCP-${id.replace(/\D/g, '').slice(-4)}`,
      client: f.client.trim(),
      phone: f.phone,
      packageId: f.packageId,
      people: Number(f.people),
      date,
      time: f.time,
      line: Number(f.line),
      // Cobro anticipado del 100%: sin pago registrado queda pendiente (CLAUDE.md §6).
      status: f.paid ? 'confirmada' : 'pendiente_pago',
      paid: f.paid,
      waiver: 'pendiente',
      equipment: 'renta',
    };
    update((d) => ({ ...d, reservations: [...d.reservations, r] }));
    onDone(r);
  };

  return (
    <Panel title="Nueva reserva" id="detalle">
      <p className={styles.muted}>Por teléfono o en mostrador · {shortDayYear(date)}</p>
      {error && <FormAlert>{error}</FormAlert>}
      <TextField label="Cliente" value={f.client} onChange={(e) => setF({ ...f, client: e.target.value })} />
      <TextField label="Teléfono / WhatsApp" type="tel" value={f.phone} onChange={(e) => setF({ ...f, phone: e.target.value })} />
      <SelectField
        label="Paquete"
        value={f.packageId}
        onChange={(e) => setF({ ...f, packageId: e.target.value })}
        options={packages.map((p) => ({ value: p.id, label: `${p.name} · $${p.price} por persona` }))}
      />
      <div className={styles.row2}>
        <SelectField label="Personas" value={f.people} onChange={(e) => setF({ ...f, people: e.target.value })} options={[1, 2, 3, 4, 5, 6].map((n) => ({ value: String(n), label: String(n) }))} />
        <SelectField label="Hora" value={f.time} onChange={(e) => setF({ ...f, time: e.target.value, line: '' })} options={times.map((t) => ({ value: t, label: t }))} />
      </div>
      <SelectField
        label="Línea"
        value={f.line}
        placeholder={freeLines.length ? 'Elige una línea libre' : 'Sin líneas libres'}
        onChange={(e) => setF({ ...f, line: e.target.value })}
        options={freeLines.map((l) => ({ value: String(l), label: `Línea ${l}` }))}
      />
      <Checkbox label="Pago recibido en mostrador (100%)" checked={f.paid} onChange={(e) => setF({ ...f, paid: e.target.checked })} />
      <Button size="lg" fullWidth onClick={save}>
        Crear reserva
      </Button>
      <Button variant="secondary" fullWidth onClick={onCancel}>
        Cancelar
      </Button>
    </Panel>
  );
}

function BlockPanel({ date, times, onDone, onCancel }: { date: string; times: string[]; onDone: (msg: string) => void; onCancel: () => void }) {
  const [lines, setLines] = useState<number[]>([]);
  const [time, setTime] = useState('todo');
  const [reason, setReason] = useState('Mantenimiento');
  const toggle = (l: number) => setLines((prev) => (prev.includes(l) ? prev.filter((x) => x !== l) : [...prev, l]));

  return (
    <Panel title="Bloquear líneas" id="detalle">
      <p className={styles.muted}>{shortDayYear(date)} · las líneas bloqueadas no se pueden reservar.</p>
      <div role="group" aria-label="Líneas a bloquear" className={styles.lineToggles}>
        {Array.from({ length: TOTAL_LINES }, (_, i) => i + 1).map((l) => (
          <button key={l} type="button" aria-pressed={lines.includes(l)} className={cx(styles.lineToggle, lines.includes(l) && styles.lineToggleOn)} onClick={() => toggle(l)}>
            {l}
          </button>
        ))}
      </div>
      <SelectField label="Horario" value={time} onChange={(e) => setTime(e.target.value)} options={[{ value: 'todo', label: 'Todo el día' }, ...times.map((t) => ({ value: t, label: t }))]} />
      <TextField label="Motivo" value={reason} onChange={(e) => setReason(e.target.value)} />
      <Button
        size="lg"
        fullWidth
        disabled={lines.length === 0}
        onClick={() => {
          update((d) => ({
            ...d,
            blocks: [...d.blocks, ...lines.map((line) => ({ id: newId('B'), date, line, time: time === 'todo' ? null : time, reason: reason || 'Bloqueada' }))],
          }));
          onDone(`Líneas ${lines.sort((a, b) => a - b).join(', ')} bloqueadas${time === 'todo' ? ' todo el día' : ` a las ${time}`}.`);
        }}
      >
        Bloquear {lines.length || ''} {lines.length === 1 ? 'línea' : 'líneas'}
      </Button>
      <Button variant="secondary" fullWidth onClick={onCancel}>
        Cancelar
      </Button>
    </Panel>
  );
}
