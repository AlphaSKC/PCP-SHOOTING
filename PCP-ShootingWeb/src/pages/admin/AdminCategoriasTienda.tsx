import { useCallback, useEffect, useState } from 'react';
import { Button, ButtonLink, EmptyState, FormAlert, Photo, StatusBadge, Switch, TextareaField, TextField } from '@/components/ui';
import { IconChevronLeft, IconChevronRight, IconPlus } from '@/components/icons/icons';
import { revealEditor } from '@/features/admin/scroll';
import { Panel, Section, Split, UploadDrop } from '@/features/admin/ui';
import {
  CATEGORY_IMAGE_ACCEPT,
  CATEGORY_IMAGE_MAX_BYTES,
  RESERVED_SLUGS,
  deleteCategory,
  emptyCategory,
  listAdminCategories,
  saveCategory,
  slugify,
  uploadCategoryImage,
  validateCategory,
  type AdminStoreCategory,
} from '@/features/tienda/categoriasApi';
import { CategoryTiles } from '@/features/tienda/components/StoreNav';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import styles from './AdminShared.module.css';
import own from './AdminMarcasApi.module.css';
import local from './AdminCategoriasTienda.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

/**
 * Panel → Productos → Categorías (F24): el cliente crea las categorías de la tienda (Rifles, Municiones…),
 * su foto, orden y visibilidad. Las marcas de cada categoría salen solas de los productos asignados.
 */
