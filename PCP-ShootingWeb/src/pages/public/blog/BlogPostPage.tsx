import { Link, useParams } from 'react-router';
import { PageHero, Photo, SectionHeading } from '@/components/ui';
import { IconArrowLeft } from '@/components/icons/icons';
import { NewsletterCard } from '@/features/contacto/NewsletterCard';
import { blogPosts, categoryLabel } from '@/data/blog';
import { formatDate } from '@/lib/format';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { BlogCard } from './BlogCard';
import styles from './BlogPostPage.module.css';

/** Artículo del blog. No hay diseño propio: sigue el sistema de Blog (hero negro + lectura en columna). */
export default function BlogPostPage() {
  const { slug } = useParams();
  const post = blogPosts.find((p) => p.slug === slug);
  if (!post) return <NotFoundPage />;

  const related = blogPosts
    .filter((p) => p.slug !== post.slug)
    .sort((a, b) => Number(b.category === post.category) - Number(a.category === post.category))
    .slice(0, 3);

  return (
    <>
      <title>{`${post.title} · Blog PCP Shooting`}</title>
      <meta name="description" content={post.excerpt} />

      <PageHero
        compact
        eyebrow={`${categoryLabel(post.category)} · ${formatDate(post.date)}`}
        title={post.title}
        lead={post.excerpt}
      />

      <div className={`container ${styles.main}`}>
        <Link to="/blog" className={styles.back}>
          <IconArrowLeft size={18} /> Volver al blog
        </Link>

        <article className={styles.article}>
          <div className={styles.cover}>
            <Photo src={post.imageUrl} alt="Foto del artículo" />
          </div>
          <div className={styles.body}>
            {post.body.map((paragraph, i) => (
              <p key={i}>{paragraph}</p>
            ))}
          </div>
        </article>

        <section className={styles.related} aria-labelledby="relacionados">
          <SectionHeading id="relacionados" title="Sigue leyendo" />
          <div className={styles.grid}>
            {related.map((p) => (
              <BlogCard key={p.slug} post={p} />
            ))}
          </div>
        </section>

        <NewsletterCard />
      </div>
    </>
  );
}
