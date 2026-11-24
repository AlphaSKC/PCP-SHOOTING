import { Link } from 'react-router';
import { Photo } from '@/components/ui';
import { categoryLabel, type BlogPost } from '@/data/blog';
import { formatDate } from '@/lib/format';
import styles from './BlogCard.module.css';

/** Tarjeta de artículo: vertical en escritorio/tableta, fila con miniatura en celular (MBlog). */
export function BlogCard({ post }: { post: BlogPost }) {
  const href = `/blog/${post.slug}`;
  return (
    <article className={styles.card}>
      <div className={styles.photo}>
        <Photo src={post.imageUrl} alt="Foto del artículo" />
      </div>
      <div className={styles.body}>
        <p className={styles.meta}>
          <span className={styles.category}>{categoryLabel(post.category)}</span>
          <span aria-hidden="true">·</span>
          <time dateTime={post.date}>{formatDate(post.date)}</time>
        </p>
        <h3 className={styles.title}>
          {/* El enlace cubre toda la tarjeta (ver ::after en CSS). */}
          <Link to={href} className={styles.link}>
            {post.title}
          </Link>
        </h3>
        <p className={styles.excerpt}>{post.excerpt}</p>
        <span className={styles.more} aria-hidden="true">
          Leer más →
        </span>
      </div>
    </article>
  );
}
