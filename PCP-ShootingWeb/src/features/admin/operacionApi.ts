/*
 * Configuración operativa (R01) · API real. Base: /api/administracion/operacion
 *   GET/PUT  /configuracion        (PUT solo Administrador; requiere `version`)
 *   GET      /lineas · PUT /lineas/{id}   (PUT solo Administrador; nombre + activa + version)
 *   GET      /bloqueos · POST /bloqueos · PUT /bloqueos/{id}  (Administrador y Recepción; cancelar = activo:false)
 * Staff (solo Administrador):
 *   GET   /api/perfiles?rol=Administrador|Recepcion|Cliente&busqueda=
 *   PATCH /api/perfiles/{id}/acceso { rol, activo }   (no se puede cambiar el acceso propio)
 * Horas locales del club (America/Cancun, UTC−5 sin horario de verano). Días: 0 = domingo … 6 = sábado.
 */
import { apiGet, apiGetAll, apiPatch, apiPost, apiPut, type Page } from '@/lib/api';

const BASE = '/api/administracion/operacion';
export const CLUB_TZ = 'America/Cancun';
/** Cancún no tiene horario de verano: siempre UTC−05:00. */
const CLUB_OFFSET = '-05:00';

export const DAY_NAMES = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
/** Orden de la semana en pantalla: lunes primero. */
export const WEEK_ORDER = [1, 2, 3, 4, 5, 6, 0];

export type DaySchedule = {
  diaSemana: number;
  cerrado: boolean;
  apertura: string;
  cierre: string;
  primerTurno: string;
  ultimoTurno: string;
};

export type OperatingConfig = {
  zonaHoraria: string;
  duracionActividadMinutos: number;
  margenMinutos: number;
  intervaloTurnosMinutos: number;
  apartadoPagoMinutos: number;
  anticipacionMinimaMinutos: number;
  anticipacionMaximaDias: number;
  llegadaAnticipadaMinutos: number;
  toleranciaLlegadaMinutos: number;
  version: string;
  horarios: DaySchedule[];
};

export type Lane = { id: string; numero: number; nombre: string; activa: boolean; capacidad: number; version: string };

export type Block = {
  id: string;
  lineaId: string | null;
  inicioUtc: string;
  finUtc: string;
  motivo: string;
  activo: boolean;
  version: string;
};

/** "08:00:00" ↔ "08:00" (input type=time). */
export const toHHMM = (t: string) => t.slice(0, 5);
const toApiTime = (t: string) => (t.length === 5 ? `${t}:00` : t);

export async function getConfig(): Promise<OperatingConfig> {
  const c = await apiGet<OperatingConfig>(`${BASE}/configuracion`);
  return { ...c, horarios: c.horarios.map((h) => ({ ...h, apertura: toHHMM(h.apertura), cierre: toHHMM(h.cierre), primerTurno: toHHMM(h.primerTurno), ultimoTurno: toHHMM(h.ultimoTurno) })) };
}

export async function saveConfig(c: OperatingConfig): Promise<OperatingConfig> {
  await apiPut<OperatingConfig>(`${BASE}/configuracion`, {
    version: c.version,
    duracionActividadMinutos: c.duracionActividadMinutos,
    margenMinutos: c.margenMinutos,
    apartadoPagoMinutos: c.apartadoPagoMinutos,
    anticipacionMinimaMinutos: c.anticipacionMinimaMinutos,
    anticipacionMaximaDias: c.anticipacionMaximaDias,
    llegadaAnticipadaMinutos: c.llegadaAnticipadaMinutos,
    toleranciaLlegadaMinutos: c.toleranciaLlegadaMinutos,
    horarios: c.horarios.map((h) => ({
      diaSemana: h.diaSemana,
      cerrado: h.cerrado,
      apertura: toApiTime(h.apertura),
      cierre: toApiTime(h.cierre),
      primerTurno: toApiTime(h.primerTurno),
      ultimoTurno: toApiTime(h.ultimoTurno),
    })),
  });
  return getConfig();
}

const minutes = (t: string) => {
  const [h = 0, m = 0] = t.split(':').map(Number);
  return h * 60 + m;
};

/** Revisión local de un día abierto (la API vuelve a validar). Devuelve el problema o null. */
export function checkDay(h: DaySchedule, interval: number, activity: number, margin: number): string | null {
  if (h.cerrado) return null;
  const [open, close, first, last] = [h.apertura, h.cierre, h.primerTurno, h.ultimoTurno].map(minutes) as [number, number, number, number];
  if (open >= close) return 'La apertura debe ser antes del cierre.';
  if (first < open) return 'El primer turno debe ser después de la apertura.';
  if (last < first) return 'El último turno debe ser después del primero.';
  if (interval > 0 && (last - first) % interval !== 0) return `El último turno debe caer en la secuencia de cada ${interval} min desde el primero.`;
  if (last + activity + margin > close) return 'El último turno (con su margen) debe terminar antes del cierre.';
  return null;
}

