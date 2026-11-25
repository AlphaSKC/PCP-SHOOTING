import { useCallback, useEffect, useMemo, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, Chip, ChipGroup, EmptyState, FormAlert, Photo, StatusBadge, TextareaField, TextField, type BadgeTone } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, DemoNote, Panel, Section, Split } from '@/features/admin/ui';
import { Markdown } from '@/features/blog/Markdown';
import {
  LIMITS,
  emptyEvent,
  eventWhen,
  fromClubInput,
  listAdminEvents,
  saveEvent,
  suggestSlug,
  validateEvent,
  type EventDraft,
  type EventState,
} from '@/features/eventos/eventosApi';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import styles from './AdminShared.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
type Filter = 'todos' | 'proximos' | 'pasados' | EventState;
const STATE_TONE: Record<EventState, BadgeTone> = { Borrador: 'neutral', Publicado: 'success', Archivado: 'danger' };
const FILTERS: { id: Filter; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'proximos', label: 'Próximos' },
  { id: 'pasados', label: 'Pasados' },
  { id: 'Publicado', label: 'Publicados' },
  { id: 'Borrador', label: 'Borradores' },
  { id: 'Archivado', label: 'Archivados' },
];

const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError
    ? e.status === 409
      ? 'No se guardó: la dirección (slug) ya la usa otro evento, o alguien más editó este evento. Recargamos los datos: revisa y vuelve a guardar.'
      : e.message
    : fallback;

const isPast = (d: EventDraft) => !!d.endUtc && new Date(d.endUtc).getTime() <= Date.now();

