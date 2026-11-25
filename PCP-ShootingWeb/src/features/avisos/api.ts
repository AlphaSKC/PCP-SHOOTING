/*
 * Avisos públicos del sitio · API real. Contrato: docs/AVISOS-PUBLICOS.md
 *   GET  /api/avisos-publicos                        (anónimo) solo activos y vigentes: id, titulo, texto, orden, inicioUtc, finUtc
 *   GET  /api/administracion/avisos-publicos         (Administrador) todos, con estado calculado y version
 *   POST /api/administracion/avisos-publicos         { titulo, texto, activo, orden, inicioUtc, finUtc }
 *   PUT  /api/administracion/avisos-publicos/{id}    mismos campos + version (409 = otro cambio; recargar, nunca sobrescribir)
 * Texto plano (no HTML ni Markdown). Inicio incluido, fin excluido. Sin borrado: se desactiva con activo=false.
 * Independiente de las plantillas de correo/WhatsApp (F10, /api/administracion/avisos).
 */
import { apiGetAll, apiPost, apiPut } from '@/lib/api';

export type NoticeState = 'Inactivo' | 'Programado' | 'Vigente' | 'Vencido';

export type PublicNotice = { id: string; title: string; text: string; order: number; startUtc: string | null; endUtc: string | null };

export type NoticeDraft = {
  id: string | null;
  version: string | null;
  title: string;
  text: string;
  active: boolean;
  order: number;
  /** Fecha y hora del club (AAAA-MM-DDTHH:mm) o '' = sin límite. */
  start: string;
  end: string;
  state: NoticeState | null;
  updatedUtc: string | null;
};

type ApiPublic = { id: string; titulo: string | null; texto: string; orden: number; inicioUtc: string | null; finUtc: string | null };
type ApiAdmin = ApiPublic & { activo: boolean; estado: NoticeState; version: string; creadoEnUtc: string; actualizadoEnUtc: string };

const PUBLIC = '/api/avisos-publicos';
const ADMIN = '/api/administracion/avisos-publicos';
export const TITLE_MAX = 120;
export const TEXT_MAX = 1000;

/** Instante UTC → fecha y hora del club (America/Cancun, UTC−5 fijo) para <input type="datetime-local">. */
export function toClubInput(isoUtc: string | null): string {
  if (!isoUtc) return '';
  const d = new Date(new Date(isoUtc).getTime() - 5 * 3600e3);
  return d.toISOString().slice(0, 16);
}
/** Fecha y hora del club → ISO con desplazamiento (la API lo guarda en UTC). */
export const fromClubInput = (v: string): string | null => (v ? `${v}:00-05:00` : null);

/** «mar 1 dic · 09:00» en hora del club. */
export function clubLabel(isoUtc: string): string {
  const d = new Date(isoUtc);
  const day = new Intl.DateTimeFormat('es-MX', { timeZone: 'America/Cancun', weekday: 'short', day: 'numeric', month: 'short' }).format(d);
  const time = new Intl.DateTimeFormat('es-MX', { timeZone: 'America/Cancun', hour: '2-digit', minute: '2-digit', hour12: false }).format(d);
  return `${day.replace(/\./g, '').replace(/,/g, '')} · ${time}`;
}

export async function listPublicNotices(): Promise<PublicNotice[]> {
  const list = await apiGetAll<ApiPublic>(PUBLIC);
  return list.map((a) => ({ id: a.id, title: a.titulo?.trim() ?? '', text: a.texto, order: a.orden, startUtc: a.inicioUtc, endUtc: a.finUtc }));
}

const toDraft = (a: ApiAdmin): NoticeDraft => ({
  id: a.id,
  version: a.version,
  title: a.titulo ?? '',
  text: a.texto,
  active: a.activo,
  order: a.orden,
  start: toClubInput(a.inicioUtc),
  end: toClubInput(a.finUtc),
  state: a.estado,
  updatedUtc: a.actualizadoEnUtc,
});

export async function listAdminNotices(): Promise<NoticeDraft[]> {
  return (await apiGetAll<ApiAdmin>(ADMIN)).map(toDraft);
}

export const emptyNotice = (order: number): NoticeDraft => ({
  id: null,
  version: null,
  title: '',
  text: '',
  active: true,
  order,
  start: '',
  end: '',
  state: null,
  updatedUtc: null,
});

export function validateNotice(d: NoticeDraft): string | null {
  if (!d.text.trim()) return 'Escribe el texto del aviso.';
  if (d.text.trim().length > TEXT_MAX) return `El texto admite hasta ${TEXT_MAX} caracteres.`;
  if (d.title.trim().length > TITLE_MAX) return `El título admite hasta ${TITLE_MAX} caracteres.`;
  if (!Number.isInteger(d.order) || d.order < 0) return 'El orden debe ser un número entero de 0 en adelante.';
  if (d.start && d.end && d.end <= d.start) return '«Hasta» debe ser después de «Desde».';
  return null;
}

export async function saveNotice(d: NoticeDraft): Promise<NoticeDraft> {
  const body = {
    titulo: d.title.trim() || null,
    texto: d.text.trim(),
    activo: d.active,
    orden: d.order,
    inicioUtc: fromClubInput(d.start),
    finUtc: fromClubInput(d.end),
  };
  const saved = d.id ? await apiPut<ApiAdmin>(`${ADMIN}/${d.id}`, { ...body, version: d.version }) : await apiPost<ApiAdmin>(ADMIN, body);
  return toDraft(saved);
}

/** Estado → etiqueta del panel. */
export const STATE_TONE: Record<NoticeState, 'success' | 'info' | 'danger' | 'neutral'> = {
  Vigente: 'success',
  Programado: 'info',
  Vencido: 'danger',
  Inactivo: 'neutral',
};
export const STATE_LABEL: Record<NoticeState, string> = { Vigente: 'Visible ahora', Programado: 'Programado', Vencido: 'Vencido', Inactivo: 'Desactivado' };
