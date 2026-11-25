/*
 * Reservas en el panel · API real (R04.2). Administrador y Recepción.
 *   GET  /api/reservas/administracion?fecha&estado&pagina&tamanoPagina   (resumen con cliente, personas, total, estado)
 *   GET  /api/reservas/administracion/{id}                               (detalle con participantes y número de línea)
 *   POST /api/reservas/mostrador  { idOperacion, clienteId, paqueteId, fecha, hora, participantes[] }
 *        Cliente registrado y activo; sin apartado; permite turnos dentro de la anticipación web
 *        mientras no hayan comenzado. Queda PendientePago (cobro con Mercado Pago, R05).
 * Pendiente en la API (ver docs/ROADMAP.md §8): búsqueda de clientes para Recepción (F18),
 * disponibilidad para personal que ignore la anticipación web (F19), llegadas con QR (R06).
 */
import { apiGet, apiPost, type Page } from '@/lib/api';
import type { Participant, ReservationState } from '@/features/reservas/reservasApi';

export type AdminReservation = {
  id: string;
  clientId: string;
  clientName: string;
  clientPhone: string | null;
  packageName: string;
  date: string;
  time: string;
  startUtc: string;
  total: number;
  state: ReservationState;
  people: number;
};

export type AdminReservationDetail = AdminReservation & {
  packageCode: string;
  pricePerPerson: number;
  payDeadlineUtc: string;
  createdUtc: string;
  participants: { laneId: string; lane: number; name: string; birth: string; tutorName: string | null; tutorBirth: string | null }[];
};

type ApiSummary = {
  id: string;
  clienteId: string;
  nombreCliente: string;
  telefonoCliente: string | null;
  nombrePaquete: string;
  fecha: string;
  hora: string;
  inicioUtc: string;
  total: number;
  estado: ReservationState;
  cantidadParticipantes: number;
};

type ApiDetail = Omit<ApiSummary, 'cantidadParticipantes'> & {
  codigoPaquete: string;
  precioPorPersona: number;
  vencePagoEnUtc: string;
  creadoEnUtc: string;
  participantes: { lineaId: string; numeroLinea: number; nombre: string; nacimiento: string; tutorNombre: string | null; tutorNacimiento: string | null }[];
};

const toSummary = (r: ApiSummary): AdminReservation => ({
  id: r.id,
  clientId: r.clienteId,
  clientName: r.nombreCliente,
  clientPhone: r.telefonoCliente,
  packageName: r.nombrePaquete,
  date: r.fecha,
  time: r.hora.slice(0, 5),
  startUtc: r.inicioUtc,
  total: r.total,
  state: r.estado,
  people: r.cantidadParticipantes,
});

/** Todas las reservas de una fecha local del club (recorre las páginas). */
export async function listReservationsByDate(date: string): Promise<AdminReservation[]> {
  const out: AdminReservation[] = [];
  for (let pagina = 1; pagina <= 20; pagina++) {
    const page = await apiGet<Page<ApiSummary>>('/api/reservas/administracion', { fecha: date, pagina, tamanoPagina: 100 });
    out.push(...page.elementos.map(toSummary));
    if (out.length >= page.total || page.elementos.length === 0) break;
  }
  return out.sort((a, b) => a.time.localeCompare(b.time) || a.clientName.localeCompare(b.clientName, 'es'));
}

export async function getReservationDetail(id: string): Promise<AdminReservationDetail> {
  const r = await apiGet<ApiDetail>(`/api/reservas/administracion/${id}`);
  return {
    ...toSummary({ ...r, cantidadParticipantes: r.participantes.length }),
    packageCode: r.codigoPaquete,
    pricePerPerson: r.precioPorPersona,
    payDeadlineUtc: r.vencePagoEnUtc,
    createdUtc: r.creadoEnUtc,
    participants: r.participantes
      .map((p) => ({ laneId: p.lineaId, lane: p.numeroLinea, name: p.nombre, birth: p.nacimiento, tutorName: p.tutorNombre, tutorBirth: p.tutorNacimiento }))
      .sort((a, b) => a.lane - b.lane),
  };
}

/** Detalles de varias reservas (para el mapa de líneas del día), de 6 en 6. */
export async function getDetails(ids: string[]): Promise<AdminReservationDetail[]> {
  const out: AdminReservationDetail[] = [];
  for (let i = 0; i < ids.length; i += 6) {
    const chunk = await Promise.all(ids.slice(i, i + 6).map((id) => getReservationDetail(id).catch(() => null)));
    out.push(...chunk.filter((d): d is AdminReservationDetail => d !== null));
  }
  return out;
}

/** Reserva de mostrador para un cliente registrado. `operationId` se conserva en reintentos. */
export function createCounterReservation(input: {
  operationId: string;
  clientId: string;
  packageId: string;
  date: string;
  time: string;
  participants: Participant[];
}) {
  return apiPost<{ id: string; vencePagoEnUtc: string; total: number; estado: ReservationState }>('/api/reservas/mostrador', {
    idOperacion: input.operationId,
    clienteId: input.clientId,
    paqueteId: input.packageId,
    fecha: input.date,
    hora: input.time.length === 5 ? `${input.time}:00` : input.time,
    participantes: input.participants.map((p) => ({
      lineaId: p.laneId,
      nombre: p.name.trim(),
      nacimiento: p.birth,
      tutorNombre: p.tutorName.trim() || null,
      tutorNacimiento: p.tutorBirth || null,
    })),
  });
}

/** Etiqueta y tono de cada estado en el panel. */
export const STATE_VIEW: Record<ReservationState, { label: string; status: 'pendiente_pago' | 'confirmada' | 'vencida' }> = {
  PendientePago: { label: 'Pendiente de pago', status: 'pendiente_pago' },
  Pagada: { label: 'Pagada', status: 'confirmada' },
  Vencida: { label: 'Vencida', status: 'vencida' },
};
