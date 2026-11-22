import { useCallback, useEffect, useRef, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, EmptyState, FormAlert, Photo, SelectField, StatusBadge, Switch, TextareaField, TextField, type BadgeTone } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import {
  CATEGORY_OPTIONS,
  clubDateTimeToIso,
  emptyPost,
  isoToClubInput,
  listAdminPosts,
  savePost,
  suggestSlug,
  validatePost,
  type PostDraft,
  type PostState,
} from '@/features/admin/blogApi';
import { when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, Panel, SegmentedTabs, Section, Split } from '@/features/admin/ui';
import { Markdown } from '@/features/blog/Markdown';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import { AdminMarcasApi } from './AdminMarcasApi';
import styles from './AdminShared.module.css';

type Tab = 'blog' | 'marcas' | 'galeria' | 'textos' | 'avisos';
type Msg = { tone: 'error' | 'success'; text: string } | null;
const STATE_TONE: Record<PostState, BadgeTone> = { Borrador: 'neutral', Programado: 'info', Publicado: 'success', Archivado: 'neutral' };
const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError ? (e.status === 409 && /versi/i.test(e.message) ? 'Alguien más editó este artículo. Recargamos los datos: revisa y vuelve a guardar.' : e.message) : fallback;

const PENDING: Record<Exclude<Tab, 'blog' | 'marcas'>, { title: string; text: string }> = {
  galeria: { title: 'Galería: pendiente', text: 'La galería aún no tiene API (pospuesta por acuerdo del equipo). Cuando exista, aquí se subirán y ordenarán las fotos.' },
  textos: { title: 'Páginas y textos: pendiente', text: 'Los textos institucionales (Nosotros, testimonios) aún no tienen API. Por ahora se cambian en el código del sitio.' },
  avisos: { title: 'Avisos del sitio: pendiente', text: 'Los avisos (cierres, horarios especiales) aún no tienen API. Cuando exista, se activarán desde aquí.' },
};

