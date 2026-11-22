import { Link } from 'react-router';
import { club } from '@/app/navigation';
import { JsonLd, businessJsonLd } from '@/app/seo';
import { ButtonAnchor, ButtonLink, CtaBanner, PageHero, ProductCard, ProductGrid, SectionHeading } from '@/components/ui';
import { IconArrowRight, IconBag, IconCalendar, IconTarget } from '@/components/icons/icons';
import { stats } from '@/data/club';
import { listPosts } from '@/features/blog/api';
import { getBrands } from '@/features/marcas/api';
import { getApiPackages, markdownItems } from '@/features/reservas/reservasApi';
import { useAsync } from '@/features/reservas/useAsync';
import { getProducts } from '@/features/tienda/api';
import { useCart } from '@/features/tienda/CartProvider';
import { DATA_SOURCE } from '@/lib/api';
import { formatMXN } from '@/lib/format';
import { BlogCard } from '@/pages/public/blog/BlogCard';
import { BrandCarousel } from './BrandCarousel';
import styles from './InicioPage.module.css';

const api = DATA_SOURCE === 'api';
const noFail = <T,>(load: () => Promise<T[]>) => () => load().catch(() => [] as T[]);

/**
 * Portada (sin diseño propio: hero de Nosotros + accesos a Reservar, Tienda y Eventos).
 * Paquetes, productos y blog salen de la API; cada bloque se oculta si aún no tiene contenido.
 */
