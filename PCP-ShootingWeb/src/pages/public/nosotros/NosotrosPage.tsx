import { Link } from 'react-router';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import { club } from '@/app/navigation';
import { ButtonAnchor, ButtonLink, PageHero, Photo, SectionHeading } from '@/components/ui';
import { IconPlay, IconQuote } from '@/components/icons/icons';
import { activities, stats, testimonial } from '@/data/club';
import styles from './NosotrosPage.module.css';

export default function NosotrosPage() {
  return (
    <>
      <title>Sobre nosotros · PCP Shooting</title>
      <meta
        name="description"
        content="PCP Shooting, el primer club de tiro exclusivo para armas de aire comprimido en México. 15 líneas de tiro en la Ruta de los Cenotes, Quintana Roo."
      />

      <PageHero
        eyebrow="Sobre nosotros"
        title="El primer club de tiro exclusivo para armas de aire comprimido en México"
        lead="En la Ruta de los Cenotes, Quintana Roo, reunimos a entusiastas y principiantes del tiro deportivo y recreativo con rifles PCP, en un entorno seguro y conforme a la normativa aplicable."
        aside={<img src={logoWhite} alt="" className={styles.heroLogo} width={320} height={176} />}
        asideFirstOnMobile
        footer={
          <div className={styles.statsBar}>
            <dl className={`container ${styles.stats}`}>
              {stats.map((s) => (
                <div key={s.value} className={styles.stat}>
                  <dt className={styles.statValue}>{s.value}</dt>
                  <dd className={styles.statLabel}>
                    <span className={styles.desktopOnly}>{s.label}</span>
                    <span className={styles.mobileOnly}>{s.short}</span>
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        }
      />

      <div className={`container ${styles.main}`}>
        <section id="modos" className={styles.modes} aria-labelledby="modos-titulo">
          <SectionHeading
            id="modos-titulo"
            eyebrow="Modos y actividades"
            title="¿Cómo quieres disparar hoy?"
            action={
              <ButtonLink to="/reservar" size="lg">
                Reservar línea
              </ButtonLink>
            }
            hideActionOnMobile
          />
          <div className={styles.modeGrid}>
            {activities.map((a) => (
              <article key={a.id} className={styles.mode}>
                <div className={styles.modePhoto}>
                  <Photo src={a.imageUrl} alt={`Foto: ${a.name}`} />
                </div>
                <div className={styles.modeBody}>
                  <span className={styles.modeLevel}>{a.level}</span>
                  <h3 className={styles.modeName}>{a.name}</h3>
                  <p className={styles.modeDesc}>
                    <span className={styles.desktopOnly}>{a.description}</span>
                    <span className={styles.mobileOnly}>{a.short}</span>
                  </p>
                  <Link to={`/reservar?actividad=${a.id}`} className={styles.modeLink}>
                    Reservar {a.name} →
                  </Link>
                </div>
              </article>
            ))}
          </div>
          <ButtonLink to="/reservar" size="lg" fullWidth className={styles.mobileOnlyFlex}>
            Reservar línea
          </ButtonLink>
        </section>

        <section className={styles.story} aria-label="Video y testimonios">
          {/* TODO: reemplazar por el video real del club (YouTube/Vimeo) cuando el cliente lo entregue. */}
          <a
            href="https://www.youtube.com/@pcpshooting"
            target="_blank"
            rel="noreferrer"
            className={styles.video}
            aria-label="Ver video: Descubre PCP (abre YouTube)"
          >
            <span className={styles.play} aria-hidden="true">
              <IconPlay size={30} />
            </span>
            Video: Descubre PCP
          </a>
          <figure className={styles.quote}>
            <IconQuote size={36} className={styles.quoteIcon} />
            <blockquote>{testimonial.quote}</blockquote>
            <figcaption>— {testimonial.author}</figcaption>
          </figure>
        </section>

        <section className={styles.safety} aria-labelledby="seguridad-titulo">
          <div className={styles.safetyText}>
            <span className={styles.eyebrow}>Seguridad primero</span>
            <h2 id="seguridad-titulo" className={styles.safetyTitle}>
              Un campo seguro para todos
            </h2>
            <p>
              Todas las sesiones siguen nuestro reglamento del campo de tiro y uso de armas. Antes de disparar, cada
              tirador firma su carta responsiva en línea.
            </p>
          </div>
          <div className={styles.safetyActions}>
            <ButtonAnchor href={club.rulesHref} target="_blank" rel="noreferrer" variant="dark" size="lg">
              Leer reglamento
            </ButtonAnchor>
            <ButtonLink to="/galeria" variant="secondary" size="lg" className={styles.desktopOnlyFlex}>
              Ver galería
            </ButtonLink>
          </div>
        </section>
      </div>
    </>
  );
}
