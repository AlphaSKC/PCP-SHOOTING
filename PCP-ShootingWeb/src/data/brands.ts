/*
 * "Marcas que nos acompañan" (portada). Contenido del club, sin API por ahora.
 * Nombres tomados del sitio actual del club. Para mostrar el logo:
 *   1. Guarda el archivo en PCP-ShootingWeb/public/marcas/ (PNG o SVG con fondo transparente, ~400×200).
 *   2. Pon la ruta en `logo` (ej. '/marcas/altaros.png').
 * Sin logo se muestra el nombre como texto. `url` es opcional (sitio de la marca).
 */
export type Brand = { name: string; tagline?: string; logo?: string | null; url?: string | null };

export const brands: Brand[] = [
  { name: 'Airguns FV', tagline: 'Rifles de aire', logo: null },
  { name: 'Altaros', tagline: 'Equipo de tiro', logo: null },
  { name: 'ZAN Projectiles', tagline: 'Proyectiles', logo: null },
  { name: 'Donny FL', tagline: 'Supresores', logo: null },
  { name: 'AEA', tagline: 'Rifles PCP', logo: null },
  { name: 'H&V', tagline: 'Equipo de tiro', logo: null },
];