/** Eventos informativos (docs/EVENTOS.md). Solo Administrador. Sin inscripciones, cupo ni precio (OQ-04). */
export default function AdminEventosApi() {
  const [events, setEvents] = useState<EventDraft[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<Filter>('todos');
  const [q, setQ] = useState('');
  const [ev, setEv] = useState<EventDraft | null>(null);
  const [slugTouched, setSlugTouched] = useState(true);
  const [preview, setPreview] = useState(false);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState<EventState | null>(null);

  const load = useCallback(async () => {
    try {
      const list = await listAdminEvents();
      setEvents(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los eventos.'));
      setStatus('error');
      return null;
    }
  }, []);
  useEffect(() => void load(), [load]);

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase();
    return events.filter((e) => {
      const byFilter =
        filter === 'todos' ? true : filter === 'proximos' ? !isPast(e) : filter === 'pasados' ? isPast(e) : e.state === filter;
      return byFilter && (!t || `${e.title} ${e.summary}`.toLowerCase().includes(t));
    });
  }, [events, filter, q]);

  function open(e: EventDraft, isNew = false) {
    setEv(e);
    setSlugTouched(!isNew);
    setPreview(false);
    setMsg(null);
    revealEditor();
  }

  async function save(state: EventState) {
    if (!ev) return;
    const problem = validateEvent(ev);
    if (problem) return setMsg({ tone: 'error', text: problem });
    if (state === 'Archivado' && !window.confirm('¿Archivar el evento? Dejará de verse en el sitio (no se borra ni cancela nada).')) return;
    setSaving(state);
    setMsg(null);
    try {
      const saved = await saveEvent(ev, state);
      await load();
      setEv(saved);
      setSlugTouched(true);
      setMsg({
        tone: 'success',
        text: state === 'Publicado' ? 'Evento publicado. Ya se ve en /eventos.' : state === 'Archivado' ? 'Evento archivado (ya no se ve en el sitio).' : 'Borrador guardado (no se ve en el sitio).',
      });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409 && ev.id) {
        const list = await load();
        const fresh = list?.find((x) => x.id === ev.id);
        if (fresh) setEv(fresh);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el evento.') });
    } finally {
      setSaving(null);
    }
  }

  const isPublic = ev?.state === 'Publicado';

  return (
    <>
      <title>Eventos · Panel PCP</title>
      <AdminPageHeader
        title="Eventos"
        subtitle="Torneos, clínicas y eventos del club (informativos)"
        actions={
          <Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyEvent(), true)}>
            Nuevo evento
          </Button>
        }
      />
      <AdminBody>
        <DemoNote>
          Por ahora los eventos son informativos: fecha, lugar y descripción. Inscripciones, cupo, precio y pago siguen pendientes de definir con el cliente (OQ-04). Los eventos no apartan líneas de tiro: para eso usa los bloqueos en Configuración.
        </DemoNote>
        {status === 'error' ? (
          <EmptyState title="No pudimos cargar los eventos" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <Split
            asideWidth={480}
            main={
              <Section title="Eventos">
                <div className={styles.toolbar}>
                  <ChipGroup label="Filtrar eventos">
                    {FILTERS.map((f) => (
                      <Chip key={f.id} selected={filter === f.id} onClick={() => setFilter(f.id)}>
                        {f.label}
                      </Chip>
                    ))}
                  </ChipGroup>
                  <TextField label="Buscar" placeholder="Título o resumen" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
                </div>
                {status === 'loading' ? (
                  <p className={styles.muted}>Cargando eventos…</p>
                ) : visible.length === 0 ? (
                  <p className={styles.muted}>{events.length === 0 ? 'Aún no hay eventos. Crea el primero con «Nuevo evento».' : 'No hay eventos con este filtro.'}</p>
                ) : (
                  <ul className={styles.list}>
                    {visible.map((e) => (
                      <li key={e.id}>
                        <button type="button" className={cx(styles.itemCard, e.id === ev?.id && styles.itemSelected)} onClick={() => open(e)}>
                          <span className={styles.thumb}>
                            <Photo src={e.imageUrl || undefined} alt={e.title} />
                          </span>
                          <span className={styles.itemBody}>
                            <span className={styles.itemHead}>
                              <strong className={styles.wrapAny}>{e.title}</strong>
                              <StatusBadge tone={STATE_TONE[e.state]}>{e.state}</StatusBadge>
                            </span>
                            <span className={styles.itemMeta}>
                              {e.startUtc && e.endUtc ? eventWhen(e.startUtc, e.endUtc) : '—'}
                              {isPast(e) ? ' · Finalizado' : ''}
                            </span>
                          </span>
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </Section>
            }
            aside={
              ev ? (
                <Panel title={ev.id ? 'Editar evento' : 'Nuevo evento'} id="editor" actions={ev.id ? <StatusBadge tone={STATE_TONE[ev.state]}>{ev.state}</StatusBadge> : undefined}>
                  {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                  <TextField
                    label="Título"
                    maxLength={LIMITS.title}
                    value={ev.title}
                    onChange={(e) => {
                      const title = e.target.value;
                      setEv({ ...ev, title, slug: slugTouched ? ev.slug : suggestSlug(title) });
                    }}
                  />
                  <TextField
                    label="Dirección (slug)"
                    hint={`Se verá en /eventos/${ev.slug || 'mi-evento'}. Cambiarla rompe enlaces ya compartidos.`}
                    maxLength={LIMITS.slug}
                    value={ev.slug}
                    onChange={(e) => {
                      setSlugTouched(true);
                      setEv({ ...ev, slug: e.target.value.toLowerCase() });
                    }}
                  />
                  <TextareaField label="Resumen" hint="Aparece en la lista de eventos." rows={2} maxLength={LIMITS.summary} value={ev.summary} onChange={(e) => setEv({ ...ev, summary: e.target.value })} />
                  <TextField label="Inicio (hora del club)" type="datetime-local" value={ev.start} max={ev.end || undefined} onChange={(e) => setEv({ ...ev, start: e.target.value })} />
                  <TextField label="Fin (hora del club)" type="datetime-local" value={ev.end} min={ev.start || undefined} onChange={(e) => setEv({ ...ev, end: e.target.value })} />
                  {ev.start && ev.end && ev.end > ev.start && <p className={styles.muted}>{eventWhen(fromClubInput(ev.start), fromClubInput(ev.end))}</p>}
                  <TextField label="Lugar (opcional)" maxLength={LIMITS.location} value={ev.location} onChange={(e) => setEv({ ...ev, location: e.target.value })} />
                  <TextField
                    label="Imagen (URL https, opcional)"
                    placeholder="https://…"
                    hint="La API de eventos aún no sube archivos: pega el enlace de una imagen publicada."
                    maxLength={LIMITS.image}
                    value={ev.imageUrl}
                    onChange={(e) => setEv({ ...ev, imageUrl: e.target.value })}
                  />
                  {ev.imageUrl.startsWith('https://') && (
                    <div className={styles.photos}>
                      <div className={styles.photo}>
                        <Photo src={ev.imageUrl} alt="Imagen del evento" />
                      </div>
                    </div>
                  )}

                  <div className={styles.actions}>
                    <Button variant="dark" onClick={() => setPreview((v) => !v)}>
                      {preview ? 'Editar descripción' : 'Vista previa'}
                    </Button>
                  </div>
                  {preview ? (
                    <div className={styles.box}>
                      <Markdown>{ev.markdown || '_Sin descripción._'}</Markdown>
                    </div>
                  ) : (
                    <TextareaField
                      label="Descripción (Markdown)"
                      hint="Programa, requisitos, qué incluye… Usa ## para subtítulos y - para listas."
                      rows={10}
                      maxLength={LIMITS.markdown}
                      value={ev.markdown}
                      onChange={(e) => setEv({ ...ev, markdown: e.target.value })}
                    />
                  )}

                  <div className={styles.actions}>
                    <Button size="lg" loading={saving === 'Publicado'} disabled={saving !== null} onClick={() => void save('Publicado')}>
                      {isPublic ? 'Guardar cambios' : 'Publicar'}
                    </Button>
                    <Button size="lg" variant="secondary" loading={saving === 'Borrador'} disabled={saving !== null} onClick={() => void save('Borrador')}>
                      {isPublic ? 'Despublicar (borrador)' : 'Guardar borrador'}
                    </Button>
                  </div>
                  {ev.id && ev.state !== 'Archivado' && (
                    <Button variant="text" loading={saving === 'Archivado'} disabled={saving !== null} onClick={() => void save('Archivado')}>
                      Archivar evento
                    </Button>
                  )}
                  {isPublic && (
                    <ButtonLink to={`/eventos/${ev.slug}`} variant="secondary" target="_blank">
                      Ver en el sitio
                    </ButtonLink>
                  )}
                  {ev.updatedUtc && <p className={styles.muted}>Última actualización: {when(ev.updatedUtc)}</p>}
                </Panel>
              ) : (
                <Panel title="Evento" id="editor">
                  <p className={styles.muted}>Elige un evento de la lista o crea uno nuevo.</p>
                </Panel>
              )
            }
          />
        )}
      </AdminBody>
    </>
  );
}