export function AdminCategoriasTienda() {
  const [list, setList] = useState<AdminStoreCategory[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'missing' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [edit, setEdit] = useState<AdminStoreCategory | null>(null);
  const [slugTouched, setSlugTouched] = useState(true);
  const [file, setFile] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [imageFailed, setImageFailed] = useState(false);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const r = await listAdminCategories();
      if (r === null) {
        setStatus('missing');
        return null;
      }
      setList(r);
      setStatus('ready');
      return r;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar las categorías.'));
      setStatus('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((r) => {
      if (r?.[0]) setEdit((d) => d ?? r[0]!);
    });
  }, [load]);

  const preview = file?.preview;
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

  function open(c: AdminStoreCategory, isNew = false) {
    setEdit(c);
    setSlugTouched(!isNew);
    setFile(null);
    setImageFailed(false);
    setMsg(null);
    revealEditor();
  }

  function pick(files: File[]) {
    const f = files[0];
    if (!f) return;
    if (!CATEGORY_IMAGE_ACCEPT.split(',').includes(f.type)) return setMsg({ tone: 'error', text: 'La foto debe ser JPG, PNG o WebP.' });
    if (f.size > CATEGORY_IMAGE_MAX_BYTES) return setMsg({ tone: 'error', text: 'La foto pesa más de 5 MB.' });
    setMsg(null);
    setImageFailed(false);
    setFile({ file: f, preview: URL.createObjectURL(f), op: crypto.randomUUID() });
  }

  async function onSave() {
    if (!edit) return;
    const problem = validateCategory(edit, list);
    if (problem) return setMsg({ tone: 'error', text: problem });
    setBusy(true);
    setMsg(null);
    try {
      let saved = await saveCategory(edit);
      setEdit(saved);
      if (file) {
        try {
          const r = await uploadCategoryImage(saved.id, file.file, file.op);
          saved = { ...saved, imageUrl: r.imageUrl, version: r.version };
          setFile(null);
          setImageFailed(false);
        } catch (e) {
          setImageFailed(true);
          throw e;
        }
      }
      const fresh = await load();
      setEdit(fresh?.find((c) => c.id === saved.id) ?? saved);
      setSlugTouched(true);
      setMsg({ tone: 'success', text: saved.active ? `Categoría «${saved.name}» guardada. Ya aparece en la tienda.` : `Categoría «${saved.name}» guardada (oculta en la tienda).` });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409 && edit.id) {
        const fresh = await load();
        const cur = fresh?.find((c) => c.id === edit.id);
        if (cur) setEdit(cur);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar la categoría.') });
    } finally {
      setBusy(false);
    }
  }

  async function onDelete() {
    if (!edit?.id) return;
    if (!window.confirm(`¿Eliminar la categoría «${edit.name}»? No se puede deshacer.`)) return;
    setBusy(true);
    setMsg(null);
    try {
      await deleteCategory(edit.id);
      const fresh = await load();
      setEdit(fresh?.[0] ?? null);
      setMsg({ tone: 'success', text: 'Categoría eliminada.' });
    } catch (e) {
      setMsg({
        tone: 'error',
        text: e instanceof ApiError && e.status === 409 ? 'Tiene productos asignados: muévelos a otra categoría o desactívala con «Visible en la tienda».' : errorText(e, 'No pudimos eliminar la categoría.'),
      });
    } finally {
      setBusy(false);
    }
  }

  /** Intercambia el orden con la vecina y guarda ambas. */
  async function move(index: number, dir: -1 | 1) {
    const a = list[index];
    const b = list[index + dir];
    if (!a || !b) return;
    setBusy(true);
    setMsg(null);
    try {
      const orderA = a.order === b.order ? b.order + dir : b.order;
      await saveCategory({ ...a, order: Math.max(0, orderA) });
      await saveCategory({ ...b, order: a.order });
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
        title="Categorías de la tienda: pendiente de backend (F24)"
        description="Aquí crearás las categorías de la tienda (Rifles, Municiones, Cargadores, Accesorios, Tanques, Compresores, Repuestos, Ropa PCP…) con su foto y orden, y en cada producto elegirás su categoría y marca. La pantalla ya está lista; falta que Kevin publique F24 (prioridad 1 en el roadmap). Mientras tanto la tienda se ve como lista única."
      />
    );
  }
  if (status === 'error') return <EmptyState title="No pudimos cargar las categorías" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;

  const nextOrder = list.length ? Math.max(...list.map((c) => c.order)) + 1 : 1;
  const image = file?.preview ?? edit?.imageUrl ?? null;

  return (
    <Split
      asideWidth={420}
      main={
        <Section
          title="Categorías de la tienda"
          actions={<Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyCategory(nextOrder), true)}>Nueva categoría</Button>}
        >
          <p className={styles.muted}>
            Son los botones de la tienda, en este orden. Al entrar a una categoría se ven las marcas de sus productos; asigna categoría y marca a cada producto en la pestaña «Tienda».
          </p>
          {status === 'loading' ? (
            <p className={styles.muted}>Cargando categorías…</p>
          ) : list.length === 0 ? (
            <p className={styles.muted}>Aún no hay categorías. Crea la primera con «Nueva categoría» (por ejemplo Rifles o Municiones).</p>
          ) : (
            <ul className={styles.list}>
              {list.map((c, i) => (
                <li key={c.id} className={cx(own.row, c.id === edit?.id && own.rowSelected)}>
                  <button type="button" className={own.rowMain} onClick={() => open(c)}>
                    <span className={styles.thumb}><Photo src={c.imageUrl} alt={c.name} /></span>
                    <span className={styles.stack}>
                      <strong>{c.name}</strong>
                      <span>/tienda/{c.slug} · {c.count} producto{c.count === 1 ? '' : 's'}</span>
                    </span>
                  </button>
                  <StatusBadge status={c.active ? 'publicado' : 'pausado'} />
                  <span className={own.order}>
                    <Button variant="secondary" aria-label={`Subir ${c.name}`} disabled={busy || i === 0} onClick={() => void move(i, -1)}>
                      <IconChevronLeft size={16} className={own.up} />
                    </Button>
                    <Button variant="secondary" aria-label={`Bajar ${c.name}`} disabled={busy || i === list.length - 1} onClick={() => void move(i, 1)}>
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
          <Panel title={edit.id ? 'Editar categoría' : 'Nueva categoría'} id="editor">
            <span className={styles.muted}>Así se verá el botón en la tienda</span>
            <div className={local.tilePreview} aria-hidden="true">
              <CategoryTiles categories={[{ ...edit, name: edit.name || 'Nombre', imageUrl: image }]} />
            </div>
            {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
            <TextField
              label="Nombre"
              placeholder="Municiones"
              maxLength={60}
              value={edit.name}
              onChange={(e) => {
                const name = e.target.value;
                setEdit((d) => d && { ...d, name, slug: slugTouched ? d.slug : slugify(name) });
              }}
            />
            <TextField
              label="Dirección web"
              hint={`Se verá como /tienda/${edit.slug || '…'} · minúsculas, números y guiones. No uses: ${RESERVED_SLUGS.join(', ')}.`}
              maxLength={60}
              value={edit.slug}
              onChange={(e) => { setSlugTouched(true); setEdit((d) => d && { ...d, slug: e.target.value.toLowerCase() }); }}
            />
            <TextareaField
              label="Descripción (opcional)"
              hint="Aparece arriba en la página de la categoría."
              rows={2}
              maxLength={300}
              value={edit.description}
              onChange={(e) => setEdit((d) => d && { ...d, description: e.target.value })}
            />
            <UploadDrop title={image ? 'Cambiar foto' : 'Subir foto (opcional)'} hint="JPG, PNG o WebP · hasta 5 MB. Sin foto se ve en negro con el nombre." accept={CATEGORY_IMAGE_ACCEPT} onFiles={pick} />
            {file && !imageFailed && <p className={styles.muted}>La foto se sube al guardar.</p>}
            {file && imageFailed && (
              <FormAlert inline>Esta foto todavía NO está guardada: es solo una vista previa. Pulsa «Guardar categoría» para reintentar.</FormAlert>
            )}
            <Switch label="Visible en la tienda" showLabel checked={edit.active} onChange={(active) => setEdit((d) => d && { ...d, active })} />
            <Button size="lg" fullWidth loading={busy} onClick={() => void onSave()}>
              {edit.id ? 'Guardar categoría' : 'Crear categoría'}
            </Button>
            {edit.id && (
              <>
                <ButtonLink to={`/tienda/${edit.slug}`} variant="secondary" fullWidth>
                  Ver en la tienda
                </ButtonLink>
                {edit.count === 0 ? (
                  <Button variant="text" disabled={busy} onClick={() => void onDelete()}>
                    Eliminar categoría
                  </Button>
                ) : (
                  <p className={styles.muted}>Tiene {edit.count} producto{edit.count === 1 ? '' : 's'}: para quitarla de la tienda apaga «Visible en la tienda».</p>
                )}
              </>
            )}
          </Panel>
        )
      }
    />
  );
}
