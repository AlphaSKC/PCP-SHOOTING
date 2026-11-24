import type { StatusKey } from '@/components/ui';
import type { AdminReservation } from './seed';

const PKG: Record<string, string> = { recreativo: 'Recreativo', 'bench-rest': 'Bench Rest', plinking: 'Plinking', ipsc: 'IPSC' };
export const packageName = (id: string) => PKG[id] ?? id;

/** En el panel, "confirmada" se lee "Por llegar" y "utilizada" se lee "Llegó". */
export const reservationBadge = (r: AdminReservation): StatusKey =>
  r.status === 'utilizada' ? 'llego' : r.status === 'confirmada' ? 'por_llegar' : r.status;

const dayLong = new Intl.DateTimeFormat('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });
const dayShort = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });
const dateTime = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' });

const cap = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
export const longDay = (iso: string) => cap(dayLong.format(new Date(iso)).replace(',', ''));
export const shortDayYear = (iso: string) => cap(dayShort.format(new Date(iso)).replace(/\./g, '').replace(/,/g, '').replace(/ de /g, ' '));
export const when = (isoDateTime: string) => dateTime.format(new Date(isoDateTime)).replace('.', '');

export const timeToMinutes = (t: string) => {
  const [h, m] = t.split(':').map(Number);
  return (h ?? 0) * 60 + (m ?? 0);
};

export const money = (n: number) => `$${n.toLocaleString('es-MX')}`;
