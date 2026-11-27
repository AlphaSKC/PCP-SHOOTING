import { useCallback, useEffect, useState } from 'react';
import { Button, EmptyState, FormAlert, StatusBadge, Switch, TextField } from '@/components/ui';
import { IconChevronLeft, IconChevronRight, IconPlus } from '@/components/icons/icons';
import { revealEditor } from '@/features/admin/scroll';
import { Panel, Section, Split, UploadDrop } from '@/features/admin/ui';
import {
  LOGO_ACCEPT,
  LOGO_MAX_BYTES,
  emptyBrand,
  isMissingEndpoint,
  listAdminBrands,
  removeBrandLogo,
  saveBrand,
  uploadBrandLogo,
  validateBrand,
  type BrandDraft,
} from '@/features/marcas/api';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import styles from './AdminShared.module.css';
import own from './AdminMarcasApi.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError ? (e.status === 409 ? 'Alguien más editó esta marca. Recargamos los datos: revisa y vuelve a guardar.' : e.message) : fallback;

/** Vista previa de la tarjeta tal como se ve en la portada. */
function BrandPreview({ name, tagline, logo }: { name: string; tagline: string; logo: string | null }) {
  return (
    <div className={own.preview}>
      {logo ? <img src={logo} alt={name || 'Logo'} className={own.previewLogo} /> : <span className={own.previewWord}>{name || 'Nombre de la marca'}</span>}
      {tagline && <span className={own.previewTag}>{tagline}</span>}
    </div>
  );
}