export default function InicioPage() {
  const cart = useCart();
  const packages = useAsync(api ? noFail(getApiPackages) : null, []);
  const products = useAsync(noFail(getProducts), []);
  const posts = useAsync(noFail(listPosts), []);
  const brands = useAsync(noFail(getBrands), []);

  const pkgs = packages.data ?? [];
  const prices = pkgs.map((p) => p.precioPorPersona).filter((n): n is number => n != null);
  const fromPrice = prices.length ? Math.min(...prices) : null;
  const featured = [...(products.data ?? [])].sort((a, b) => a.featured - b.featured).slice(0, 4);
  const latest = (posts.data ?? []).slice(0, 3);

  const quick = [
    {
      to: '/reservar',
      icon: IconTarget,
      title: 'Reserva tu línea',
      text: fromPrice != null ? `Elige actividad, horario y línea. Desde ${formatMXN(fromPrice).replace(' MXN', '')} por persona.` : 'Elige actividad, horario y una de nuestras 15 líneas de tiro.',
      cta: 'Reservar',
    },
    { to: '/tienda', icon: IconBag, title: 'Tienda oficial', text: 'Playeras y artículos del club. Pagas en línea y recoges en el club.', cta: 'Ver tienda' },
    {
      to: '/eventos',
      icon: IconCalendar,
      title: 'Eventos y torneos',
      text: api ? 'Torneos, clínicas y eventos del club. Muy pronto con inscripción en línea.' : 'Torneos, clínicas y eventos sociales del club.',
      cta: 'Ver eventos',
    },
  ];

  return (
    <>
      <title>PCP Shooting · Club de tiro de aire comprimido en Quintana Roo</title>
      <meta
        name="description"
        content="El primer club de tiro exclusivo para armas de aire comprimido en México. Reserva tu línea de tiro, conoce las actividades y visítanos en la Ruta de los Cenotes."
      />
      <JsonLd data={businessJsonLd()} />

      <PageHero
        eyebrow="Club de tiro · Ruta de los Cenotes"
        title="El primer club de tiro de aire comprimido en México"
        lead="Rifles PCP para tiro deportivo y recreativo, 15 líneas de tiro y personal que te acompaña desde tu primer disparo."
        aside={
          <dl className={styles.stats}>
            {stats.map((s) => (
              <div key={s.label}>
                <dt>{s.value}</dt>
                <dd>{s.short}</dd>
              </div>
            ))}
          </dl>
        }
      >
        <div className={styles.heroActions}>
          <ButtonLink to="/reservar" size="lg">
            Reservar línea
          </ButtonLink>
          <ButtonLink to="/nosotros" size="lg" variant="ghost">
            Conoce el club
          </ButtonLink>
        </div>
      </PageHero>

      <div className={`container ${styles.main}`}>
        <section aria-labelledby="accesos" className={styles.section}>
          <SectionHeading id="accesos" eyebrow="Bienvenido" title="¿Qué quieres hacer hoy?" />
          <div className={styles.quick}>
            {quick.map(({ to, icon: Icon, title, text, cta }) => (
              <Link key={to} to={to} className={styles.quickCard}>
                <span className={styles.quickIcon} aria-hidden="true">
                  <Icon size={26} />
                </span>
                <strong className={styles.quickTitle}>{title}</strong>
                <span className={styles.quickText}>{text}</span>
                <span className={styles.quickCta}>
                  {cta} <IconArrowRight size={16} />
                </span>
              </Link>
            ))}
          </div>
        </section>

        {pkgs.length > 0 && (
          <section aria-labelledby="actividades" className={styles.section}>
            <SectionHeading id="actividades" eyebrow="Actividades" title="Elige cómo disparar" action={<ButtonLink to="/reservar" variant="secondary">Reservar</ButtonLink>} hideActionOnMobile />
            <div className={styles.packages}>
              {pkgs.slice(0, 3).map((p) => (
                <article key={p.id} className={styles.pkg}>
                  <h3 className={styles.pkgTitle}>{p.nombre}</h3>
                  {p.descripcion && <p className={styles.pkgText}>{p.descripcion}</p>}
                  <ul className={styles.pkgList}>
                    {markdownItems(p.incluyeMarkdown).slice(0, 4).map((i) => (
                      <li key={i}>{i}</li>
                    ))}
                  </ul>
                  <div className={styles.pkgFoot}>
                    <strong>{p.precioPorPersona != null ? `${formatMXN(p.precioPorPersona).replace(' MXN', '')} por persona` : 'Precio por confirmar'}</strong>
                    <ButtonLink to="/reservar" size="md">
                      Reservar
                    </ButtonLink>
                  </div>
                </article>
              ))}
            </div>
          </section>
        )}

        {featured.length > 0 && (
          <section aria-labelledby="tienda" className={styles.section}>
            <SectionHeading id="tienda" eyebrow="Tienda oficial" title="Lleva el club contigo" action={<ButtonLink to="/tienda" variant="secondary">Ver tienda</ButtonLink>} hideActionOnMobile />
            <ProductGrid>
              {featured.map((p) => (
                <ProductCard
                  key={p.id}
                  product={{ name: p.name, category: p.categoryLabel, description: p.description, price: p.price, imageUrl: p.imageUrl }}
                  added={cart.has(p.id)}
                  onAdd={() => cart.add(p)}
                />
              ))}
            </ProductGrid>
          </section>
        )}
      </div>

      {(brands.data?.length ?? 0) > 0 && <BrandCarousel brands={brands.data!} />}

      <div className={`container ${styles.main}`}>
        {latest.length > 0 && (
          <section aria-labelledby="blog" className={styles.section}>
            <SectionHeading id="blog" eyebrow="Blog PCP" title="Guías y noticias del club" action={<ButtonLink to="/blog" variant="secondary">Ver blog</ButtonLink>} hideActionOnMobile />
            <div className={styles.posts}>
              {latest.map((p) => (
                <BlogCard key={p.slug} post={p} />
              ))}
            </div>
          </section>
        )}

        <CtaBanner
          title="Visítanos"
          text={`${club.address} · ${club.hours}`}
          textMobile={club.hours}
          actions={
            <>
              <ButtonLink to="/reservar" size="lg">
                Reservar línea
              </ButtonLink>
              <ButtonAnchor href={club.mapsHref} target="_blank" rel="noreferrer" size="lg" variant="ghost">
                Cómo llegar
              </ButtonAnchor>
            </>
          }
        />
      </div>
    </>
  );
}
