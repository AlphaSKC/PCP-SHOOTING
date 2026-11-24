/*
 * Blog. DATOS DE EJEMPLO de los prototipos: títulos y resúmenes de muestra,
 * el contenido completo y las fechas los entrega el cliente (se publican desde el panel → Contenido).
 */

export type BlogCategory = 'guias' | 'tecnica' | 'eventos' | 'club';

export const blogCategories: { id: BlogCategory | 'todos'; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'guias', label: 'Guías' },
  { id: 'tecnica', label: 'Técnica' },
  { id: 'eventos', label: 'Eventos' },
  { id: 'club', label: 'Club' },
];

export const categoryLabel = (id: BlogCategory) => blogCategories.find((c) => c.id === id)?.label ?? id;

export type BlogPost = {
  slug: string;
  category: BlogCategory;
  title: string;
  excerpt: string;
  /** ISO yyyy-mm-dd */
  date: string;
  featured?: boolean;
  imageUrl?: string | null;
  /** Párrafos del artículo (datos de ejemplo). */
  body: string[];
  /** Contenido Markdown (API). Si existe, se usa en lugar de `body`. */
  markdown?: string;
};

const PLACEHOLDER_BODY = [
  '[CONTENIDO DEL ARTÍCULO: el cliente entrega el texto completo.]',
  'Este es un texto de ejemplo para revisar el diseño de la página del artículo: tipografía, ancho de lectura y espaciado entre párrafos.',
];

export const blogPosts: BlogPost[] = [
  {
    slug: 'que-es-un-rifle-pcp',
    category: 'guias',
    title: '¿Qué es un rifle PCP y por qué es diferente?',
    excerpt:
      'Cómo funciona el aire precargado, qué lo distingue de otros rifles de aire y por qué es la opción preferida para el tiro deportivo.',
    date: '2027-01-23',
    featured: true,
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'bench-rest-plinking-ipsc',
    category: 'guias',
    title: 'Bench Rest, Plinking e IPSC: ¿qué disciplina es para ti?',
    excerpt: 'Las tres modalidades que practicamos en el club y qué necesitas para empezar en cada una.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'tu-primera-visita',
    category: 'guias',
    title: 'Tu primera visita al campo: qué esperar',
    excerpt: 'Desde la reservación hasta tu primer disparo, paso a paso.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'como-elegir-calibre',
    category: 'tecnica',
    title: 'Cómo elegir el calibre: .22, .25 o .30',
    excerpt: 'Diferencias de precisión, alcance y uso para cada calibre.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'mantenimiento-rifle-pcp',
    category: 'tecnica',
    title: 'Mantenimiento básico de tu rifle PCP',
    excerpt: 'Cuidados sencillos para que tu rifle rinda por años.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'reglas-de-seguridad',
    category: 'club',
    title: 'Reglas de seguridad en el campo de tiro',
    excerpt: 'Lo esencial del reglamento para disfrutar con seguridad.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
  {
    slug: 'proximo-torneo',
    category: 'eventos',
    title: 'Próximo torneo en PCP Shooting',
    excerpt: 'Fechas, categorías e inscripciones abiertas.',
    date: '2027-01-23',
    body: PLACEHOLDER_BODY,
  },
];
