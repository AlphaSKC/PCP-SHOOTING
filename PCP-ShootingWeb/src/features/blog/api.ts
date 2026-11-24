/*
 * Blog público. Con VITE_DATA_SOURCE=api:
 *   GET /api/blog            artículos visibles (publicados y programados cuya fecha ya llegó), sin contenido
 *   GET /api/blog/{slug}     detalle con contenido Markdown (404 si no es público)
 * Categorías API: Guias, Tecnica, Eventos, Club.
 */
import { blogPosts, type BlogCategory, type BlogPost } from '@/data/blog';
import { ApiError, DATA_SOURCE, apiGet, apiGetAll } from '@/lib/api';

export const API_CATEGORY: Record<string, BlogCategory> = { Guias: 'guias', Tecnica: 'tecnica', Eventos: 'eventos', Club: 'club' };
const toCategory = (c: string | number): BlogCategory =>
  typeof c === 'number' ? (['guias', 'tecnica', 'eventos', 'club'] as const)[c] ?? 'club' : API_CATEGORY[c] ?? 'club';

type ApiSummary = {
  id: string;
  slug: string;
  titulo: string;
  resumen: string;
  categoria: string | number;
  imagenUrl: string | null;
  destacado: boolean;
  publicarEnUtc: string | null;
};
type ApiDetail = ApiSummary & { contenidoMarkdown: string };

const toPost = (a: ApiSummary): BlogPost => ({
  slug: a.slug,
  category: toCategory(a.categoria),
  title: a.titulo,
  excerpt: a.resumen,
  date: a.publicarEnUtc ?? new Date().toISOString(),
  featured: a.destacado,
  imageUrl: a.imagenUrl,
  body: [],
});

/** Todos los artículos públicos, más recientes primero. */
export async function listPosts(): Promise<BlogPost[]> {
  if (DATA_SOURCE !== 'api') return blogPosts;
  const list = await apiGetAll<ApiSummary>('/api/blog');
  return list.map(toPost).sort((a, b) => b.date.localeCompare(a.date));
}

/** Artículo público por slug; null si no existe o no es público. */
export async function getPost(slug: string): Promise<BlogPost | null> {
  if (DATA_SOURCE !== 'api') return blogPosts.find((p) => p.slug === slug) ?? null;
  try {
    const a = await apiGet<ApiDetail>(`/api/blog/${encodeURIComponent(slug)}`);
    return { ...toPost(a), markdown: a.contenidoMarkdown };
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}
