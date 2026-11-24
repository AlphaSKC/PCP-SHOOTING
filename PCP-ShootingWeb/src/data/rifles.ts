/*
 * Catálogo de armas (informativo, sin venta en línea).
 * DATOS DE EJEMPLO de los prototipos. Precios: pendientes del cliente → null = "Por cotizar".
 * Se administra desde el panel (Productos → pestaña Catálogo de armas).
 */

export type Rifle = {
  id: string;
  name: string;
  type: string;
  description: string;
  /** Descripción corta para celular. */
  short: string;
  calibers: string;
  specs: { label: string; value: string }[];
  firingMode: string | null;
  highlight: string;
  price: number | null;
  imageUrl?: string | null;
};

export const rifles: Rifle[] = [
  {
    id: 'mordax-venom',
    name: 'Mordax Venom',
    type: 'Compacto',
    description: 'Rifle PCP compacto y manejable, ideal para quien busca precisión en un formato ligero.',
    short: 'Rifle PCP compacto y manejable, ideal para precisión en un formato ligero.',
    calibers: '.22 · .25 · .30',
    specs: [{ label: 'Formato', value: 'Compacto' }],
    firingMode: null,
    highlight: 'Formato compacto',
    price: null,
  },
  {
    id: 'sidewinder',
    name: 'Sidewinder',
    type: 'Semiautomático y automático',
    description: 'Fuego semiautomático y automático, regulador ajustable y alta capacidad de aire para sesiones largas.',
    short: 'Fuego semi y automático, regulador ajustable y alta capacidad de aire.',
    calibers: '.22 · .25 · .30',
    specs: [
      { label: 'Modo de disparo', value: 'Semi y automático' },
      { label: 'Regulador', value: 'Ajustable' },
      { label: 'Capacidad de aire', value: 'Alta' },
    ],
    firingMode: 'Semi y automático',
    highlight: 'Regulador ajustable, alta capacidad',
    price: null,
  },
  {
    id: 'aea-sf-standard',
    name: 'AEA SF Standard',
    type: 'Semiautomático',
    description: 'Rifle PCP semiautomático, una opción versátil para tiro recreativo y deportivo.',
    short: 'Rifle PCP semiautomático, versátil para tiro recreativo y deportivo.',
    calibers: '.22 · .25 · .30',
    specs: [{ label: 'Modo de disparo', value: 'Semiautomático' }],
    firingMode: 'Semiautomático',
    highlight: 'Versatilidad',
    price: null,
  },
];
