/*
 * Clientes · API real (solo Administrador).
 *   GET   /api/perfiles?rol=Cliente[&busqueda&activo]           lista (nombre, teléfono, activo)
 *   GET   /api/administracion/usuarios/{id}/historial            perfil + correo + pedidos + pagos + membresías + reservas (F14)
 *         ?tamanoPagina (común, máx. 100) &paginaReservas (independiente)
 *   PATCH /api/perfiles/{id}/acceso { rol, activo }               activar / desactivar la cuenta
 *   POST  /api/membresias/renovaciones                           { idOperacion, usuarioId, pagoId, inicioVigencia, finVigenciaExclusivo }
 * La membresía CARTAQ se registra con un PAGO CONFIRMADO del cliente (de un pedido suyo) que no se haya usado antes.
 * `correo` es el actual de Supabase Auth (null si la cuenta no tiene). Una reserva pagada NO significa que asistió.
 * No existen en la API: cartas responsivas ni última visita (R06).
 */
import { apiGet, apiGetAll, apiPatch, apiPost, type Page, type WriteResult } from '@/lib/api';
import { folioOf, mapOrderStatus } from '@/features/tienda/api';
import type { ReservationState } from '@/features/reservas/reservasApi';
import type { OrderStatus } from '@/features/tienda/types';

export type Client = { id: string; name: string; phone: string | null; active: boolean };

type ApiProfile = { id: string; nombre: string; telefono: string | null; rol: string; activo: boolean };

export async function listClients(): Promise<Client[]> {
  const list = await apiGetAll<ApiProfile>('/api/perfiles', { rol: 'Cliente' });
  return list.map((p) => ({ id: p.id, name: p.nombre, phone: p.telefono, active: p.activo })).sort((a, b) => a.name.localeCompare(b.name, 'es'));
}

export type Membership = { id: string; paymentId: string; from: string; untilExclusive: string };
export type ClientPayment = { id: string; orderId: string; amount: number; reference: string | null; status: number; confirmedAt: string | null };
export type ClientOrder = { id: string; folio: string; createdAt: string; status: OrderStatus; total: number };

export type ClientReservation = {
  id: string;
  /** Web (con apartado) o mostrador (sin apartado). */
  origin: 'web' | 'mostrador';
  packageName: string;
  startUtc: string;
  payDeadlineUtc: string;
  total: number;
  state: ReservationState;
  people: number;
};

export type ClientHistory = {
  profile: Client;
  /** undefined = la API desplegada aún no envía el campo; null = cuenta sin correo. */
  email: string | null | undefined;
  reservations: ClientReservation[];
  reservationsTotal: number;
  reservationsPage: number;
  orders: ClientOrder[];
  ordersTotal: number;
  payments: ClientPayment[];
  memberships: Membership[];
};

type ApiHistory = {
  perfil: ApiProfile;
  pedidos: Page<{ id: string; creadoEnUtc: string; estado: number; total: number }>;
  pagos: Page<{ id: string; pedidoId: string; importe: number; referenciaExterna: string | null; estado: number; confirmadoEnUtc: string | null }>;
  membresias: Page<{ id: string; pagoId: string; inicioVigencia: string; finVigenciaExclusivo: string }>;
  correo?: string | null;
  reservas?: Page<ApiClientReservation>;
};

type ApiClientReservation = {
  id: string;
  apartadoId: string | null;
  nombrePaquete: string;
  inicioUtc: string;
  vencePagoEnUtc: string;
  total: number;
  estado: ReservationState;
  cantidadParticipantes: number;
};

const toReservation = (r: ApiClientReservation): ClientReservation => ({
  id: r.id,
  origin: r.apartadoId ? 'web' : 'mostrador',
  packageName: r.nombrePaquete,
  startUtc: r.inicioUtc,
  payDeadlineUtc: r.vencePagoEnUtc,
  total: r.total,
  state: r.estado,
  people: r.cantidadParticipantes,
});

