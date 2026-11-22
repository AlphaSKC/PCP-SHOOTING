/*
 * SEO en el navegador. Los datos viven en seo.json (los usa también scripts/prerender-seo.mjs al compilar).
 * - <RouteSeo/> (en PublicLayout): canonical, Open Graph/Twitter y noindex según la ruta.
 * - Las páginas siguen poniendo su <title> y <meta name="description"> (React 19 los sube al <head>).
 * - Rutas con contenido propio (artículos del blog) pasan `dynamic` y ponen sus propias etiquetas.
 */
import { useLocation } from 'react-router';
import seo from './seo.json';

export const SITE_URL = (import.meta.env.VITE_SITE_URL || seo.defaultUrl).replace(/\/$/, '');
export const absoluteUrl = (path: string) => (/^https?:\/\//.test(path) ? path : `${SITE_URL}${path.startsWith('/') ? path : `/${path}`}`);

type RouteInfo = (typeof seo.routes)[number];
const byPath = new Map<string, RouteInfo>(seo.routes.map((r) => [r.path, r]));

export const isNoindex = (path: string) => seo.noindex.some((p) => path === p || path.startsWith(`${p}/`));

/** Canonical, Open Graph y robots de la ruta actual. */
export function RouteSeo() {
  const { pathname } = useLocation();
  const path = pathname !== '/' ? pathname.replace(/\/$/, '') : '/';
  const info = byPath.get(path);
  const dynamic = path.startsWith('/blog/');

  if (isNoindex(path)) return <meta name="robots" content="noindex, nofollow" />;

  return (
    <>
      <link rel="canonical" href={absoluteUrl(path)} />
      <meta property="og:url" content={absoluteUrl(path)} />
      <meta property="og:site_name" content={seo.siteName} />
      <meta property="og:locale" content={seo.locale} />
      {!dynamic && (
        <>
          <meta property="og:type" content="website" />
          <meta property="og:title" content={info?.title ?? seo.siteName} />
          <meta property="og:description" content={info?.description ?? seo.business.description} />
          <meta property="og:image" content={absoluteUrl(seo.image)} />
          <meta property="og:image:alt" content={seo.imageAlt} />
          <meta name="twitter:card" content="summary_large_image" />
        </>
      )}
    </>
  );
}

/** Datos estructurados (JSON-LD). El contenido se serializa; "<" se escapa para no cerrar el <script>. */
export function JsonLd({ data }: { data: object }) {
  return <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(data).replace(/</g, '\\u003c') }} />;
}

/** Negocio local (portada): nombre, dirección, horario y contacto. */
export function businessJsonLd() {
  const b = seo.business;
  return {
    '@context': 'https://schema.org',
    '@type': 'SportsActivityLocation',
    name: b.name,
    description: b.description,
    url: SITE_URL,
    image: absoluteUrl(seo.image),
    logo: absoluteUrl('/favicon.png'),
    telephone: b.telephone,
    email: b.email,
    address: { '@type': 'PostalAddress', streetAddress: b.streetAddress, addressRegion: b.addressRegion, addressCountry: b.addressCountry },
    openingHoursSpecification: [{ '@type': 'OpeningHoursSpecification', dayOfWeek: b.openingDays, opens: b.opens, closes: b.closes }],
    ...(b.sameAs.length ? { sameAs: b.sameAs } : {}),
  };
}

/** Artículo del blog. */
export function articleJsonLd(post: { slug: string; title: string; excerpt: string; date: string; imageUrl?: string | null }) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BlogPosting',
    headline: post.title,
    description: post.excerpt,
    datePublished: post.date,
    image: post.imageUrl ? [post.imageUrl] : [absoluteUrl(seo.image)],
    mainEntityOfPage: absoluteUrl(`/blog/${post.slug}`),
    publisher: { '@type': 'Organization', name: seo.siteName, logo: { '@type': 'ImageObject', url: absoluteUrl('/favicon.png') } },
  };
}

export { seo };
