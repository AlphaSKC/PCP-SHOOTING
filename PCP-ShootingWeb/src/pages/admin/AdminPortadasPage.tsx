import { useCallback, useEffect, useState, type MouseEvent } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, Chip, ChipGroup, EmptyState, FormAlert, SelectField, StatusBadge, Switch } from '@/components/ui';
import { when } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, DemoNote, Panel, Section, Split, UploadDrop } from '@/features/admin/ui';
import {
  DEFAULT_POSITION,
  IMAGE_ACCEPT,
  IMAGE_MAX_BYTES,
  PORTADA_PAGES,
  isMissingEndpoint,
  listAdminPortadas,
  localDrafts,
  refreshPublicPortadas,
  savePortada,
  uploadPortada,
  type PortadaDraft,
  type Veil,
} from '@/features/portadas/api';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import shared from './AdminShared.module.css';
import styles from './AdminPortadas.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
const VEILS: { id: Veil; label: string }[] = [
  { id: 'suave', label: 'Suave' },
  { id: 'normal', label: 'Normal' },
  { id: 'fuerte', label: 'Fuerte' },
];
const labelOf = (key: string) => PORTADA_PAGES.find((p) => p.key === key)?.label ?? key;
const pathOf = (key: string) => PORTADA_PAGES.find((p) => p.key === key)?.path ?? '/';
const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError
    ? e.status === 409
      ? 'Alguien más cambió esta portada. Recargamos los datos: revisa y vuelve a guardar.'
      : e.status === 503
        ? 'Subir imágenes todavía no está activo en el servidor (falta configurar el almacenamiento).'
        : e.message
    : fallback;

/** Vista previa del encabezado (escritorio o celular). Clic = punto de enfoque de la foto. */
function HeroPreview({ d, image, mobile, onFocus }: { d: PortadaDraft; image: string | null; mobile?: boolean; onFocus?: (pos: string) => void }) {
  const click = (e: MouseEvent<HTMLButtonElement>) => {
    if (!onFocus || !image) return;
    const r = e.currentTarget.getBoundingClientRect();
    const x = Math.round(((e.clientX - r.left) / r.width) * 100);
    const y = Math.round(((e.clientY - r.top) / r.height) * 100);
    onFocus(`${x}% ${y}%`);
  };
  return (
    <button
      type="button"
      className={cx(styles.preview, mobile && styles.previewMobile, image && styles[`veil_${d.veil}`])}
      style={image ? { backgroundImage: `${mobile ? 'var(--grad-m)' : 'var(--grad)'}, url("${image.replace(/"/g, '%22')}")`, backgroundPosition: `center, ${d.position}` } : undefined}
      onClick={click}
      disabled={!onFocus || !image}
      aria-label={mobile ? 'Vista previa en celular' : 'Vista previa en escritorio. Toca la foto para elegir el punto de enfoque.'}
    >
      <span className={styles.previewEyebrow}>PCP Shooting</span>
      <span className={styles.previewTitle}>{labelOf(d.page)}</span>
      {!image && <span className={styles.previewEmpty}>Sin foto · fondo negro</span>}
    </button>
  );
}

