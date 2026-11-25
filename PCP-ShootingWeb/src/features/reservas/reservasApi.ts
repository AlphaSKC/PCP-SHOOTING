/*
 * Reservaciones web · API real (VITE_DATA_SOURCE=api). R03 + R04.1 del backend.
 *   GET  /api/paquetes                                         (público, paquetes visibles)
 *   GET  /api/disponibilidad/ventana                            (público; fechas reservables según la anticipación, F05)
 *   GET  /api/disponibilidad?paqueteId&fecha[&personas]        (público; turnos + estado de las 15 líneas)
 *   POST /api/apartados  { idOperacion, paqueteId, fecha, hora, lineas[] }   (sesión; aparta N líneas N minutos)
 *   POST /api/reservas   { idOperacion, apartadoId, participantes[] }        (sesión; reserva PendientePago)
 *   GET  /api/reservas/{id} · GET /api/reservas                (solo propias)
 * Reglas: una persona por línea; un participante por línea (≥ 9 años; menores de 18 con tutor adulto).
 * La reserva conserva el vencimiento del apartado. SIN Mercado Pago (R05) la reserva no se confirma:
 * vence y libera las líneas. Horas locales del club (America/Cancun).
 */
import { apiGet, apiGetAll, apiPost, type Page } from '@/lib/api';

export const CLUB_TZ = 'America/Cancun';
export const MAX_PEOPLE_API = 15;
export const MIN_AGE = 9;
export const ADULT_AGE = 18;

/* ---------- Paquetes ---------- */

export type ApiPackage = {
  id: string;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  incluyeMarkdown: string;
  requisitosMarkdown: string | null;
  precioPorPersona: number | null;
  precioSocioCartaq: number | null;
};

export const getApiPackages = () => apiGetAll<ApiPackage>('/api/paquetes');

/* ---------- Disponibilidad ---------- */

export type LaneState = 'Libre' | 'Bloqueada' | 'Inactiva' | 'Apartada' | 'FueraDeAnticipacion';
export type ApiLane = { id: string; numero: number; nombre: string; disponible: boolean; estado: LaneState };
export type ApiSlot = { hora: string; inicioUtc: string; disponible: boolean; lineasLibres: number; lineas: ApiLane[] };
export type ApiDay = { fecha: string; diaAbierto: boolean; horarios: ApiSlot[] };

