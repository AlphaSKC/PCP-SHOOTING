/*
 * Blog en el panel · API real (solo Administrador). docs/BLOG.md
 *   GET  /api/administracion/blog          todos los estados (el estado es el efectivo según la fecha)
 *   POST /api/administracion/blog          crear
 *   PUT  /api/administracion/blog/{id}     cuerpo completo + version (409 = otra edición)
 * Estados: Borrador · Programado (publicarEnUtc futura) · Publicado · Archivado. Sin borrado físico.
 *   POST /api/administracion/blog/{id}/portada   multipart { archivo, idOperacion } → { imagenUrl, version } (F16; JPG/PNG/WebP ≤ 5 MB)
 * Portada: subir archivo (artículo ya guardado) o pegar URL HTTPS; quitar = guardar con imagenUrl null.
 * Slug: minúsculas, números y guiones, único.
 */
import { apiGetAll, apiPost, apiPut, apiUpload } from '@/lib/api';

const BASE = '/api/administracion/blog';

export type PostState = 'Borrador' | 'Programado' | 'Publicado' | 'Archivado';
export type PostCategory = 'Guias' | 'Tecnica' | 'Eventos' | 'Club';
export const CATEGORY_OPTIONS: { value: PostCategory; label: string }[] = [
  { value: 'Guias', label: 'Guías' },
  { value: 'Tecnica', label: 'Técnica' },
  { value: 'Eventos', label: 'Eventos' },
  { value: 'Club', label: 'Club' },
];
const STATES: PostState[] = ['Borrador', 'Programado', 'Publicado', 'Archivado'];
const CATS: PostCategory[] = ['Guias', 'Tecnica', 'Eventos', 'Club'];

type ApiPost = {
  id: string;
  slug: string;
  titulo: string;
  resumen: string;
  contenidoMarkdown: string;
  categoria: PostCategory | number;
  imagenUrl: string | null;
  destacado: boolean;
  estado: PostState | number;
  publicarEnUtc: string | null;
  actualizadoEnUtc: string;
  version: string;
};

export type PostDraft = {
  id: string | null;
  version: string | null;
  slug: string;
  title: string;
  summary: string;
  markdown: string;
  category: PostCategory;
  imageUrl: string;
  featured: boolean;
  state: PostState;
  publishAt: string | null;
  updatedAt: string | null;
};

export const emptyPost = (): PostDraft => ({
  id: null,
  version: null,
  slug: '',
  title: '',
  summary: '',
  markdown: '',
  category: 'Club',
  imageUrl: '',
  featured: false,
  state: 'Borrador',
  publishAt: null,
  updatedAt: null,
});

const toDraft = (p: ApiPost): PostDraft => ({
  id: p.id,
  version: p.version,
  slug: p.slug,
  title: p.titulo,
  summary: p.resumen,
  markdown: p.contenidoMarkdown,
  category: typeof p.categoria === 'number' ? CATS[p.categoria] ?? 'Club' : p.categoria,
  imageUrl: p.imagenUrl ?? '',
  featured: p.destacado,
  state: typeof p.estado === 'number' ? STATES[p.estado] ?? 'Borrador' : p.estado,
  publishAt: p.publicarEnUtc,
  updatedAt: p.actualizadoEnUtc,
});

export async function listAdminPosts(): Promise<PostDraft[]> {
  const list = await apiGetAll<ApiPost>(BASE);
  return list.map(toDraft).sort((a, b) => (b.updatedAt ?? '').localeCompare(a.updatedAt ?? ''));
}

/** Slug sugerido: "¿Qué es un rifle PCP?" → "que-es-un-rifle-pcp". */
export function suggestSlug(title: string): string {
  return title
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 160);
}
export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

/** "2026-11-25T10:00" (hora local del club) → ISO con zona. Cancún: UTC−5 fijo. */
export const clubDateTimeToIso = (local: string) => `${local}:00-05:00`;
/** ISO UTC → "2026-11-25T10:00" en hora del club (para input datetime-local). */
export function isoToClubInput(iso: string): string {
  const d = new Date(/Z$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Cancun', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false }).formatToParts(d);
  const v = (t: string) => parts.find((p) => p.type === t)?.value ?? '00';
  return `${v('year')}-${v('month')}-${v('day')}T${v('hour') === '24' ? '00' : v('hour')}:${v('minute')}`;
}

export function validatePost(d: PostDraft): string | null {
  if (!d.title.trim()) return 'Escribe el título.';
  if (!SLUG_PATTERN.test(d.slug)) return 'La dirección (slug) solo admite minúsculas, números y guiones (ej. noticias-del-club).';
  if (!d.summary.trim()) return 'Escribe el resumen (se muestra en la lista del blog).';
  if (!d.markdown.trim()) return 'Escribe el contenido del artículo.';
  if (d.imageUrl.trim() && !/^https:\/\/[^\s@]+$/i.test(d.imageUrl.trim())) return 'La portada debe ser una URL que empiece con https://';
  return null;
}

/** Guarda con el estado indicado. `publishAt` (ISO con zona) solo para Programado o para fijar la fecha de publicación. */
export async function savePost(d: PostDraft, state: PostState, publishAt: string | null): Promise<PostDraft> {
  const body = {
    slug: d.slug.trim(),
    titulo: d.title.trim(),
    resumen: d.summary.trim(),
    contenidoMarkdown: d.markdown,
    categoria: d.category,
    imagenUrl: d.imageUrl.trim() || null,
    destacado: d.featured,
    estado: state,
    publicarEnUtc: state === 'Borrador' ? null : publishAt,
  };
  const r = d.id ? await apiPut<ApiPost>(`${BASE}/${d.id}`, { ...body, version: d.version }) : await apiPost<ApiPost>(BASE, body);
  return toDraft(r);
}

export const COVER_ACCEPT = 'image/jpeg,image/png,image/webp';
export const COVER_MAX_BYTES = 5 * 1024 * 1024;

/** Sube la portada (F16). Conservar `operationId` y archivo al reintentar el mismo envío. */
export async function uploadPostCover(id: string, file: File, operationId: string): Promise<{ imageUrl: string; version: string }> {
  const form = new FormData();
  form.append('archivo', file);
  form.append('idOperacion', operationId);
  const r = await apiUpload<{ imagenUrl: string; version: string }>(`${BASE}/${id}/portada`, form);
  return { imageUrl: r.imagenUrl, version: r.version };
}