/** Marcas que nos acompañan (portada): lista, orden, activar, logo o solo texto. API F17. */
export function AdminMarcasApi() {
  const [brands, setBrands] = useState<BrandDraft[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'missing' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [edit, setEdit] = useState<BrandDraft | null>(null);
  const [file, setFile] = useState<{ file: File; preview: string; op: string; savedVersion?: string | null } | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [logoFailed, setLogoFailed] = useState(false);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const list = await listAdminBrands();
      setBrands(list);
      setStatus('ready');
      return list;
    } catch (e) {
      if (isMissingEndpoint(e)) setStatus('missing');
      else {
        setLoadError(errorText(e, 'No pudimos cargar las marcas.'));
        setStatus('error');
      }
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((list) => {
      if (list?.[0]) setEdit((d) => d ?? list[0]!);
    });
  }, [load]);

  const previewUrl = file?.preview;
  useEffect(() => () => { if (previewUrl) URL.revokeObjectURL(previewUrl); }, [previewUrl]);

  function open(b: BrandDraft) {
    setEdit(b);
    setFile(null);
    setLogoFailed(false);
    setMsg(null);
    revealEditor();
  }

  function pick(files: File[]) {
    const f = files[0];
    if (!f) return;
    if (!LOGO_ACCEPT.split(',').includes(f.type)) return setMsg({ tone: 'error', text: 'El logo debe ser PNG, JPG o WebP.' });
    if (f.size > LOGO_MAX_BYTES) return setMsg({ tone: 'error', text: 'El logo pesa más de 2 MB.' });
    setMsg(null);
    setFile({ file: f, preview: URL.createObjectURL(f), op: crypto.randomUUID() });
    setLogoFailed(false);
  }

  async function onSave() {
    if (!edit) return;
    const problem = validateBrand(edit);
    if (problem) return setMsg({ tone: 'error', text: problem });
    setBusy(true);
    setMsg(null);
    try {
      const current = brands.find((b) => b.id === edit.id);
      const sameFields = current && current.version === edit.version
        && current.name === edit.name && current.tagline === edit.tagline && current.url === edit.url
        && current.active === edit.active && current.order === edit.order
        && current.slug === edit.slug && current.carousel === edit.carousel;
      let saved = sameFields ? { ...edit } : await saveBrand(edit);
      setEdit(saved);
      setBrands((list) => [...list.filter((b) => b.id !== saved.id), saved].sort((a, b) => a.order - b.order));
      if (file && saved.id) {
        const op = file.savedVersion && file.savedVersion !== saved.version ? crypto.randomUUID() : file.op;
        setFile({ ...file, op, savedVersion: saved.version });
        saved = { ...saved, logoUrl: await uploadBrandLogo(saved.id, file.file, op) };
        setFile(null);
        setLogoFailed(false);
      }
      const list = await load();
      setEdit(list?.find((b) => b.id === saved.id) ?? saved);
      setMsg({ tone: 'success', text: saved.active ? 'Marca guardada. Ya aparece en la portada.' : 'Marca guardada (oculta en la portada).' });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        const list = await load();
        const fresh = list?.find((b) => b.id === edit.id);
        if (fresh) setEdit(fresh);
        setFile((f) => f ? { ...f, op: crypto.randomUUID(), savedVersion: null } : null);
      }
      if (file) setLogoFailed(true);
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar la marca.') });
    } finally {
      setBusy(false);
    }
  }

  async function onRemoveLogo() {
    if (!edit?.id || !window.confirm('¿Quitar el logo? La marca se mostrará solo con su nombre.')) return;
    setBusy(true);
    setMsg(null);
    try {
      await removeBrandLogo(edit.id);
      const list = await load();
      const fresh = list?.find((b) => b.id === edit.id);
      if (fresh) setEdit(fresh);
      setMsg({ tone: 'success', text: 'Logo quitado: se muestra el nombre.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos quitar el logo.') });
    } finally {
      setBusy(false);
    }
  }

  /** Mueve una marca una posición (intercambia `orden` con la vecina y guarda ambas). */
  async function move(index: number, dir: -1 | 1) {
    const a = brands[index];
    const b = brands[index + dir];
    if (!a || !b) return;
    setBusy(true);
    setMsg(null);
    try {
      await saveBrand({ ...a, order: b.order });
      await saveBrand({ ...b, order: a.order });
      await load();
    } catch (e) {
      await load();
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar el orden.') });
    } finally {
      setBusy(false);
    }
  }

  if (status === 'missing') {
    return (
      <EmptyState
        title="Marcas: pendiente de API"
        description="El backend aún no guarda las marcas (solicitud F17 en el roadmap). Mientras tanto la portada muestra la lista fija del sitio. En cuanto Kevin publique los endpoints, aquí podrás agregar, ordenar, ocultar y subir el logo de cada marca."
      />
    );
  }
  if (status === 'error') return <EmptyState title="No pudimos cargar las marcas" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;

  const nextOrder = brands.length ? Math.max(...brands.map((b) => b.order)) + 1 : 1;
  // F24: el backend ya devuelve `slug` (tienda por marcas).
  const taxonomy = brands.some((b) => b.taxonomy);
  const logo = file?.preview ?? edit?.logoUrl ?? null;

  return (
    <Split
      asideWidth={420}
      main={
        <Section title="Marcas que nos acompañan" actions={<Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyBrand(nextOrder, taxonomy))}>Nueva marca</Button>}>
          <p className={styles.muted}>
            {taxonomy
              ? 'Se usan en la tienda (botones de marca dentro de cada categoría) y, si lo eliges, en el carrusel de la portada, en este orden. Cada marca puede llevar logo o solo su nombre.'
              : 'Aparecen en la portada, en este orden, dentro del carrusel. Cada marca puede llevar logo o solo su nombre.'}
          </p>
          {status === 'loading' ? (
            <p className={styles.muted}>Cargando marcas…</p>
          ) : brands.length === 0 ? (
            <p className={styles.muted}>Aún no hay marcas. Agrega la primera con «Nueva marca».</p>
          ) : (
            <ul className={styles.list}>
              {brands.map((b, i) => (
                <li key={b.id} className={cx(own.row, b.id === edit?.id && own.rowSelected)}>
                  <button type="button" className={own.rowMain} onClick={() => open(b)}>
                    <BrandPreview name={b.name} tagline="" logo={b.logoUrl} />
                    <span className={styles.stack}>
                      <strong>{b.name}</strong>
                      <span>{b.logoUrl ? 'Con logo' : 'Solo texto'}{b.tagline ? ` · ${b.tagline}` : ''}</span>
                    </span>
                  </button>
                  <StatusBadge status={b.active ? 'publicado' : 'pausado'} />
                  <span className={own.order}>
                    <Button variant="secondary" aria-label={`Subir ${b.name}`} disabled={busy || i === 0} onClick={() => void move(i, -1)}>
                      <IconChevronLeft size={16} className={own.up} />
                    </Button>
                    <Button variant="secondary" aria-label={`Bajar ${b.name}`} disabled={busy || i === brands.length - 1} onClick={() => void move(i, 1)}>
                      <IconChevronRight size={16} className={own.up} />
                    </Button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Section>
      }
      aside={
        edit && (
          <Panel title={edit.id ? 'Editar marca' : 'Nueva marca'} id="editor">
            <span className={styles.muted}>Así se verá en la portada</span>
            <BrandPreview name={edit.name} tagline={edit.tagline} logo={logo} />
            {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
            <TextField label="Nombre de la marca" maxLength={80} value={edit.name} onChange={(e) => setEdit({ ...edit, name: e.target.value })} />
            <TextField label="Texto corto (opcional)" placeholder="Rifles PCP, proyectiles…" maxLength={60} value={edit.tagline} onChange={(e) => setEdit({ ...edit, tagline: e.target.value })} />
            <TextField label="Sitio de la marca (opcional)" placeholder="https://…" maxLength={2048} value={edit.url} onChange={(e) => setEdit({ ...edit, url: e.target.value })} />
            <UploadDrop
              title={logo ? 'Cambiar logo' : 'Subir logo (opcional)'}
              hint="PNG o WebP con fondo transparente, o JPG · hasta 2 MB. Sin logo se muestra el nombre."
              accept={LOGO_ACCEPT}
              onFiles={pick}
            />
            {file && !logoFailed && <p className={styles.muted}>El logo se sube al guardar.</p>}
            {file && logoFailed && (
              <FormAlert inline>Este logo todavía NO está guardado: es solo una vista previa. Pulsa «Guardar marca» para reintentar.</FormAlert>
            )}
            {edit.id && edit.logoUrl && !file && (
              <Button variant="text" disabled={busy} onClick={() => void onRemoveLogo()}>
                Quitar logo (mostrar solo el nombre)
              </Button>
            )}
            {taxonomy ? (
              <>
                <TextField
                  label="Dirección web en la tienda (opcional)"
                  hint={`Se verá como /tienda/municiones/${edit.slug || 'nombre-de-la-marca'}. Vacío = se genera del nombre.`}
                  maxLength={80}
                  value={edit.slug}
                  onChange={(e) => setEdit({ ...edit, slug: e.target.value.toLowerCase() })}
                />
                <Switch label="Activa (se ve en la tienda)" showLabel checked={edit.active} onChange={(active) => setEdit({ ...edit, active })} />
                <Switch label="Mostrar también en el carrusel de la portada" showLabel checked={edit.carousel} onChange={(carousel) => setEdit({ ...edit, carousel })} />
              </>
            ) : (
              <Switch label="Visible en la portada" showLabel checked={edit.active} onChange={(active) => setEdit({ ...edit, active })} />
            )}
            <Button size="lg" fullWidth loading={busy} onClick={() => void onSave()}>
              {edit.id ? 'Guardar marca' : 'Agregar marca'}
            </Button>
          </Panel>
        )
      }
    />
  );
}
