/*
 * Datos del perfil del cliente. DATOS DE EJEMPLO (de los prototipos).
 * Vendrán de la API: GET /api/reservas/mias, GET /api/pedidos/mios, inscripciones a eventos y carta responsiva.
 */
import type { StatusKey } from '@/components/ui';

export type MyReservation = {
  id: string;
  activity: string;
  people: number;
  /** ISO yyyy-mm-dd */
  date: string;
  time: string;
  lines: number[];
  equipment: 'propio' | 'renta';
  status: Extract<StatusKey, 'pendiente_pago' | 'confirmada' | 'utilizada' | 'cancelada'>;
};

export const myReservations: MyReservation[] = [
  { id: 'R-1042', activity: 'Bench Rest', people: 2, date: '2027-01-09', time: '11:00', lines: [5], equipment: 'propio', status: 'confirmada' },
  { id: 'R-1043', activity: 'Recreativo', people: 4, date: '2027-01-16', time: '10:00', lines: [9, 10], equipment: 'renta', status: 'confirmada' },
];

export const myEvents = [{ id: 'E-1', name: 'Torneo de Bench Rest', date: '2027-01-23', time: '9:00' }];

export const myOrders: { id: string; items: string; status: StatusKey }[] = [
  { id: '0142', items: 'Playera PCP Negra, Gorra PCP Blanca', status: 'listo_para_recoger' },
];

/** Carta responsiva: el mecanismo legal está pendiente (CLAUDE.md §8). */
export const myWaiver = { status: 'firmada' as StatusKey, signedAt: '2027-01-23' };
