/*
 * Eventos informativos · API real. Contrato: docs/EVENTOS.md
 *   GET  /api/eventos?proximos=true|false&pagina&tamanoPagina      (público) solo publicados; sin Markdown
 *   GET  /api/eventos/{slug}                                       (público) detalle; 404 si borrador/archivado
 *   GET  /api/administracion/eventos?estado&busqueda&proximos       (Administrador) todos los estados
 *   POST /api/administracion/eventos                                crear
 *   PUT  /api/administracion/eventos/{id}                           documento completo + version (409 = slug repetido o versión vieja)
 * Solo información: SIN inscripciones, cupo, precio ni pago (OQ-04). No ocupa líneas. Imagen solo por URL HTTPS.
 * Fechas: se capturan en hora del club (UTC−5) y la API las guarda en UTC.
 */
import { ApiError, apiGet, apiGetAll, apiPost, apiPut } from '@/lib/api';

export type EventState = 'Borrador' | 'Publicado' | 'Archivado';

export type PublicEvent = {
  id: string;
  slug: string;
  title: string;
  summary: string;
  location: string | null;
  imageUrl: string | null;
  startUtc: string;
  endUtc: string;
};
export type PublicEventDetail = PublicEvent & { markdown: string };

export type EventDraft = {
  id: string | null;
  version: string | null;
  slug: string;
  title: string;
  summary: string;
  markdown: string;
  location: string;
  imageUrl: string;
  /** Hora del club, AAAA-MM-DDTHH:mm. */
  start: string;
  end: string;
  state: EventState;
  updatedUtc: string | null;
  startUtc: string | null;
  endUtc: string | null;
};

type ApiSummary = { id: string; slug: string; titulo: string; resumen: string; ubicacion: string | null; imagenUrl: string | null; inicioUtc: string; finUtc: string };
type ApiDetail = ApiSummary & { descripcionMarkdown: string };
type ApiAdmin = ApiDetail & { estado: EventState; version: string; creadoEnUtc: string; actualizadoEnUtc: string };

const PUBLIC = '/api/eventos';
const ADMIN = '/api/administracion/eventos';

export const LIMITS = { slug: 160, title: 200, summary: 600, markdown: 30000, location: 250, image: 2048 };
export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const toPublic = (e: ApiSummary): PublicEvent => ({
  id: e.id,
  slug: e.slug,
  title: e.titulo,
  summary: e.resumen,
  location: e.ubicacion,
  imageUrl: e.imagenUrl,
  startUtc: e.inicioUtc,
  endUtc: e.finUtc,
});

/** Próximos (incluye en curso) o pasados. Próximos: del más cercano al más lejano; pasados: del más reciente al más viejo. */
export async function listPublicEvents(upcoming: boolean): Promise<PublicEvent[]> {
  const list = (await apiGetAll<ApiSummary>(PUBLIC, { proximos: upcoming })).map(toPublic);
  return upcoming ? list : list.reverse();
}