/** Fecha local del club (YYYY-MM-DD) desplazada `offset` días desde hoy. */
export function clubDate(offset = 0): string {
  const today = new Intl.DateTimeFormat('en-CA', { timeZone: CLUB_TZ, year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
  const d = new Date(`${today}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + offset);
  return d.toISOString().slice(0, 10);
}

export const getAvailability = (packageId: string, date: string, people: number) =>
  apiGet<ApiDay>('/api/disponibilidad', { paqueteId: packageId, fecha: date, personas: people }, undefined);

/** Ventana pública de reserva (F05): primera y última fecha local permitidas por la anticipación. */
export type BookingWindow = { primeraFecha: string; ultimaFecha: string; anticipacionMinimaMinutos: number; anticipacionMaximaDias: number };
export const getBookingWindow = () => apiGet<BookingWindow>('/api/disponibilidad/ventana');

const nextDate = (d: string) => {
  const x = new Date(`${d}T12:00:00Z`);
  x.setUTCDate(x.getUTCDate() + 1);
  return x.toISOString().slice(0, 10);
};

/**
 * Días reservables: solo las fechas de la ventana pública (no garantiza turnos ni líneas libres:
 * cada fecha se consulta en /api/disponibilidad y se descartan las cerradas o sin turnos).
 */
export async function getBookableDays(packageId: string, people: number): Promise<ApiDay[]> {
  const w = await getBookingWindow();
  const dates: string[] = [];
  for (let d = w.primeraFecha; d <= w.ultimaFecha && dates.length < 31; d = nextDate(d)) dates.push(d);
  const days = await Promise.all(dates.map((d) => getAvailability(packageId, d, people)));
  return days.filter((d) => d.diaAbierto && d.horarios.length > 0);
}

/* ---------- Apartados ---------- */

export type ApiHold = {
  id: string;
  paqueteId: string;
  fecha: string;
  hora: string;
  venceEnUtc: string;
  estado: 'Vigente' | 'Vencido';
  lineas: string[];
  /** F04: número y nombre de cada línea, ordenados por número. */
  lineasDetalle?: { lineaId: string; numeroLinea: number; nombreLinea: string }[];
};

export function createHold(input: { operationId: string; packageId: string; date: string; time: string; laneIds: string[] }) {
  return apiPost<ApiHold>('/api/apartados', {
    idOperacion: input.operationId,
    paqueteId: input.packageId,
    fecha: input.date,
    hora: input.time.length === 5 ? `${input.time}:00` : input.time,
    lineas: input.laneIds,
  });
}

/* ---------- Reservas ---------- */

export type Participant = { laneId: string; name: string; birth: string; tutorName: string; tutorBirth: string };

export type ApiReservation = {
  id: string;
  apartadoId: string;
  paqueteId: string;
  codigoPaquete: string;
  nombrePaquete: string;
  descripcionPaquete: string | null;
  incluyeMarkdown: string;
  requisitosMarkdown: string | null;
  fecha: string;
  hora: string;
  inicioUtc: string;
  vencePagoEnUtc: string;
  creadoEnUtc: string;
  precioPorPersona: number;
  precioSocioInformativo: number | null;
  total: number;
  estado: ReservationState;
  participantes: {
    lineaId: string;
    nombre: string;
    nacimiento: string;
    tutorNombre: string | null;
    tutorNacimiento: string | null;
    /** F04: número y nombre actuales de la línea. */
    numeroLinea?: number;
    nombreLinea?: string;
  }[];
};

/** Estados de la reserva web. `Pagada` llega cuando el pago se confirma (Mercado Pago o registro manual). */
export type ReservationState = 'PendientePago' | 'Vencida' | 'Pagada';

export type ApiReservationSummary = {
  id: string;
  nombrePaquete: string;
  inicioUtc: string;
  vencePagoEnUtc: string;
  total: number;
  estado: ReservationState;
  /** F04: participantes (= líneas) de la reserva. */
  cantidadParticipantes?: number;
};

export function createReservation(input: { operationId: string; holdId: string; participants: Participant[] }) {
  return apiPost<ApiReservation>('/api/reservas', {
    idOperacion: input.operationId,
    apartadoId: input.holdId,
    participantes: input.participants.map((p) => ({
      lineaId: p.laneId,
      nombre: p.name.trim(),
      nacimiento: p.birth,
      tutorNombre: p.tutorName.trim() || null,
      tutorNacimiento: p.tutorBirth || null,
    })),
  });
}

export const getApiReservation = (id: string) => apiGet<ApiReservation>(`/api/reservas/${id}`);

export async function listMyReservations(): Promise<ApiReservationSummary[]> {
  const page = await apiGet<Page<ApiReservationSummary>>('/api/reservas', { pagina: 1, tamanoPagina: 20 });
  return page.elementos;
}

/* ---------- Utilidades ---------- */

/** Edad cumplida en la fecha de la actividad. */
export function ageOn(birth: string, onDate: string): number {
  const [by, bm, bd] = birth.split('-').map(Number) as [number, number, number];
  const [y, m, d] = onDate.split('-').map(Number) as [number, number, number];
  return y - by - (m < bm || (m === bm && d < bd) ? 1 : 0);
}

/** Valida participantes como la API (para avisar antes de enviar). */
export function participantProblem(p: Participant, activityDate: string, index: number): string | null {
  const who = `Participante ${index + 1}`;
  if (p.name.trim().length < 2) return `${who}: escribe su nombre completo.`;
  if (!p.birth) return `${who}: escribe su fecha de nacimiento.`;
  const age = ageOn(p.birth, activityDate);
  if (age < MIN_AGE) return `${who}: la edad mínima es de ${MIN_AGE} años.`;
  if (age < ADULT_AGE) {
    if (p.tutorName.trim().length < 2 || !p.tutorBirth) return `${who} es menor de edad: escribe el nombre y nacimiento de su tutor.`;
    if (ageOn(p.tutorBirth, activityDate) < ADULT_AGE) return `${who}: el tutor debe ser mayor de edad.`;
  }
  return null;
}

const time = new Intl.DateTimeFormat('es-MX', { timeZone: CLUB_TZ, hour: '2-digit', minute: '2-digit', hour12: false });
const utc = (s: string) => new Date(/Z$|[+-]\d\d:\d\d$/.test(s) ? s : `${s}Z`);
/** Hora del club de un instante UTC ("14:35"). */
export const clubTime = (isoUtc: string) => time.format(utc(isoUtc));
export const msUntil = (isoUtc: string) => utc(isoUtc).getTime() - Date.now();

const longDay = new Intl.DateTimeFormat('es-MX', { timeZone: CLUB_TZ, weekday: 'long', day: 'numeric', month: 'long' });
export const clubLongDay = (isoUtc: string) => longDay.format(utc(isoUtc));

/** Convierte el Markdown simple de "qué incluye" en una lista de texto (sin HTML: seguro). */
export function markdownItems(md: string | null): string[] {
  return (md ?? '')
    .split(/\r?\n/)
    .map((l) => l.replace(/^\s*(?:[-*+]|\d+\.)\s+/, '').replace(/[*_`#>]/g, '').trim())
    .filter(Boolean);
}

/** Números de línea de una reserva (desde la API, F04). */
export const laneNumbersOf = (r: Pick<ApiReservation, 'participantes'>) =>
  r.participantes.map((p) => p.numeroLinea).filter((n): n is number => typeof n === 'number').sort((a, b) => a - b);
