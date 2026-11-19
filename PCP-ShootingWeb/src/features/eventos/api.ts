/*
 * Eventos — SIMULADO. Endpoints propuestos (CLAUDE.md §9):
 *   getEvents  → GET  /api/eventos
 *   enroll     → POST /api/eventos/{id}/inscripciones → { inscripción pendiente_pago, preferencia de Mercado Pago }
 * DATOS DE EJEMPLO: fechas, precios y cupos reales los define el club desde el panel (Admin → Eventos).
 */

export type EventType = 'torneo' | 'clinica' | 'social';

export const EVENT_TYPES: { id: EventType | 'todos'; label: string; tag?: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'torneo', label: 'Torneos', tag: 'Torneo' },
  { id: 'clinica', label: 'Clínicas', tag: 'Clínica' },
  { id: 'social', label: 'Sociales', tag: 'Social' },
];

export type ClubEvent = {
  id: string;
  type: EventType;
  name: string;
  description: string;
  short: string;
  date: string; // ISO yyyy-mm-dd
  start: string;
  end: string;
  capacity: number;
  taken: number;
  price: number;
};

let EVENTS: ClubEvent[] = [
  {
    id: 'torneo-bench-rest',
    type: 'torneo',
    name: 'Torneo de Bench Rest',
    description: 'Competencia de precisión con rifle apoyado. Categorías por calibre.',
    short: 'Competencia de precisión con rifle apoyado.',
    date: '2027-01-23',
    start: '9:00',
    end: '14:00',
    capacity: 30,
    taken: 18,
    price: 450,
  },
  {
    id: 'clinica-ipsc',
    type: 'clinica',
    name: 'Clínica de IPSC para principiantes',
    description: 'Aprende lo básico del tiro dinámico con acompañamiento del staff.',
    short: 'Aprende lo básico del tiro dinámico.',
    date: '2027-01-30',
    start: '10:00',
    end: '12:00',
    capacity: 12,
    taken: 9,
    price: 450,
  },
  {
    id: 'tarde-plinking',
    type: 'social',
    name: 'Tarde de Plinking',
    description: 'Blancos reactivos, retos por equipos y premios para los mejores.',
    short: 'Blancos reactivos y retos por equipos.',
    date: '2027-02-06',
    start: '13:00',
    end: '16:00',
    capacity: 30,
    taken: 30,
    price: 450,
  },
  {
    id: 'copa-pcp',
    type: 'torneo',
    name: 'Copa PCP Shooting',
    description: 'Torneo abierto a socios e invitados.',
    short: 'Torneo abierto a socios e invitados.',
    date: '2027-02-20',
    start: '9:00',
    end: '16:00',
    capacity: 30,
    taken: 4,
    price: 450,
  },
];

const delay = (ms = 300) => new Promise((r) => setTimeout(r, ms));

export async function getEvents(): Promise<ClubEvent[]> {
  await delay(200);
  return EVENTS;
}

export class EnrollError extends Error {}

export type Enrollment = { folio: string; eventId: string; places: number; total: number; status: 'pagado' };

/** Inscribe y simula el pago aprobado. La API valida el cupo para evitar sobreventa. */
export async function enroll(eventId: string, places: number): Promise<Enrollment> {
  await delay(900);
  const ev = EVENTS.find((e) => e.id === eventId);
  if (!ev) throw new EnrollError('Este evento ya no está disponible.');
  if (ev.capacity - ev.taken < places) throw new EnrollError('Ya no hay lugares suficientes para este evento.');
  EVENTS = EVENTS.map((e) => (e.id === eventId ? { ...e, taken: e.taken + places } : e));
  return {
    folio: `EV-${Math.floor(Math.random() * 9000) + 1000}`,
    eventId,
    places,
    total: ev.price * places,
    status: 'pagado',
  };
}
