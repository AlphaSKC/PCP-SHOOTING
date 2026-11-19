import { club } from '@/app/navigation';
import type { Reservation } from './types';

const pad = (n: number) => String(n).padStart(2, '0');

/** Hora local del club (Quintana Roo, UTC−5 todo el año) → formato iCalendar en UTC. */
function toIcsUtc(date: string, time: string, plusHours = 0): string {
  const [h, m] = time.split(':').map(Number);
  const d = new Date(`${date}T00:00:00Z`);
  d.setUTCHours((h ?? 0) + 5 + plusHours, m ?? 0);
  return `${d.getUTCFullYear()}${pad(d.getUTCMonth() + 1)}${pad(d.getUTCDate())}T${pad(d.getUTCHours())}${pad(d.getUTCMinutes())}00Z`;
}

/** Descarga un archivo .ics para agregar la reserva al calendario del teléfono o la computadora. */
export function downloadIcs(r: Reservation) {
  const ics = [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//PCP Shooting//Reservas//ES',
    'BEGIN:VEVENT',
    `UID:${r.code}@pcpshooting.com`,
    `DTSTAMP:${new Date().toISOString().replace(/[-:]/g, '').slice(0, 15)}Z`,
    `DTSTART:${toIcsUtc(r.date, r.time)}`,
    // PENDIENTE: la duración real depende del paquete; se reserva 1 h en el calendario solo como referencia.
    `DTEND:${toIcsUtc(r.date, r.time, 1)}`,
    `SUMMARY:PCP Shooting · ${r.packageName} (Línea ${r.line})`,
    `DESCRIPTION:Código de reserva ${r.code}. Presenta el QR en recepción.`,
    `LOCATION:${club.address}`,
    'END:VEVENT',
    'END:VCALENDAR',
  ].join('\r\n');

  const url = URL.createObjectURL(new Blob([ics], { type: 'text/calendar;charset=utf-8' }));
  const a = document.createElement('a');
  a.href = url;
  a.download = `reserva-${r.code}.ics`;
  a.click();
  URL.revokeObjectURL(url);
}
