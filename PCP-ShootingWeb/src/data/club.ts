/*
 * Contenido de las páginas informativas.
 * DATOS DE EJEMPLO tomados de los prototipos: textos, fotos y testimonios
 * los entrega el cliente (CLAUDE.md §8). Cuando exista la API o el CMS, se reemplaza esta fuente.
 */

export const stats = [
  { value: '15', label: 'líneas de tiro', short: 'líneas de tiro' },
  { value: '3', label: 'disciplinas: Bench Rest, Plinking, IPSC', short: 'disciplinas' },
  { value: '140 J', label: 'energía máxima permitida', short: 'energía máxima' },
  { value: '6 días', label: 'abiertos, de lunes a sábado', short: 'abiertos a la semana' },
];

export type Activity = {
  id: string;
  name: string;
  level: string;
  description: string;
  short: string;
  imageUrl?: string | null;
};

/** Modos y actividades. Se reservan por actividad o tirada, NO por duración (CLAUDE.md §6). */
export const activities: Activity[] = [
  {
    id: 'recreativo',
    name: 'Recreativo',
    level: 'Para principiantes',
    description: 'Renta de rifle PCP con diábolos y área de tiro. Ideal para tu primera vez.',
    short: 'Renta de rifle, diábolos y área de tiro.',
  },
  {
    id: 'bench-rest',
    name: 'Bench Rest',
    level: 'Precisión',
    description: 'Tiro de precisión con el rifle apoyado en mesa.',
    short: 'Rifle apoyado en mesa.',
  },
  {
    id: 'plinking',
    name: 'Plinking',
    level: 'Diversión',
    description: 'Blancos reactivos y retos para disfrutar en grupo.',
    short: 'Blancos reactivos en grupo.',
  },
  {
    id: 'ipsc',
    name: 'IPSC',
    level: 'Dinámico',
    description: 'Tiro dinámico con desplazamientos y varios blancos.',
    short: 'Desplazamientos y varios blancos.',
  },
];

export const testimonial = {
  quote: 'Me encantó la experiencia en el PCP, sobre todo la emoción de las ráfagas automáticas.',
  author: 'Raúl, cliente',
};