/** Detalle público; null si no existe o no está publicado. */
export async function getPublicEvent(slug: string): Promise<PublicEventDetail | null> {
  try {
    const e = await apiGet<ApiDetail>(`${PUBLIC}/${encodeURIComponent(slug)}`);
    return { ...toPublic(e), markdown: e.descripcionMarkdown };
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}

/* ---------- Hora del club ---------- */

/** Instante UTC → AAAA-MM-DDTHH:mm del club. */
export const toClubInput = (isoUtc: string) => new Date(new Date(isoUtc).getTime() - 5 * 3600e3).toISOString().slice(0, 16);
export const fromClubInput = (v: string) => `${v}:00-05:00`;

const clubFmt = (opts: Intl.DateTimeFormatOptions) => new Intl.DateTimeFormat('es-MX', { timeZone: 'America/Cancun', ...opts });
const dayFmt = clubFmt({ weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
const timeFmt = clubFmt({ hour: '2-digit', minute: '2-digit', hour12: false });
const monthFmt = clubFmt({ month: 'short' });
const dayNum = clubFmt({ day: 'numeric' });
const weekdayFmt = clubFmt({ weekday: 'long' });
const cap = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);

/** Bloque de fecha de la tarjeta (mes, día, día de la semana) en hora del club. */
export function dateTileClub(isoUtc: string) {
  const d = new Date(isoUtc);
  return { month: monthFmt.format(d).replace('.', '').toUpperCase(), day: dayNum.format(d), weekday: cap(weekdayFmt.format(d)) };
}

/** «Sábado 10 de enero de 2027 · 10:00 – 12:00» (o con dos fechas si termina otro día). */
export function eventWhen(startUtc: string, endUtc: string): string {
  const s = new Date(startUtc);
  const e = new Date(endUtc);
  const sameDay = toClubInput(startUtc).slice(0, 10) === toClubInput(endUtc).slice(0, 10);
  return sameDay
    ? `${cap(dayFmt.format(s))} · ${timeFmt.format(s)} – ${timeFmt.format(e)}`
    : `${cap(dayFmt.format(s))}, ${timeFmt.format(s)} – ${cap(dayFmt.format(e))}, ${timeFmt.format(e)}`;
}
export const eventTime = (startUtc: string, endUtc: string) => `${timeFmt.format(new Date(startUtc))} – ${timeFmt.format(new Date(endUtc))}`;

/* ---------- Administración ---------- */

const toDraft = (e: ApiAdmin): EventDraft => ({
  id: e.id,
  version: e.version,
  slug: e.slug,
  title: e.titulo,
  summary: e.resumen,
  markdown: e.descripcionMarkdown,
  location: e.ubicacion ?? '',
  imageUrl: e.imagenUrl ?? '',
  start: toClubInput(e.inicioUtc),
  end: toClubInput(e.finUtc),
  state: e.estado,
  updatedUtc: e.actualizadoEnUtc,
  startUtc: e.inicioUtc,
  endUtc: e.finUtc,
});

export async function listAdminEvents(): Promise<EventDraft[]> {
  return (await apiGetAll<ApiAdmin>(ADMIN)).map(toDraft).sort((a, b) => (b.startUtc ?? '').localeCompare(a.startUtc ?? ''));
}

export const emptyEvent = (): EventDraft => ({
  id: null,
  version: null,
  slug: '',
  title: '',
  summary: '',
  markdown: '',
  location: 'PCP Shooting',
  imageUrl: '',
  start: '',
  end: '',
  state: 'Borrador',
  updatedUtc: null,
  startUtc: null,
  endUtc: null,
});

export function suggestSlug(title: string): string {
  return title
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, LIMITS.slug);
}

export function validateEvent(d: EventDraft): string | null {
  if (!d.title.trim()) return 'Escribe el título.';
  if (!SLUG_PATTERN.test(d.slug)) return 'La dirección (slug) solo admite minúsculas, números y guiones (ej. torneo-de-enero).';
  if (!d.summary.trim()) return 'Escribe el resumen (se ve en la lista de eventos).';
  if (!d.markdown.trim()) return 'Escribe la descripción del evento.';
  if (!d.start || !d.end) return 'Indica el inicio y el fin del evento.';
  if (d.end <= d.start) return 'El fin debe ser después del inicio.';
  if (d.imageUrl.trim() && !/^https:\/\/[^\s@]+$/i.test(d.imageUrl.trim())) return 'La imagen debe ser una URL que empiece con https://';
  return null;
}

/** Guarda el documento completo con el estado indicado. */
export async function saveEvent(d: EventDraft, state: EventState): Promise<EventDraft> {
  const body = {
    slug: d.slug.trim(),
    titulo: d.title.trim(),
    resumen: d.summary.trim(),
    descripcionMarkdown: d.markdown,
    ubicacion: d.location.trim() || null,
    imagenUrl: d.imageUrl.trim() || null,
    inicioUtc: fromClubInput(d.start),
    finUtc: fromClubInput(d.end),
    estado: state,
  };
  const r = d.id ? await apiPut<ApiAdmin>(`${ADMIN}/${d.id}`, { ...body, version: d.version }) : await apiPost<ApiAdmin>(ADMIN, body);
  return toDraft(r);
}
