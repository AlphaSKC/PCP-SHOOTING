import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { Button, ButtonLink, Chip, ChipGroup, EmptyState, PageHero, Pagination, Photo, TextField } from '@/components/ui';
import { IconArrowRight, IconSearch } from '@/components/icons/icons';
import { NewsletterCard } from '@/features/contacto/NewsletterCard';
import { blogCategories, categoryLabel, type BlogCategory } from '@/data/blog';
import { listPosts } from '@/features/blog/api';
import { useAsync } from '@/features/reservas/useAsync';
import { formatDate } from '@/lib/format';
import { BlogCard } from './BlogCard';
import styles from './BlogPage.module.css';

const PAGE_SIZE = 6;

/** Quita acentos y mayúsculas para buscar "tecnica" = "Técnica". */
const normalize = (s: string) =>
  s
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase();

export default function BlogPage() {
  // Filtros en la URL: se pueden compartir y el botón "atrás" funciona.
  const [params, setParams] = useSearchParams();
  const category = (params.get('categoria') as BlogCategory | null) ?? 'todos';
  const query = params.get('q') ?? '';
  const page = Math.max(1, Number(params.get('pagina')) || 1);

  const update = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(params);
    for (const [k, v] of Object.entries(changes)) {
      if (v === null || v === '') next.delete(k);
      else next.set(k, v);
    }
    setParams(next, { replace: true, preventScrollReset: true });
  };

  const [attempt, setAttempt] = useState(0);
  const posts = useAsync(listPosts, [attempt]);
  const blogPosts = useMemo(() => posts.data ?? [], [posts.data]);

  const featured = blogPosts.find((p) => p.featured);
  const showFeatured = Boolean(featured) && category === 'todos' && !query && page === 1;

  const filtered = useMemo(() => {
    const q = normalize(query.trim());
    return blogPosts.filter((p) => {
      if (showFeatured && p === featured) return false;
      if (category !== 'todos' && p.category !== category) return false;
      if (q && !normalize(`${p.title} ${p.excerpt}`).includes(q)) return false;
      return true;
    });
  }, [blogPosts, category, query, showFeatured, featured]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const current = Math.min(page, totalPages);
  const visible = filtered.slice((current - 1) * PAGE_SIZE, current * PAGE_SIZE);

  return (
    <>
      <title>Blog · PCP Shooting</title>
      <meta
        name="description"
        content="Guías, técnica y vida de club: todo sobre el tiro con aire comprimido, desde tu primera visita hasta tu primer torneo."
      />

      <PageHero
        tone="light"
        eyebrow="Blog PCP"
        title="Guías, técnica y vida de club"
        lead="Todo lo que necesitas saber sobre el tiro con aire comprimido, desde tu primera visita hasta tu primer torneo."
        hideLeadOnMobile
        aside={
          <form role="search" className={styles.search} onSubmit={(e) => e.preventDefault()}>
            <TextField
              label="Buscar artículos"
              type="search"
              placeholder="Ej. calibre, mantenimiento…"
              defaultValue={query}
              onChange={(e) => update({ q: e.target.value, pagina: null })}
            />
          </form>
        }
      />

      <div className={`container ${styles.main}`}>
        <ChipGroup label="Categorías del blog">
          {blogCategories.map((c) => (
            <Chip
              key={c.id}
              selected={category === c.id}
              onClick={() => update({ categoria: c.id === 'todos' ? null : c.id, pagina: null })}
            >
              {c.label}
            </Chip>
          ))}
        </ChipGroup>

        {showFeatured && featured && (
          <article className={styles.featured} aria-labelledby="destacado-titulo">
            <div className={styles.featuredPhoto}>
              <Photo src={featured.imageUrl} alt="Foto destacada del artículo" className={styles.darkPlaceholder} />
            </div>
            <div className={styles.featuredBody}>
              <p className={styles.featuredMeta}>
                <span className={styles.badge}>Destacado · {categoryLabel(featured.category)}</span>
                <time dateTime={featured.date}>{formatDate(featured.date)}</time>
              </p>
              <h2 id="destacado-titulo" className={styles.featuredTitle}>
                {featured.title}
              </h2>
              <p className={styles.featuredExcerpt}>{featured.excerpt}</p>
              <ButtonLink
                to={`/blog/${featured.slug}`}
                size="lg"
                iconEnd={<IconArrowRight size={18} />}
                className={styles.featuredButton}
              >
                Leer artículo
              </ButtonLink>
            </div>
          </article>
        )}

        {posts.error ? (
          <EmptyState
            title="No pudimos cargar el blog"
            description="Revisa tu conexión e intenta de nuevo."
            action={<Button onClick={() => setAttempt((n) => n + 1)}>Reintentar</Button>}
          />
        ) : posts.loading && !posts.data ? (
          <EmptyState title="Cargando artículos…" />
        ) : blogPosts.length === 0 ? (
          <EmptyState title="Pronto publicaremos artículos" description="Estamos preparando guías y noticias del club." />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={IconSearch}
            title="Sin artículos"
            description="No encontramos artículos con esa búsqueda o categoría."
          />
        ) : (
          <div className={styles.grid}>
            {visible.map((post) => (
              <BlogCard key={post.slug} post={post} />
            ))}
          </div>
        )}

        <Pagination
          page={current}
          totalPages={totalPages}
          label="Páginas del blog"
          onChange={(p) => {
            update({ pagina: p === 1 ? null : String(p) });
            window.scrollTo({ top: 0, behavior: 'smooth' });
          }}
        />

        <NewsletterCard />
      </div>
    </>
  );
}
