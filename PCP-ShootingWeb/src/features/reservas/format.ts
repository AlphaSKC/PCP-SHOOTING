import type { Equipment } from './types';

const short = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });

/** "2027-01-04" → "Lun 4 ene" */
export function shortDay(iso: string): string {
  const s = short
    .format(new Date(iso))
    .replace(/\./g, '')
    .replace(',', '')
    .replace(' de ', ' ');
  return s.charAt(0).toUpperCase() + s.slice(1);
}

export const equipmentLabel = (e: Equipment) => (e === 'renta' ? 'Renta de rifle' : 'Rifle propio');

export const peopleLabel = (n: number, abbr = false) => (abbr ? `${n} pers.` : `${n} ${n === 1 ? 'persona' : 'personas'}`);