function BlogEditor() {
  const [posts, setPosts] = useState<PostDraft[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [post, setPost] = useState<PostDraft | null>(null);
  const [slugTouched, setSlugTouched] = useState(true);
  const [preview, setPreview] = useState(false);
  const [scheduleAt, setScheduleAt] = useState('');
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState<PostState | null>(null);
  const bodyRef = useRef<HTMLTextAreaElement>(null);

  const load = useCallback(async () => {
    try {
      const list = await listAdminPosts();
      setPosts(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar el blog.'));
      setStatus('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((list) => {
      if (list?.[0]) setPost((p) => p ?? list[0]!);
    });
  }, [load]);

  function open(p: PostDraft, isNew = false) {
    setPost(p);
    setSlugTouched(!isNew);
    setPreview(false);
    setScheduleAt(p.state === 'Programado' && p.publishAt ? isoToClubInput(p.publishAt) : '');
    setMsg(null);
    revealEditor();
  }

  // Barra de formato (Markdown): envuelve la selección.
  const wrap = (before: string, after = before) => {
    const el = bodyRef.current;
    if (!el || !post) return;
    const { selectionStart: s, selectionEnd: e, value } = el;
    setPost({ ...post, markdown: value.slice(0, s) + before + value.slice(s, e) + after + value.slice(e) });
  };

  async function save(state: PostState) {
    if (!post) return;
    const problem = validatePost(post);
    if (problem) return setMsg({ tone: 'error', text: problem });
    let publishAt: string | null = null;
    if (state === 'Programado') {
      if (!scheduleAt) return setMsg({ tone: 'error', text: 'Elige la fecha y hora de publicación.' });
      publishAt = clubDateTimeToIso(scheduleAt);
      if (new Date(publishAt).getTime() <= Date.now()) return setMsg({ tone: 'error', text: 'La fecha programada debe ser futura.' });
    }
    if (state === 'Archivado' && !window.confirm('¿Archivar el artículo? Dejará de verse en el sitio (no se borra).')) return;
    setSaving(state);
    setMsg(null);
    try {
      const saved = await savePost(post, state, publishAt);
      await load();
      setPost(saved);
      setSlugTouched(true);
      setMsg({
        tone: 'success',
        text:
          state === 'Publicado'
            ? 'Artículo publicado. Ya se ve en el blog.'
            : state === 'Programado'
              ? `Programado para el ${when(publishAt!)}.`
              : state === 'Archivado'
                ? 'Artículo archivado.'
                : 'Borrador guardado (no se ve en el sitio).',
      });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        const list = await load();
        const fresh = list?.find((p) => p.id === post.id);
        if (fresh) setPost(fresh);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el artículo.') });
    } finally {
      setSaving(null);
    }
  }

  if (status === 'error') return <EmptyState title="No pudimos cargar el blog" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;

  const isPublic = post?.state === 'Publicado';
  return (
    <Split
      asideWidth={480}
      main={
        <Section title="Artículos del blog" actions={<Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyPost(), true)}>Nuevo</Button>}>
          {status === 'loading' ? (
            <p className={styles.muted}>Cargando artículos…</p>
          ) : posts.length === 0 ? (
            <p className={styles.muted}>Aún no hay artículos. Crea el primero con «Nuevo».</p>
          ) : (
            <ul className={styles.list}>
              {posts.map((p) => (
                <li key={p.id}>
                  <button type="button" className={cx(styles.itemCard, p.id === post?.id && styles.itemSelected)} onClick={() => open(p)}>
                    <span className={styles.thumb}><Photo src={p.imageUrl || undefined} alt={p.title} /></span>
                    <span className={styles.itemBody}>
                      <span className={styles.itemHead}>
                        <strong>{p.title}</strong>
                        <StatusBadge tone={STATE_TONE[p.state]}>{p.state}{p.featured ? ' · Destacado' : ''}</StatusBadge>
                      </span>
                      <span className={styles.itemMeta}>
                        {CATEGORY_OPTIONS.find((c) => c.value === p.category)?.label} · {p.publishAt ? when(p.publishAt) : 'Sin publicar'}
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
        post && (
          <Panel title={post.id ? 'Editar artículo' : 'Nuevo artículo'} id="editor" actions={post.id ? <StatusBadge tone={STATE_TONE[post.state]}>{post.state}</StatusBadge> : undefined}>
            {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
            <TextField
              label="Título"
              maxLength={200}
              value={post.title}
              onChange={(e) => {
                const title = e.target.value;
                setPost({ ...post, title, slug: slugTouched ? post.slug : suggestSlug(title) });
              }}
            />
            <TextField
              label="Dirección (slug)"
              hint={`Se verá en /blog/${post.slug || 'mi-articulo'}. Cambiarla rompe enlaces ya compartidos.`}
              maxLength={160}
              value={post.slug}
              onChange={(e) => { setSlugTouched(true); setPost({ ...post, slug: e.target.value.toLowerCase() }); }}
            />
            <TextareaField label="Resumen" hint="Aparece en la lista del blog." rows={2} maxLength={600} value={post.summary} onChange={(e) => setPost({ ...post, summary: e.target.value })} />
            <div className={styles.grid2}>
              <SelectField label="Categoría" value={post.category} onChange={(e) => setPost({ ...post, category: e.target.value as PostDraft['category'] })} options={CATEGORY_OPTIONS} />
              <TextField label="Portada (URL https)" placeholder="https://…" maxLength={2048} value={post.imageUrl} onChange={(e) => setPost({ ...post, imageUrl: e.target.value })} />
            </div>
            {post.imageUrl.startsWith('https://') && (
              <div className={styles.photos}>
                <div className={styles.photo}><Photo src={post.imageUrl} alt="Portada" /></div>
              </div>
            )}
            <Switch label="Destacado (arriba del blog)" showLabel checked={post.featured} onChange={(featured) => setPost({ ...post, featured })} />

            <div className={styles.actions} role="toolbar" aria-label="Formato">
              <Button variant="secondary" disabled={preview} onClick={() => wrap('**')} aria-label="Negritas">B</Button>
              <Button variant="secondary" disabled={preview} onClick={() => wrap('_')} aria-label="Cursivas">I</Button>
              <Button variant="secondary" disabled={preview} onClick={() => wrap('\n## ', '')} aria-label="Subtítulo">H2</Button>
              <Button variant="secondary" disabled={preview} onClick={() => wrap('\n- ', '')} aria-label="Lista">Lista</Button>
              <Button variant="secondary" disabled={preview} onClick={() => wrap('[', '](https://)')} aria-label="Enlace">Link</Button>
              <Button variant="dark" onClick={() => setPreview((v) => !v)}>{preview ? 'Editar' : 'Vista previa'}</Button>
            </div>
            {preview ? (
              <div className={styles.box}>
                <Markdown>{post.markdown || '_Sin contenido._'}</Markdown>
              </div>
            ) : (
              <TextareaField ref={bodyRef} label="Contenido (Markdown)" rows={12} maxLength={100000} value={post.markdown} onChange={(e) => setPost({ ...post, markdown: e.target.value })} />
            )}

            <TextField
              label="Programar publicación (hora del club)"
              hint="Opcional. Déjalo vacío para publicar ahora."
              type="datetime-local"
              value={scheduleAt}
              onChange={(e) => setScheduleAt(e.target.value)}
            />
            <div className={styles.actions}>
              {scheduleAt ? (
                <Button size="lg" loading={saving === 'Programado'} disabled={saving !== null} onClick={() => void save('Programado')}>
                  Programar
                </Button>
              ) : (
                <Button size="lg" loading={saving === 'Publicado'} disabled={saving !== null} onClick={() => void save('Publicado')}>
                  {isPublic ? 'Guardar cambios' : 'Publicar ahora'}
                </Button>
              )}
              <Button size="lg" variant="secondary" loading={saving === 'Borrador'} disabled={saving !== null} onClick={() => void save('Borrador')}>
                {isPublic ? 'Despublicar (borrador)' : 'Guardar borrador'}
              </Button>
            </div>
            {post.id && post.state !== 'Archivado' && (
              <Button variant="text" loading={saving === 'Archivado'} disabled={saving !== null} onClick={() => void save('Archivado')}>
                Archivar artículo
              </Button>
            )}
            {isPublic && (
              <ButtonLink to={`/blog/${post.slug}`} variant="secondary" target="_blank">
                Ver en el sitio
              </ButtonLink>
            )}
            <p className={styles.muted}>La portada es una URL (la API aún no sube imágenes del blog).</p>
          </Panel>
        )
      }
    />
  );
}

/** Contenido con la API: el blog es real; galería, textos y avisos aún no tienen API. */
export default function AdminContenidoApi() {
  const [tab, setTab] = useState<Tab>('blog');
  return (
    <>
      <title>Contenido · Panel PCP</title>
      <AdminPageHeader title="Contenido" subtitle="Blog y marcas de la portada (galería, textos y avisos: pendientes de API)" actions={<ButtonLink to="/blog" variant="secondary">Ver blog</ButtonLink>} />
      <AdminBody>
        <SegmentedTabs
          label="Tipo de contenido"
          value={tab}
          onChange={setTab}
          options={[
            { value: 'blog', label: 'Blog' },
            { value: 'marcas', label: 'Marcas' },
            { value: 'galeria', label: 'Galería · pendiente' },
            { value: 'textos', label: 'Textos · pendiente' },
            { value: 'avisos', label: 'Avisos · pendiente' },
          ]}
        />
        {tab === 'blog' ? <BlogEditor /> : tab === 'marcas' ? <AdminMarcasApi /> : <EmptyState title={PENDING[tab].title} description={PENDING[tab].text} />}
      </AdminBody>
    </>
  );
}
