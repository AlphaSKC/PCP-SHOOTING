export type Equipment = 'renta' | 'propio';

/** Paquete o actividad de tiro. Se cobra por actividad/tirada, NO por duración (CLAUDE.md §6). */
export type ShootingPackage = {
  id: string;
  name: string;
  description: string;
  /** Lo que incluye el paquete (se muestra en lugar de la duración). */
  includes: string;
  pricePerPerson: number;
  /** Si se puede rentar rifle con este paquete. */
  rentalAvailable: boolean;
};

/** `label`: motivo cuando no está disponible ("Lleno", "No disponible"…). Por defecto "Lleno". */
export type TimeSlot = { time: string; full: boolean; label?: string };

export type DayAvailability = {
  date: string; // ISO yyyy-mm-dd
  slots: TimeSlot[];
};

/** Líneas ocupadas para una fecha y hora. */
export type LineAvailability = { occupied: number[] };

export type ContactData = {
  nombre: string;
  email: string;
  telefono: string;
  nacimiento: string; // ISO yyyy-mm-dd
};

/** Lo que el cliente va eligiendo en el wizard. */
export type ReservationDraft = {
  packageId: string;
  equipment: Equipment;
  people: number;
  date: string | null;
  time: string | null;
  line: number | null;
  contact: ContactData | null;
  waiver: { accepted: boolean; signature: string | null } | null;
};

/** Estados de la reserva (CLAUDE.md §6). */
export type ReservationStatus = 'pendiente_pago' | 'confirmada' | 'utilizada' | 'cancelada';

export type Reservation = {
  id: string;
  /** Código corto que se muestra y se codifica en el QR. */
  code: string;
  status: ReservationStatus;
  packageId: string;
  packageName: string;
  equipment: Equipment;
  people: number;
  date: string;
  time: string;
  line: number;
  total: number;
  contact: ContactData;
  createdAt: string;
};
