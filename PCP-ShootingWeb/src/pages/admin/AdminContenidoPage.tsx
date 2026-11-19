import { useRef, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, FormAlert, Photo, SelectField, StatusBadge, Switch, TextareaField, TextField } from '@/components/ui';
import { IconClose, IconPlus } from '@/components/icons/icons';
import { blogCategories } from '@/data/blog';
import { galleryCategories } from '@/data/gallery';
import { testimonial } from '@/data/club';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminPost } from '@/features/admin/seed';
import { newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, SegmentedTabs, Section, Split, UploadDrop } from '@/features/admin/ui';
import { cx, formatDate } from '@/lib/format';
import styles from './AdminShared.module.css';

type Tab = 'blog' | 'galeria' | 'textos' | 'avisos';
const emptyPost = (): AdminPost => ({ slug: '', title: '', category: 'guias', date: new Date().toISOString().slice(0, 10), status: 'borrador', body: '' });

export default function AdminContenidoPage() {
  const posts = useAdmin((d) => d.posts);
  const photos = useAdmin((d) => d.photos);
  const [tab, setTab] = useState<Tab>('blog');
  const [post, setPost] = useState<AdminPost | null>(posts[0] ?? null);
  const [album, setAlbum] = useState<string>('torneos');
  const [msg, setMsg] = useState<string | null>(null);
  const [texts, setTexts] = useState({ quote: testimonial.quote, author: testimonial.author, about: 'En la Ruta de los Cenotes, Quintana Roo, reunimos a entusiastas y principiantes del tiro deportivo y recreativo con rifles PCP.' });
  const [notice, setNotice] = useState({ on: false, text: 'Este sábado cerramos a las 14:00 por torneo interno.' });
  const bodyRef = useRef<HTMLTextAreaElement>(null);

  // Barra de formato sencilla (Markdown): envuelve la selección.
  const wrap = (before: string, after = before) => {
    const el = bodyRef.current;
    if (!el || !post) return;
    const { selectionStart: s, selectionEnd: e, value } = el;
    const next = value.slice(0, s) + before + value.slice(s, e) + after + value.slice(e);
    setPost({ ...post, body: next });
  };

  const savePost = (status: AdminPost['status']) => {
    if (!post || !post.title.trim()) return setMsg('Escribe un título.');
    const isNew = !post.slug;
    const p = { ...post, status, slug: post.slug || post.title.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') };
    update((d) => ({ ...d, posts: isNew ? [p, ...d.posts] : d.posts.map((x) => (x.slug === p.slug ? p : x)) }));
    setPost(p);
    setMsg(status === 'publicado' ? 'Artículo publicado.' : status === 'programado' ? `Programado para el ${formatDate(p.date)}.` : 'Borrador guardado.');
  };

  return (
    <>
      <title>Contenido · Panel PCP</title>
      <AdminPageHeader title="Contenido" subtitle="Blog, galería y textos del sitio, sin tocar código" actions={<ButtonLink to="/" variant="secondary">Ver sitio</ButtonLink>} />
      <AdminBody>
        <SegmentedTabs
          label="Tipo de contenido"
          value={tab}
          onChange={(t) => { setTab(t); setMsg(null); }}
          options={[
            { value: 'blog', label: 'Blog' },
            { value: 'galeria', label: 'Galería' },
            { value: 'textos', label: 'Páginas y textos' },
            { value: 'avisos', label: 'Avisos del sitio' },
          ]}
        />

        {tab === 'blog' && (
          <Split
            asideWidth={460}
            main={
              <Section title="Artículos del blog" actions={<Button iconStart={<IconPlus size={16} />} onClick={() => { setPost(emptyPost()); setMsg(null); revealEditor(); }}>Nuevo</Button>}>
                <ul className={styles.list}>
                  {posts.map((p) => (
                    <li key={p.slug}>
                      <button type="button" className={cx(styles.itemCard, p.slug === post?.slug && styles.itemSelected)} onClick={() => { setPost(p); setMsg(null); revealEditor(); }}>
                        <span className={styles.thumb}><Photo alt={p.title} /></span>
                        <span className={styles.itemBody}>
                          <span className={styles.itemHead}>
                            <strong>{p.title}</strong>
                            <StatusBadge status={p.status} />
                          </span>
                          <span className={styles.itemMeta}>{blogCategories.find((c) => c.id === p.category)?.label} · {formatDate(p.date)}</span>
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              </Section>
            }
            aside={
              post && (
                <Panel title={post.slug ? 'Editar artículo' : 'Nuevo artículo'} id="editor">
                  {msg && <FormAlert tone={msg.startsWith('Escribe') ? 'error' : 'success'}>{msg}</FormAlert>}
                  <TextField label="Título" value={post.title} onChange={(e) => setPost({ ...post, title: e.target.value })} />
                  <div className={styles.grid2}>
                    <SelectField label="Categoría" value={post.category} onChange={(e) => setPost({ ...post, category: e.target.value })} options={blogCategories.slice(1).map((c) => ({ value: c.id, label: c.label }))} />
                    <TextField label="Publicar el" type="date" value={post.date} onChange={(e) => setPost({ ...post, date: e.target.value })} />
                  </div>
                  <UploadDrop title="Imagen de portada" />
                  <div>
                    <div className={styles.actions} role="toolbar" aria-label="Formato">
                      <Button variant="secondary" onClick={() => wrap('**')} aria-label="Negritas">B</Button>
                      <Button variant="secondary" onClick={() => wrap('_')} aria-label="Cursivas">I</Button>
                      <Button variant="secondary" onClick={() => wrap('\n## ', '')} aria-label="Subtítulo">H2</Button>
                      <Button variant="secondary" onClick={() => wrap('[', '](https://)')} aria-label="Enlace">Link</Button>
                    </div>
                  </div>
                  <TextareaField ref={bodyRef} label="Contenido" rows={8} value={post.body} onChange={(e) => setPost({ ...post, body: e.target.value })} />
                  <div className={styles.actions}>
                    <Button size="lg" onClick={() => savePost(post.date > new Date().toISOString().slice(0, 10) ? 'programado' : 'publicado')}>
                      {post.date > new Date().toISOString().slice(0, 10) ? 'Programar' : 'Publicar'}
                    </Button>
                    <Button size="lg" variant="secondary" onClick={() => savePost('borrador')}>Guardar borrador</Button>
                  </div>
                </Panel>
              )
            }
          />
        )}

        {tab === 'galeria' && (
          <Section
            title="Galería"
            actions={<SelectField label="Álbum" value={album} onChange={(e) => setAlbum(e.target.value)} options={galleryCategories.slice(1).map((c) => ({ value: c.id, label: c.label }))} />}
          >
            {msg && <FormAlert tone="success">{msg}</FormAlert>}
            <div className={styles.photos}>
              <UploadDrop
                title="Subir fotos o videos"
                accept="image/png,image/jpeg,video/mp4"
                multiple
                onFiles={(files) => {
                  update((d) => ({ ...d, photos: [...files.map((f) => ({ id: newId('G'), title: f.name.replace(/\.\w+$/, ''), album })), ...d.photos] }));
                  setMsg(`${files.length} archivo(s) agregados a ${album} (en demo solo se guarda el nombre).`);
                }}
              />
              {photos.filter((p) => p.album === album).map((p) => (
                <div key={p.id} className={styles.photo} style={{ position: 'relative' }}>
                  <Photo alt={p.title} />
                  <button
                    type="button"
                    aria-label={`Quitar ${p.title}`}
                    onClick={() => update((d) => ({ ...d, photos: d.photos.filter((x) => x.id !== p.id) }))}
                    style={{ position: 'absolute', top: 6, right: 6, width: 32, height: 32, borderRadius: 8, border: 'none', background: 'rgba(0,0,0,.7)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
                  >
                    <IconClose size={16} />
                  </button>
                </div>
              ))}
            </div>
          </Section>
        )}

        {tab === 'textos' && (
          <Section title="Páginas y textos">
            {msg && <FormAlert tone="success">{msg}</FormAlert>}
            <TextareaField label="Nosotros · texto principal" rows={3} value={texts.about} onChange={(e) => setTexts({ ...texts, about: e.target.value })} />
            <TextareaField label="Testimonio" rows={2} value={texts.quote} onChange={(e) => setTexts({ ...texts, quote: e.target.value })} />
            <TextField label="Autor del testimonio" value={texts.author} onChange={(e) => setTexts({ ...texts, author: e.target.value })} />
            <div><Button size="lg" onClick={() => setMsg('Textos guardados (se publican al integrar la API).')}>Guardar textos</Button></div>
          </Section>
        )}

        {tab === 'avisos' && (
          <Section title="Avisos del sitio">
            {msg && <FormAlert tone="success">{msg}</FormAlert>}
            <Switch label="Mostrar aviso en la parte superior del sitio" showLabel checked={notice.on} onChange={(on) => setNotice({ ...notice, on })} />
            <TextareaField label="Texto del aviso" rows={2} value={notice.text} onChange={(e) => setNotice({ ...notice, text: e.target.value })} />
            <div><Button size="lg" onClick={() => setMsg(notice.on ? 'Aviso activado.' : 'Aviso guardado (desactivado).')}>Guardar aviso</Button></div>
          </Section>
        )}
      </AdminBody>
    </>
  );
}
