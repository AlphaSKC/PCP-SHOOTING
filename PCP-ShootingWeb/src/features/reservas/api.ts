/*
 * Servicio de reservas — SIMULADO. Cada función corresponde a un endpoint propuesto (CLAUDE.md §9):
 *   getPackages        → GET  /api/paquetes
 *   getDays            → GET  /api/disponibilidad?paqueteId&fecha   (horarios por día)
 *   getLines           → GET  /api/disponibilidad?paqueteId&fecha&hora (estado de las 15 líneas)
 *   createReservation  → POST /api/reservas  → { reserva pendiente_pago, preferencia de Mercado Pago }
 *   getReservation     → GET  /api/reservas/{id}
 * La confirmación real llega por el webhook de Mercado Pago al backend; aquí se simula con `simulatePayment`.
 */

import { BOOKING_WINDOW_DAYS, OPEN_WEEKDAYS, START_TIMES, TOTAL_LINES } from './config';
import type { DayAvailability, LineAvailability, Reservation, ReservationDraft, ShootingPackage } from './types';

const delay = (ms = 350) => new Promise((r) => setTimeout(r, ms));

/** DATOS DE EJEMPLO: precios y nombres reales los entrega el cliente. */
const PACKAGES: ShootingPackage[] = [
  {
    id: 'recreativo',
    name: 'Recreativo',
    description: 'Ideal para tu primera vez. Renta de rifle PCP, diábolos y área de tiro.',
    includes: 'Incluye rifle y diábolos',
    pricePerPerson: 350,
    rentalAvailable: true,
  },
  {
    id: 'bench-rest',
    name: 'Bench Rest',
    description: 'Tiro de precisión con el rifle apoyado en mesa.',
    includes: 'Incluye blancos de precisión',
    pricePerPerson: 350,
    rentalAvailable: true,
  },
  {
    id: 'plinking',
    name: 'Plinking',
    description: 'Tiro recreativo a blancos reactivos y variados.',
    includes: 'Incluye blancos reactivos',
    pricePerPerson: 350,
    rentalAvailable: true,
  },
  {
    id: 'ipsc',
    name: 'IPSC',
    description: 'Tiro dinámico con desplazamientos y varios blancos.',
    includes: 'Incluye recorrido y blancos',
    pricePerPerson: 350,
    rentalAvailable: true,
  },
];

export async function getPackages(): Promise<ShootingPackage[]> {
  await delay(150);
  return PACKAGES;
}

/** Número pseudoaleatorio estable a partir de un texto (la misma fecha/hora da siempre el mismo resultado). */
function seeded(text: string): number {
  let h = 2166136261;
  for (let i = 0; i < text.length; i++) {
    h ^= text.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return (h >>> 0) / 4294967295;
}

const toISO = (d: Date) => d.toISOString().slice(0, 10);

/**
 * Próximos días hábiles (lunes a sábado) con sus horarios.
 * `_packageId`: la API real filtra por paquete (consumibles/capacidad); la simulación no lo usa.
 */
export async function getDays(_packageId: string): Promise<DayAvailability[]> {
  await delay();
  const days: DayAvailability[] = [];
  const cursor = new Date();
  cursor.setUTCHours(12, 0, 0, 0);
  while (days.length < BOOKING_WINDOW_DAYS) {
    cursor.setUTCDate(cursor.getUTCDate() + 1);
    if (!OPEN_WEEKDAYS.includes(cursor.getUTCDay())) continue;
    const date = toISO(cursor);
    days.push({
      date,
      slots: START_TIMES.map((time) => ({ time, full: isFull(date, time) })),
    });
  }
  return days;
}

/** Horario lleno = sin líneas libres (en la API real lo calcula el backend). */
const isFull = (date: string, time: string) => seeded(`${date}${time}`) < 0.18;

export async function getLines(date: string, time: string): Promise<LineAvailability> {
  await delay(250);
  const occupied: number[] = [];
  const full = isFull(date, time);
  for (let n = 1; n <= TOTAL_LINES; n++) {
    if (full || seeded(`${date}${time}L${n}`) < 0.28) occupied.push(n);
  }
  return { occupied };
}

/* ---------- Reservas creadas (simuladas en el navegador) ---------- */

const STORE_KEY = 'pcp.reservas.demo';

function readStore(): Record<string, Reservation> {
  try {
    return JSON.parse(localStorage.getItem(STORE_KEY) ?? '{}') as Record<string, Reservation>;
  } catch {
    return {};
  }
}

function writeStore(store: Record<string, Reservation>) {
  try {
    localStorage.setItem(STORE_KEY, JSON.stringify(store));
  } catch {
    /* sin almacenamiento: la confirmación solo vive en esta pestaña */
  }
}

const memory: Record<string, Reservation> = {};

function makeCode(): string {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let s = '';
  for (let i = 0; i < 4; i++) s += alphabet[Math.floor(Math.random() * alphabet.length)];
  return `PCP-${s}`;
}

export class ReservationError extends Error {}

/** Crea la reserva en estado pendiente_pago. La API valida disponibilidad y evita duplicados. */
export async function createReservation(draft: ReservationDraft): Promise<Reservation> {
  await delay(600);
  const pkg = PACKAGES.find((p) => p.id === draft.packageId);
  if (!pkg || !draft.date || !draft.time || !draft.line || !draft.contact) {
    throw new ReservationError('Faltan datos de la reserva.');
  }
  const { occupied } = await getLines(draft.date, draft.time);
  if (occupied.includes(draft.line)) {
    throw new ReservationError('Esa línea se acaba de ocupar. Elige otra línea u otro horario.');
  }
  const code = makeCode();
  const reservation: Reservation = {
    id: code,
    code,
    status: 'pendiente_pago',
    packageId: pkg.id,
    packageName: pkg.name,
    equipment: draft.equipment,
    people: draft.people,
    date: draft.date,
    time: draft.time,
    line: draft.line,
    total: pkg.pricePerPerson * draft.people,
    contact: draft.contact,
    createdAt: new Date().toISOString(),
  };
  memory[reservation.id] = reservation;
  writeStore({ ...readStore(), [reservation.id]: reservation });
  return reservation;
}

/** Simula que Mercado Pago aprobó el pago y el webhook confirmó la reserva. */
export async function simulatePayment(id: string): Promise<Reservation> {
  await delay(1200);
  const store = readStore();
  const current = store[id] ?? memory[id];
  if (!current) throw new ReservationError('No encontramos la reserva.');
  const confirmed: Reservation = { ...current, status: 'confirmada' };
  memory[id] = confirmed;
  writeStore({ ...store, [id]: confirmed });
  return confirmed;
}

export async function getReservation(id: string): Promise<Reservation | null> {
  await delay(200);
  return readStore()[id] ?? memory[id] ?? demoProfileReservation(id);
}

/** Las reservas de ejemplo del perfil (data/profile) también abren su QR. */
async function demoProfileReservation(id: string): Promise<Reservation | null> {
  const { myReservations } = await import('@/data/profile');
  const r = myReservations.find((x) => x.id === id);
  if (!r) return null;
  const pkg = PACKAGES.find((p) => p.name === r.activity);
  return {
    id: r.id,
    code: `PCP-${r.id.replace(/\D/g, '')}`,
    status: r.status,
    packageId: pkg?.id ?? '',
    packageName: r.activity,
    equipment: r.equipment,
    people: r.people,
    date: r.date,
    time: r.time,
    line: r.lines[0] ?? 0,
    total: (pkg?.pricePerPerson ?? 0) * r.people,
    contact: { nombre: '', email: '', telefono: '', nacimiento: '' },
    createdAt: r.date,
  };
}