/** Portadas de las páginas públicas (foto de fondo del encabezado negro). Contrato propuesto F21 (docs/ROADMAP.md, sección F21). */
export default function AdminPortadasPage() {
  const [items, setItems] = useState<PortadaDraft[]>([]);
  const [mode, setMode] = useState<'loading' | 'live' | 'preview' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [edit, setEdit] = useState<PortadaDraft | null>(null);
  const [file, setFile] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState<'save' | 'upload' | null>(null);

  const load = useCallback(async () => {
    try {
      const list = await listAdminPortadas();
      setItems(list);
      setMode('live');
      return list;
    } catch (e) {
      if (isMissingEndpoint(e)) {
        const list = localDrafts();
        setItems(list);
        setMode('preview');
        return list;
      }
      setLoadError(errorText(e, 'No pudimos cargar las portadas.'));
      setMode('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((list) => list?.[0] && setEdit((d) => d ?? list[0]!));
  }, [load]);

  const previewUrl = file?.preview;
  useEffect(() => () => { if (previewUrl) URL.revokeObjectURL(previewUrl); }, [previewUrl]);

  function open(d: PortadaDraft) {
    setEdit(d);
    setFile(null);
    setMsg(null);
    revealEditor();
  }

  function pick(files: File[]) {
    const f = files[0];
    if (!f || !edit) return;
    if (!IMAGE_ACCEPT.split(',').includes(f.type)) return setMsg({ tone: 'error', text: 'La portada debe ser JPG, PNG o WebP.' });
    if (f.size > IMAGE_MAX_BYTES) return setMsg({ tone: 'error', text: 'La imagen pesa más de 8 MB.' });
    const next = { file: f, preview: URL.createObjectURL(f), op: crypto.randomUUID() };
    setFile(next);
    setEdit({ ...edit, position: DEFAULT_POSITION, active: true });
    if (mode === 'live') void upload(next);
    else setMsg({ tone: 'success', text: 'Vista previa lista. Aún no se guarda: falta que el backend publique Portadas (F21).' });
  }

  async function upload(f: { file: File; preview: string; op: string }) {
    if (!edit) return;
    const page = edit.page;
    setBusy('upload');
    setMsg(null);
    try {
      const r = await uploadPortada(page, f.file, f.op);
      setEdit((d) => (d && d.page === page ? { ...d, imageUrl: r.imageUrl, version: r.version } : d));
      setItems((list) => list.map((d) => (d.page === page ? { ...d, imageUrl: r.imageUrl, version: r.version } : d)));
      setFile(null);
      refreshPublicPortadas();
      setMsg({ tone: 'success', text: 'Imagen subida. Ajusta el enfoque y el velo, y guarda.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos subir la imagen.') });
    } finally {
      setBusy(null);
    }
  }

  async function onSave() {
    if (!edit || mode !== 'live') return;
    if (edit.active && !edit.imageUrl) return setMsg({ tone: 'error', text: 'Sube una imagen o elige la de otra página antes de activar la portada.' });
    setBusy('save');
    setMsg(null);
    try {
      const saved = await savePortada(edit);
      setEdit(saved);
      setItems((list) => list.map((d) => (d.page === saved.page ? saved : d)));
      refreshPublicPortadas();
      setMsg({ tone: 'success', text: saved.active ? `Guardado. Ya se ve en ${labelOf(saved.page)}.` : `Guardado. ${labelOf(saved.page)} queda con fondo negro.` });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        const list = await load();
        const fresh = list?.find((d) => d.page === edit.page);
        if (fresh) setEdit(fresh);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar la portada.') });
    } finally {
      setBusy(null);
    }
  }

  if (mode === 'error') {
    return (
      <>
        <AdminPageHeader title="Portadas" subtitle="Foto de fondo del encabezado de cada página" />
        <AdminBody>
          <EmptyState title="No pudimos cargar las portadas" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        </AdminBody>
      </>
    );
  }

  const image = file?.preview ?? edit?.imageUrl ?? null;
  const reusable = items.filter((d) => d.page !== edit?.page && d.imageUrl);

  return (
    <>
      <title>Portadas · Panel PCP</title>
      <AdminPageHeader title="Portadas" subtitle="Foto de fondo del encabezado negro de cada página" />
      <AdminBody>
        {mode === 'preview' && (
          <DemoNote>
            Modo vista previa: el backend aún no guarda portadas (solicitud F21 para Kevin en el roadmap). Puedes probar cómo se vería cada página, pero no se guarda. Hoy el sitio solo tiene la foto fija de Inicio.
          </DemoNote>
        )}
        <Split
          asideWidth={460}
          main={
            <Section title="Páginas">
              <p className={shared.muted}>Elige una página para ponerle portada. Si no tiene, el encabezado se queda en negro.</p>
              <ul className={styles.grid}>
                {items.map((d) => {
                  const img = d.page === edit?.page && file ? file.preview : d.imageUrl;
                  return (
                    <li key={d.page}>
                      <button type="button" className={cx(styles.card, d.page === edit?.page && styles.cardSelected)} onClick={() => open(d)}>
                        <span className={styles.thumb} style={img && d.active ? { backgroundImage: `url("${img.replace(/"/g, '%22')}")`, backgroundPosition: d.position } : undefined} />
                        <span className={styles.cardBody}>
                          <strong>{labelOf(d.page)}</strong>
                          <span className={shared.muted}>{pathOf(d.page)}</span>
                        </span>
                        {d.active && img ? <StatusBadge tone="success">Con portada</StatusBadge> : <StatusBadge tone="neutral">Fondo negro</StatusBadge>}
                      </button>
                    </li>
                  );
                })}
              </ul>
            </Section>
          }
          aside={
            edit && (
              <Panel title={labelOf(edit.page)} id="editor" actions={<ButtonLink to={pathOf(edit.page)} variant="secondary" target="_blank">Ver página</ButtonLink>}>
                {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                <span className={shared.muted}>Escritorio · toca la foto para elegir qué parte se ve</span>
                <HeroPreview d={{ ...edit, active: true }} image={edit.active ? image : null} onFocus={(position) => setEdit({ ...edit, position })} />
                <div className={styles.mobileRow}>
                  <HeroPreview d={{ ...edit, active: true }} image={edit.active ? image : null} mobile />
                  <div className={styles.mobileText}>
                    <span className={shared.muted}>Celular</span>
                    {image && edit.position !== DEFAULT_POSITION && (
                      <Button variant="text" onClick={() => setEdit({ ...edit, position: DEFAULT_POSITION })}>
                        Centrar foto
                      </Button>
                    )}
                  </div>
                </div>

                <Switch label="Mostrar portada en esta página" showLabel checked={edit.active} onChange={(active) => setEdit({ ...edit, active })} />

                <UploadDrop
                  title={busy === 'upload' ? 'Subiendo…' : image ? 'Cambiar imagen' : 'Subir imagen'}
                  hint="JPG, PNG o WebP · hasta 8 MB. Mejor horizontal, de 1600 px de ancho o más."
                  accept={IMAGE_ACCEPT}
                  onFiles={pick}
                />
                {file && mode === 'live' && busy !== 'upload' && msg?.tone === 'error' && (
                  <>
                    <FormAlert inline>Esta imagen todavía NO está guardada: es solo una vista previa.</FormAlert>
                    <Button variant="secondary" onClick={() => void upload(file)}>
                      Reintentar subida
                    </Button>
                  </>
                )}
                {reusable.length > 0 && (
                  <SelectField
                    label="O usa la imagen de otra página"
                    value=""
                    placeholder="Elegir página…"
                    onChange={(e) => {
                      const src = items.find((d) => d.page === e.target.value);
                      if (src?.imageUrl) {
                        setFile(null);
                        setEdit({ ...edit, imageUrl: src.imageUrl, position: src.position, veil: src.veil, active: true });
                      }
                    }}
                    options={reusable.map((d) => ({ value: d.page, label: labelOf(d.page) }))}
                  />
                )}
                {image && (
                  <>
                    <ChipGroup label="Oscurecer la foto">
                      {VEILS.map((v) => (
                        <Chip key={v.id} selected={edit.veil === v.id} onClick={() => setEdit({ ...edit, veil: v.id })}>
                          Velo {v.label.toLowerCase()}
                        </Chip>
                      ))}
                    </ChipGroup>
                    <Button variant="text" onClick={() => { setFile(null); setEdit({ ...edit, imageUrl: null, active: false }); }}>
                      Quitar imagen (fondo negro)
                    </Button>
                  </>
                )}

                <Button size="lg" fullWidth loading={busy === 'save'} disabled={busy !== null || mode !== 'live'} onClick={() => void onSave()}>
                  {mode === 'live' ? 'Guardar portada' : 'Guardar (pendiente de backend)'}
                </Button>
                {edit.updatedUtc && <p className={shared.muted}>Última actualización: {when(edit.updatedUtc)}</p>}
              </Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
