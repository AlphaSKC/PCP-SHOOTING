/* Galería. DATOS DE EJEMPLO: las fotos y videos se suben desde el panel (Contenido). */

export type GalleryCategory = 'campo' | 'torneos' | 'eventos' | 'armas';

export const galleryCategories: { id: GalleryCategory | 'todo'; label: string }[] = [
  { id: 'todo', label: 'Todo' },
  { id: 'campo', label: 'Campo' },
  { id: 'torneos', label: 'Torneos' },
  { id: 'eventos', label: 'Eventos' },
  { id: 'armas', label: 'Armas' },
];

export type GalleryItem = {
  id: string;
  category: GalleryCategory;
  title: string;
  kind: 'foto' | 'video';
  /** Ocupa el doble de espacio en el mosaico. */
  featured: boolean;
  imageUrl?: string | null;
};

export const galleryItems: GalleryItem[] = [
  { id: 'g1', category: 'campo', title: 'Líneas de tiro', kind: 'foto', featured: true },
  { id: 'g2', category: 'campo', title: 'Zona de blancos', kind: 'foto', featured: false },
  { id: 'g3', category: 'torneos', title: 'Torneo Bench Rest', kind: 'foto', featured: false },
  { id: 'g4', category: 'eventos', title: 'Cumpleaños en grupo', kind: 'foto', featured: false },
  { id: 'g5', category: 'armas', title: 'Sidewinder', kind: 'foto', featured: true },
  { id: 'g6', category: 'campo', title: 'Entrada del club', kind: 'foto', featured: false },
  { id: 'g7', category: 'torneos', title: 'Premiación', kind: 'foto', featured: false },
  { id: 'g8', category: 'armas', title: 'Mordax Venom', kind: 'foto', featured: false },
  { id: 'g9', category: 'eventos', title: 'Tarde de Plinking', kind: 'video', featured: false },
];
