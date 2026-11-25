import { Link, useParams } from 'react-router';
import { club } from '@/app/navigation';
import { absoluteUrl, seo } from '@/app/seo';
import { ButtonAnchor, ButtonLink, EmptyState, PageHero, Photo } from '@/components/ui';
import { IconArrowLeft, IconCalendar, IconPin } from '@/components/icons/icons';
import { Markdown } from '@/features/blog/Markdown';
import { eventWhen, getPublicEvent } from '@/features/eventos/eventosApi';
import { useAsync } from '@/features/reservas/useAsync';
import { LoadingScreen } from '@/pages/LoadingScreen';
import { NotFoundPage } from '@/pages/NotFoundPage';
import read from '../blog/BlogPostPage.module.css';
import styles from './EventoPage.module.css';

/** Detalle de un evento informativo (GET /api/eventos/{slug}). Sin inscripción en línea (OQ-04). */
export default function EventoPage() {
  const { slug = '' } = useParams();
  const { data: event, loading, error } = useAsync(() => getPublicEvent(slug), [slug]);

  if (loading) return <LoadingScreen />;
  if (error)
    return (
      <div className={`container ${read.main}`}>
        <EmptyState title="No pudimos cargar el evento" description="Intenta de nuevo en un momento." action={<ButtonLink to="/eventos">Ver eventos</ButtonLink>} />
      </div>
    );
  if (!event) return <NotFoundPage />;

  const finished = new Date(event.endUtc).getTime() <= Date.now();

  return (
    <>
      <title>{`${event.title} · Eventos PCP Shooting`}</title>
      <meta name="description" content={event.summary} />
      <meta property="og:type" content="article" />
      <meta property="og:title" content={event.title} />
      <meta property="og:description" content={event.summary} />
      <meta property="og:image" content={event.imageUrl || absoluteUrl(seo.image)} />
      <meta name="twitter:card" content="summary_large_image" />

      <PageHero compact eyebrow={finished ? 'Evento finalizado' : 'Evento del club'} title={event.title} lead={event.summary} />

      <div className={`container ${read.main}`}>
        <Link to={finished ? '/eventos?ver=pasados' : '/eventos'} className={read.back}>
          <IconArrowLeft size={18} /> Volver a eventos
        </Link>

        <article className={read.article}>
          <dl className={styles.facts}>
            <div>
              <dt aria-label="Fecha">
                <IconCalendar size={18} />
              </dt>
              <dd>{eventWhen(event.startUtc, event.endUtc)} (hora de Quintana Roo)</dd>
            </div>
            {event.location && (
              <div>
                <dt aria-label="Lugar">
                  <IconPin size={18} />
                </dt>
                <dd>{event.location}</dd>
              </div>
            )}
          </dl>
          {event.imageUrl && (
            <div className={read.cover}>
              <Photo src={event.imageUrl} alt={`Imagen del evento ${event.title}`} />
            </div>
          )}
          <Markdown className={read.body}>{event.markdown}</Markdown>

          {!finished && (
            <>
              <p className={styles.note}>La inscripción en línea aún no está disponible. Escríbenos para apartar tu lugar o resolver dudas.</p>
              <div className={styles.actions}>
                <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer" size="lg">
                  Preguntar por WhatsApp
                </ButtonAnchor>
                <ButtonLink to="/contacto" variant="secondary" size="lg">
                  Contacto
                </ButtonLink>
              </div>
            </>
          )}
        </article>
      </div>
    </>
  );
}