/** Turnos de un día abierto (para mostrar cuántos hay). */
export function turnsOf(h: DaySchedule, interval: number): number {
  if (h.cerrado || interval <= 0) return 0;
  const n = Math.floor((minutes(h.ultimoTurno) - minutes(h.primerTurno)) / interval) + 1;
  return Math.max(0, n);
}

export const getLanes = () => apiGet<Lane[]>(`${BASE}/lineas`);
export const saveLane = (l: Lane) => apiPut<Lane>(`${BASE}/lineas/${l.id}`, { nombre: l.nombre.trim(), activa: l.activa, version: l.version });

/** Bloqueos activos que terminan desde hoy en adelante. */
export async function listUpcomingBlocks(): Promise<Block[]> {
  const startOfToday = new Date(`${todayInClub()}T00:00:00${CLUB_OFFSET}`).toISOString();
  const list = await apiGetAll<Block>(`${BASE}/bloqueos`, { activo: true, desdeUtc: startOfToday });
  return list.sort((a, b) => a.inicioUtc.localeCompare(b.inicioUtc));
}

/** Fecha de hoy en el club (YYYY-MM-DD). */
export function todayInClub(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: CLUB_TZ, year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

/** Fecha y hora locales del club → ISO con offset. "24:00" = fin del día (día siguiente 00:00). */
export function clubDateTime(date: string, time: string): string {
  if (time === '24:00') {
    const d = new Date(`${date}T00:00:00${CLUB_OFFSET}`);
    d.setUTCDate(d.getUTCDate() + 1);
    return d.toISOString();
  }
  return `${date}T${toApiTime(time)}${CLUB_OFFSET}`;
}

export function createBlock(b: { lineaId: string | null; date: string; from: string; to: string; motivo: string }) {
  return apiPost<Block>(`${BASE}/bloqueos`, {
    lineaId: b.lineaId,
    inicioUtc: clubDateTime(b.date, b.from),
    finUtc: clubDateTime(b.date, b.to),
    motivo: b.motivo.trim(),
    activo: true,
  });
}

export const cancelBlock = (b: Block) =>
  apiPut<Block>(`${BASE}/bloqueos/${b.id}`, {
    lineaId: b.lineaId,
    inicioUtc: b.inicioUtc.endsWith('Z') ? b.inicioUtc : `${b.inicioUtc}Z`,
    finUtc: b.finUtc.endsWith('Z') ? b.finUtc : `${b.finUtc}Z`,
    motivo: b.motivo,
    activo: false,
    version: b.version,
  });

/** "mar 25 nov, 11:00–13:00" en hora del club. */
export function blockRange(b: Block): string {
  const utc = (s: string) => new Date(s.endsWith('Z') || /[+-]\d\d:\d\d$/.test(s) ? s : `${s}Z`);
  const day = new Intl.DateTimeFormat('es-MX', { timeZone: CLUB_TZ, weekday: 'short', day: 'numeric', month: 'short' });
  const time = new Intl.DateTimeFormat('es-MX', { timeZone: CLUB_TZ, hour: '2-digit', minute: '2-digit', hour12: false });
  const start = utc(b.inicioUtc);
  const end = utc(b.finUtc);
  const sameDay = day.format(start) === day.format(new Date(end.getTime() - 1));
  return sameDay
    ? `${day.format(start)}, ${time.format(start)}–${time.format(end) === '00:00' ? '24:00' : time.format(end)}`
    : `${day.format(start)} ${time.format(start)} → ${day.format(end)} ${time.format(end)}`;
}

/* ---------- Staff ---------- */

export type ApiRole = 'Cliente' | 'Administrador' | 'Recepcion';
export type StaffProfile = { id: string; nombre: string; telefono: string | null; rol: ApiRole; activo: boolean };

export async function listStaff(): Promise<StaffProfile[]> {
  const [admins, reception] = await Promise.all([
    apiGetAll<StaffProfile>('/api/perfiles', { rol: 'Administrador' }),
    apiGetAll<StaffProfile>('/api/perfiles', { rol: 'Recepcion' }),
  ]);
  return [...admins, ...reception].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));
}

/** Busca clientes registrados por nombre o teléfono (para darles acceso al panel). */
export async function searchCustomers(q: string): Promise<StaffProfile[]> {
  const page = await apiGet<Page<StaffProfile>>('/api/perfiles', { rol: 'Cliente', activo: true, busqueda: q.trim(), tamanoPagina: 10 });
  return page.elementos;
}

export const setAccess = (id: string, rol: ApiRole, activo = true) => apiPatch<StaffProfile>(`/api/perfiles/${id}/acceso`, { rol, activo });
