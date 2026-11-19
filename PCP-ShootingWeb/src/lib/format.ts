const mxn = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

/** 450 → "$450 MXN" · 1250.5 → "$1,250.5 MXN" (formato de los diseños). */
export function formatMXN(amount: number): string {
  // Enteros sin decimales ($450); con centavos, siempre dos ($380.50).
  const text = Number.isInteger(amount) ? mxn.format(amount) : mxn.format(amount).replace(/\.(\d)$/, '.$10');
  return `${text} MXN`;
}

const shortDate = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });

/** "2027-01-23" → "23 ene 2027". */
export function formatDate(isoDate: string): string {
  return shortDate.format(new Date(isoDate)).replace('.', '');
}

const longDay = new Intl.DateTimeFormat('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });
const monthShort = new Intl.DateTimeFormat('es-MX', { month: 'short', timeZone: 'UTC' });

/** "2027-01-09" → "Sábado 9 de enero". */
export function formatLongDay(isoDate: string): string {
  const s = longDay.format(new Date(isoDate)).replace(',', '');
  return s.charAt(0).toUpperCase() + s.slice(1);
}

/** "2027-01-09" → { month: "ENE", day: "9" } (tarjetas de fecha). */
export function dateTile(isoDate: string): { month: string; day: string } {
  const d = new Date(isoDate);
  return { month: monthShort.format(d).replace('.', '').toUpperCase(), day: String(d.getUTCDate()) };
}

/** Une clases condicionales: cx('a', cond && 'b') → "a b". */
export function cx(...classes: Array<string | false | null | undefined>): string {
  return classes.filter(Boolean).join(' ');
}
