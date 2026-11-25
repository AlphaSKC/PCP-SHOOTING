import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { club } from '@/app/navigation';
import { Button, ButtonAnchor, ButtonLink, Chip, ChipGroup, EmptyState, PageHero, Photo } from '@/components/ui';
import { IconClock, IconPin, IconUsers } from '@/components/icons/icons';
import { EVENT_TYPES, getEvents, type ClubEvent, type EventType } from '@/features/eventos/api';
import { EnrollDialog } from '@/features/eventos/EnrollDialog';
import { useAsync } from '@/features/reservas/useAsync';
import { cx, dateTile, formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './EventosPage.module.css';
import { DATA_SOURCE } from '@/lib/api';
import { dateTileClub, eventTime, listPublicEvents, type PublicEvent } from '@/features/eventos/eventosApi';

const weekday = new Intl.DateTimeFormat('es-MX', { weekday: 'long', timeZone: 'UTC' });
const cap = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);

/** Horario del club (CLAUDE.md §1). PENDIENTE: "último turno" depende de los intervalos por definir. */
const SCHEDULE = [
  ['Martes a domingo', '8:00 – 18:00'],
  ['Lunes', 'Cerrado'],
  ['Último turno', '15:30'],
];

function Schedule({ className }: { className?: string }) {
  return (
    <section className={cx(styles.schedule, className)} aria-labelledby={`horario-${className}`}>
      <h2 id={`horario-${className}`} className={styles.scheduleTitle}>
        <IconClock size={18} className={styles.scheduleIcon} /> Horario del club
      </h2>
      <dl>
        {SCHEDULE.map(([k, v]) => (
          <div key={k}>
            <dt>{k}</dt>
            <dd>{v}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function EventRow({ event, onEnroll }: { event: ClubEvent; onEnroll: () => void }) {
  const tile = dateTile(event.date);
  const left = event.capacity - event.taken;
  const full = left <= 0;
  const tag = EVENT_TYPES.find((t) => t.id === event.type)?.tag;

  return (
    <article className={styles.event} aria-labelledby={`ev-${event.id}`}>
      <div className={styles.date} aria-hidden="true">
        <span className={styles.month}>{tile.month}</span>
        <strong className={styles.day}>{tile.day}</strong>
        <span className={styles.weekday}>{cap(weekday.format(new Date(event.date)))}</span>
      </div>
      <div className={styles.body}>
        <span className={styles.tag}>{tag}</span>
        <h3 id={`ev-${event.id}`} className={styles.name}>
          {event.name}
        </h3>
        <p className={styles.desc}>{event.description}</p>
        <p className={styles.meta}>
          <span>
            <IconClock size={16} /> {event.start} – {event.end}
          </span>
          <span>
            <IconUsers size={16} /> {full ? 'Cupo lleno' : `Quedan ${left} de ${event.capacity} lugares`}
          </span>
        </p>
      </div>
      <p className={styles.descMobile}>{event.short}</p>
      <div className={styles.side}>
        <span className={styles.priceLabel}>Inscripción</span>
        <strong className={styles.price}>{formatMXN(event.price).replace(' MXN', '')}</strong>
        <Button size="lg" variant={full ? 'secondary' : 'primary'} onClick={onEnroll} disabled={full} className={styles.enroll}>
          {full ? 'Lleno' : 'Inscribirme'}
        </Button>
      </div>
    </article>
  );
}

/** Con la API: eventos informativos (sin inscripción en línea todavía, OQ-04). */
export default function EventosPage() {
  if (DATA_SOURCE === 'api') return <EventosApiPage />;
  return <EventosPageDemo />;
}

function ApiEventRow({ event, past }: { event: PublicEvent; past: boolean }) {
  const tile = dateTileClub(event.startUtc);
  const live = !past && new Date(event.startUtc).getTime() <= Date.now();
  return (
    <article className={styles.event} aria-labelledby={`ev-${event.id}`}>
      <div className={styles.date} aria-hidden="true">
        <span className={styles.month}>{tile.month}</span>
        <strong className={styles.day}>{tile.day}</strong>
        <span className={styles.weekday}>{tile.weekday}</span>
      </div>
      <div className={styles.body}>
        <span className={styles.tag}>{past ? 'Finalizado' : live ? 'En curso' : 'Próximo'}</span>
        <h3 id={`ev-${event.id}`} className={styles.name}>
          <Link to={`/eventos/${event.slug}`} className={styles.nameLink}>
            {event.title}
          </Link>
        </h3>
        <p className={styles.desc}>{event.summary}</p>
        <p className={styles.meta}>
          <span>
            <IconClock size={16} /> {eventTime(event.startUtc, event.endUtc)}
          </span>
          {event.location && (
            <span>
              <IconPin size={16} /> {event.location}
            </span>
          )}
        </p>
      </div>
      <p className={styles.descMobile}>{event.summary}</p>
      <div className={styles.side}>
        <ButtonLink to={`/eventos/${event.slug}`} size="lg" variant={past ? 'secondary' : 'primary'} className={styles.enroll}>
          Ver detalles
        </ButtonLink>
      </div>
    </article>
  );
}

function EventosApiPage() {
  const [params, setParams] = useSearchParams();
  const past = params.get('ver') === 'pasados';
  const { data: events, loading, error } = useAsync(() => listPublicEvents(!past), [past]);

  return (
    <>
      <title>Eventos · PCP Shooting</title>
      <meta name="description" content="Torneos, clínicas y eventos del club de tiro PCP Shooting en Quintana Roo." />

      <PageHero
        eyebrow="Eventos y horarios"
        title="Torneos, clínicas y eventos del club"
        lead="Consulta fechas y detalles. Para apartar tu lugar escríbenos por WhatsApp."
        aside={<Schedule className={styles.heroSchedule} />}
        hideAsideOnMobile
      />

      <div className={`container ${styles.main}`}>
        <Schedule className={styles.mobileSchedule} />

        <section className={styles.list} aria-labelledby="proximos">
          <div className={styles.listHead}>
            <h2 id="proximos" className={styles.listTitle}>
              {past ? 'Eventos pasados' : 'Próximos eventos'}
            </h2>
            <ChipGroup label="Mostrar">
              <Chip selected={!past} onClick={() => setParams({}, { replace: true, preventScrollReset: true })}>
                Próximos
              </Chip>
              <Chip selected={past} onClick={() => setParams({ ver: 'pasados' }, { replace: true, preventScrollReset: true })}>
                Pasados
              </Chip>
            </ChipGroup>
          </div>

          {loading && !events ? (
            <LoadingScreen />
          ) : error ? (
            <EmptyState title="No pudimos cargar los eventos" description="Intenta de nuevo en un momento o pregúntanos por WhatsApp." />
          ) : !events || events.length === 0 ? (
            <EmptyState
              title={past ? 'Aún no hay eventos pasados' : 'No hay eventos próximos por ahora'}
              description={past ? 'Aquí verás los eventos que ya se realizaron.' : 'Vuelve pronto o síguenos en redes para enterarte de los siguientes.'}
              action={
                <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer" variant="secondary">
                  Preguntar por WhatsApp
                </ButtonAnchor>
              }
            />
          ) : (
            <div className={styles.events}>
              {events.map((e) => (
                <ApiEventRow key={e.id} event={e} past={past} />
              ))}
            </div>
          )}
        </section>

        <PrivateEvents />
      </div>
    </>
  );
}

function EventosPageDemo() {
  const [refresh, setRefresh] = useState(0);
  const { data: events, loading } = useAsync(getEvents, [refresh]);
  const [params, setParams] = useSearchParams();
  const type = (params.get('tipo') as EventType | null) ?? 'todos';
  const [selected, setSelected] = useState<ClubEvent | null>(null);

  const visible = useMemo(
    () => (events ?? []).filter((e) => type === 'todos' || e.type === type),
    [events, type],
  );

  return (
    <>
      <title>Eventos y horarios · PCP Shooting</title>
      <meta
        name="description"
        content="Torneos, clínicas y noches de tiro en PCP Shooting. Inscríbete en línea, aparta tu lugar y paga desde tu cuenta."
      />

      <PageHero
        eyebrow="Eventos y horarios"
        title="Torneos, clínicas y noches de tiro"
        lead="Inscríbete en línea, aparta tu lugar y paga desde tu cuenta."
        aside={<Schedule className={styles.heroSchedule} />}
        hideAsideOnMobile
      />

      <div className={`container ${styles.main}`}>
        <Schedule className={styles.mobileSchedule} />

        <section className={styles.list} aria-labelledby="proximos">
          <div className={styles.listHead}>
            <h2 id="proximos" className={styles.listTitle}>
              Próximos eventos
            </h2>
            <ChipGroup label="Tipo de evento">
              {EVENT_TYPES.map((t) => (
                <Chip
                  key={t.id}
                  selected={type === t.id}
                  onClick={() => {
                    const next = new URLSearchParams(params);
                    if (t.id === 'todos') next.delete('tipo');
                    else next.set('tipo', t.id);
                    setParams(next, { replace: true, preventScrollReset: true });
                  }}
                >
                  {t.label}
                </Chip>
              ))}
            </ChipGroup>
          </div>

          {loading && !events ? (
            <LoadingScreen />
          ) : visible.length === 0 ? (
            <EmptyState title="No hay eventos de este tipo por ahora" description="Vuelve pronto o síguenos en redes." />
          ) : (
            <div className={styles.events}>
              {visible.map((e) => (
                <EventRow key={e.id} event={e} onEnroll={() => setSelected(e)} />
              ))}
            </div>
          )}
        </section>

        <PrivateEvents />
      </div>

      <EnrollDialog event={selected} onClose={() => setSelected(null)} onEnrolled={() => setRefresh((n) => n + 1)} />
    </>
  );
}

/** Eventos privados (cotización por contacto o WhatsApp). */
function PrivateEvents() {
  return (
  <section className={styles.private} aria-labelledby="privados">
    <div className={styles.privateText}>
      <span className={styles.privateEyebrow}>Eventos privados</span>
      <h2 id="privados" className={styles.privateTitle}>
        Cumpleaños, empresas y grupos
      </h2>
      <p>
        <span className={styles.desktopInline}>
          Aparta varias líneas de tiro para tu grupo. Te armamos un paquete a la medida.
        </span>
        <span className={styles.mobileInline}>Aparta varias líneas para tu grupo.</span>
      </p>
      <div className={styles.privateActions}>
        <ButtonLink to="/contacto?motivo=grupos" size="lg">
          Cotizar mi evento
        </ButtonLink>
        <ButtonAnchor href={club.whatsappHref} target="_blank" rel="noreferrer" variant="ghost" size="lg" className={styles.desktopFlex}>
          WhatsApp
        </ButtonAnchor>
      </div>
    </div>
    <div className={styles.privatePhoto}>
      <Photo alt="Foto: grupo en el campo de tiro" className={styles.darkPlaceholder} />
    </div>
  </section>
  );
}