export const RESERVATIONS_PAGE = 50;

export async function getClientHistory(id: string, reservationsPage = 1): Promise<ClientHistory> {
  const h = await apiGet<ApiHistory>(`/api/administracion/usuarios/${id}/historial`, { tamanoPagina: RESERVATIONS_PAGE, paginaReservas: reservationsPage });
  return {
    profile: { id: h.perfil.id, name: h.perfil.nombre, phone: h.perfil.telefono, active: h.perfil.activo },
    email: h.correo,
    reservations: (h.reservas?.elementos ?? []).map(toReservation),
    reservationsTotal: h.reservas?.total ?? 0,
    reservationsPage: h.reservas?.pagina ?? reservationsPage,
    orders: h.pedidos.elementos.map((o) => ({ id: o.id, folio: folioOf(o.id), createdAt: o.creadoEnUtc, status: mapOrderStatus(o.estado), total: o.total })),
    ordersTotal: h.pedidos.total,
    payments: h.pagos.elementos.map((p) => ({ id: p.id, orderId: p.pedidoId, amount: p.importe, reference: p.referenciaExterna, status: p.estado, confirmedAt: p.confirmadoEnUtc })),
    memberships: h.membresias.elementos
      .map((m) => ({ id: m.id, paymentId: m.pagoId, from: m.inicioVigencia, untilExclusive: m.finVigenciaExclusivo }))
      .sort((a, b) => b.from.localeCompare(a.from)),
  };
}

/** Más reservas del cliente (siguiente página del historial). */
export async function getMoreReservations(id: string, page: number): Promise<{ items: ClientReservation[]; total: number }> {
  const h = await getClientHistory(id, page);
  return { items: h.reservations, total: h.reservationsTotal };
}

/** Fecha local del club (YYYY-MM-DD) de un instante UTC. */
export const clubDate = (isoUtc: string) =>
  new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Cancun', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date(isoUtc));

/** Fecha y hora del club, legible: «mar 24 nov 2026 · 11:30». */
export const clubWhen = (isoUtc: string) => {
  const d = new Date(isoUtc);
  const day = new Intl.DateTimeFormat('es-MX', { timeZone: 'America/Cancun', weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' }).format(d);
  const time = new Intl.DateTimeFormat('es-MX', { timeZone: 'America/Cancun', hour: '2-digit', minute: '2-digit', hour12: false }).format(d);
  return `${day.replace(/\./g, '').replace(/,/g, '').replace(/ de /g, ' ')} · ${time}`;
};

/** Fecha local del club (YYYY-MM-DD). */
export const todayClub = () =>
  new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Cancun', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());

/** Membresía vigente hoy (inicio ≤ hoy < fin), si existe. */
export function currentMembership(ms: Membership[]): Membership | null {
  const today = todayClub();
  return ms.find((m) => m.from <= today && today < m.untilExclusive) ?? null;
}

/** Último día de vigencia (el fin de la API es exclusivo). */
export function lastDay(untilExclusive: string): string {
  const d = new Date(`${untilExclusive}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() - 1);
  return d.toISOString().slice(0, 10);
}

/** Suma meses a una fecha YYYY-MM-DD. */
export function addMonths(date: string, months: number): string {
  const d = new Date(`${date}T12:00:00Z`);
  d.setUTCMonth(d.getUTCMonth() + months);
  return d.toISOString().slice(0, 10);
}

export const setClientActive = (id: string, active: boolean) => apiPatch(`/api/perfiles/${id}/acceso`, { rol: 'Cliente', activo: active });

export const renewMembership = (input: { operationId: string; userId: string; paymentId: string; from: string; untilExclusive: string }) =>
  apiPost<WriteResult>('/api/membresias/renovaciones', {
    idOperacion: input.operationId,
    usuarioId: input.userId,
    pagoId: input.paymentId,
    inicioVigencia: input.from,
    finVigenciaExclusivo: input.untilExclusive,
  });
