import { useCallback, useEffect, useState } from 'react';
import { DeleteProductButton } from '@/features/admin/DeleteProductButton';
import { Button, DataTable, EmptyState, FormAlert, Photo, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { money } from '@/features/admin/format';
import { IMAGE_ACCEPT, IMAGE_MAX_BYTES, setProductActive, suggestSku, uploadProductImage } from '@/features/admin/productosApi';
import { revealEditor } from '@/features/admin/scroll';
import { Panel, Split, UploadDrop } from '@/features/admin/ui';
import { listAdminRifles, parseOptionalPrice, saveRifle, type AdminRifleApi, type RifleDraft } from '@/features/catalogo/api';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;

const emptyDraft = (): RifleDraft => ({
  id: null,
  sku: '',
  name: '',
  description: '',
  short: '',
  type: '',
  calibers: '.22 · .25 · .30',
  firingMode: '',
  highlight: '',
  sheet: [],
  price: '',
  imageUrl: null,
  notes: null,
  rawSpecs: {},
});

const toDraft = (r: AdminRifleApi): RifleDraft => ({ ...r, price: r.price != null ? String(r.price) : '' });
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

/** Catálogo de armas en el panel, conectado a la API (categoría 3, no vendible). */
export function AdminArmasApi() {
  const [items, setItems] = useState<AdminRifleApi[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [draft, setDraft] = useState<RifleDraft | null>(null);
  const [skuTouched, setSkuTouched] = useState(false);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);
  const [toggling, setToggling] = useState(false);
  const [pendingImage, setPendingImage] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [uploadFailed, setUploadFailed] = useState(false);
  const [uploading, setUploading] = useState(false);

  const current = draft?.id ? items.find((r) => r.id === draft.id) ?? null : null;

  const load = useCallback(async () => {
    try {
      const list = await listAdminRifles();
      setItems(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar el catálogo.'));
      setStatus('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((list) => {
      if (list && list[0]) setDraft((d) => d ?? toDraft(list[0]));
    });
  }, [load]);

  useEffect(() => () => { if (pendingImage) URL.revokeObjectURL(pendingImage.preview); }, [pendingImage]);

  function open(next: RifleDraft, touched: boolean) {
    setDraft(next);
    setSkuTouched(touched);
    setMsg(null);
    setPendingImage(null);
    revealEditor();
  }

  function pickImage(files: File[]) {
    const file = files[0];
    if (!file) return;
    if (!IMAGE_ACCEPT.split(',').includes(file.type)) return setMsg({ tone: 'error', text: 'La foto debe ser JPG, PNG o WebP.' });
    if (file.size > IMAGE_MAX_BYTES) return setMsg({ tone: 'error', text: 'La foto pesa más de 5 MB. Redúcela e intenta de nuevo.' });
    setMsg(null);
    setPendingImage({ file, preview: URL.createObjectURL(file), op: crypto.randomUUID() });
    setUploadFailed(false);
  }

  async function uploadPending(id: string) {
    if (!pendingImage) return true;
    setUploading(true);
    try {
      await uploadProductImage(id, pendingImage.file, pendingImage.op);
      setPendingImage(null);
      setUploadFailed(false);
      return true;
    } catch (e) {
      setUploadFailed(true);
      setMsg({ tone: 'error', text: `${errorText(e, 'No pudimos subir la foto.')} Puedes reintentar con «Subir foto».` });
      return false;
    } finally {
      setUploading(false);
    }
  }

  async function onSave() {
    if (!draft) return;
    if (!draft.name.trim()) return setMsg({ tone: 'error', text: 'Escribe el nombre del modelo.' });
    if (!draft.sku.trim()) return setMsg({ tone: 'error', text: 'Escribe el SKU (código interno único).' });
    if (parseOptionalPrice(draft.price) === undefined)
      return setMsg({ tone: 'error', text: 'El precio debe ser un número (ej. 18500) o quedar vacío para «Por cotizar».' });
    setSaving(true);
    setMsg(null);
    try {
      const id = await saveRifle(draft);
      const imageOk = await uploadPending(id);
      const list = await load();
      const saved = list?.find((r) => r.id === id);
      if (saved) setDraft(toDraft(saved));
      setSkuTouched(true);
      if (imageOk) setMsg({ tone: 'success', text: draft.id ? 'Modelo guardado.' : 'Modelo creado. Ya aparece en el catálogo.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el modelo. Intenta de nuevo.') });
    } finally {
      setSaving(false);
    }
  }

  async function onUploadOnly() {
    if (!draft?.id || !pendingImage) return;
    setMsg(null);
    if (await uploadPending(draft.id)) {
      const list = await load();
      const saved = list?.find((r) => r.id === draft.id);
      if (saved) setDraft((d) => (d ? { ...d, imageUrl: saved.imageUrl } : d));
      setMsg({ tone: 'success', text: 'Foto actualizada.' });
    }
  }

  async function onToggle(active: boolean) {
    if (!current) return;
    setToggling(true);
    setMsg(null);
    try {
      await setProductActive(current.id, active);
      await load();
      setMsg({ tone: 'success', text: active ? 'El modelo ya se ve en el catálogo.' : 'El modelo se ocultó del catálogo.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar la visibilidad.') });
    } finally {
      setToggling(false);
    }
  }

  const setSheet = (i: number, patch: Partial<{ label: string; value: string }>) =>
    draft && setDraft({ ...draft, sheet: draft.sheet.map((f, j) => (j === i ? { ...f, ...patch } : f)) });

  const cols: Column<AdminRifleApi>[] = [
    {
      key: 'name',
      header: 'Modelo',
      primary: true,
      render: (r) => (
        <span className={styles.prodCell}>
          <span className={styles.thumb}><Photo src={r.imageUrl ?? undefined} alt={r.name} /></span>
          <span className={styles.stack}>
            <strong>{r.name}</strong>
            <span>{r.sku}</span>
          </span>
        </span>
      ),
    },
    { key: 'type', header: 'Tipo', render: (r) => r.type || '—', hideOnMobile: true },
    { key: 'price', header: 'Precio', render: (r) => (r.price != null ? money(r.price) : 'Por cotizar') },
    { key: 'vis', header: 'Estado', render: (r) => <StatusBadge status={r.active ? 'publicado' : 'borrador'} /> },
    {
      key: 'edit',
      header: 'Acción',
      hideOnMobile: true,
      render: (r) => (
        <button type="button" className={styles.linkBtn} onClick={() => open(toDraft(r), true)}>
          Editar
        </button>
      ),
    },
  ];

  if (status === 'error') {
    return <EmptyState title="No pudimos cargar el catálogo" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;
  }

  const photo = pendingImage?.preview ?? draft?.imageUrl ?? undefined;

  return (
    <Split
      main={
        <div className={styles.tableCard}>
          <div className={`${styles.row} ${styles.tableHead}`}>
            <span className={styles.muted}>{items.length} modelos · informativo, los clientes solicitan cotización</span>
            <Button variant="secondary" iconStart={<IconPlus size={16} />} onClick={() => open(emptyDraft(), false)}>
              Nuevo modelo
            </Button>
          </div>
          <DataTable
            caption="Catálogo de armas"
            columns={cols}
            rows={items}
            rowKey={(r) => r.id}
            onRowClick={(r) => open(toDraft(r), true)}
            selectedKey={draft?.id ?? undefined}
            empty={
              <EmptyState
                title={status === 'loading' ? 'Cargando catálogo…' : 'Aún no hay modelos'}
                description={status === 'loading' ? undefined : 'Agrega el primero con «Nuevo modelo».'}
              />
            }
          />
        </div>
      }
      aside={
        draft && (
          <Panel title={draft.id ? 'Editar modelo' : 'Nuevo modelo'} id="editor">
            <div>
              <span className={styles.muted}>Foto del modelo</span>
              <div className={styles.photos}>
                <div className={styles.photo}><Photo src={photo} alt={draft.name || 'Modelo'} /></div>
              </div>
            </div>
            <UploadDrop title={photo ? 'Cambiar foto' : 'Agregar foto'} hint="JPG, PNG o WebP · hasta 5 MB" accept={IMAGE_ACCEPT} onFiles={pickImage} />
            {pendingImage && (
              <p className={styles.muted}>{draft.id ? 'Foto lista. Súbela o guarda los cambios.' : 'La foto se sube al crear el modelo.'}</p>
            )}
            {pendingImage && uploadFailed && (
              <FormAlert inline>Esta foto todavía NO está guardada: es solo una vista previa. No se verá en el catálogo hasta que la subida funcione.</FormAlert>
            )}
            {pendingImage && draft.id && (
              <Button variant="secondary" loading={uploading} disabled={saving} onClick={() => void onUploadOnly()}>
                Subir foto
              </Button>
            )}

            {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}

            <TextField
              label="Modelo"
              value={draft.name}
              maxLength={200}
              onChange={(e) => {
                const name = e.target.value;
                setDraft({ ...draft, name, sku: skuTouched ? draft.sku : suggestSku(`ARMA ${name}`) });
              }}
            />
            <TextField
              label="SKU"
              hint="Código interno único."
              value={draft.sku}
              maxLength={80}
              onChange={(e) => { setSkuTouched(true); setDraft({ ...draft, sku: e.target.value.toUpperCase() }); }}
            />
            <div className={styles.grid2}>
              <TextField label="Tipo" placeholder="Compacto, Bullpup…" value={draft.type} onChange={(e) => setDraft({ ...draft, type: e.target.value })} />
              <TextField label="Calibres" placeholder=".22 · .25 · .30" value={draft.calibers} onChange={(e) => setDraft({ ...draft, calibers: e.target.value })} />
            </div>
            <div className={styles.grid2}>
              <TextField
                label="Modo de disparo"
                hint="Vacío = «Consultar»."
                value={draft.firingMode}
                onChange={(e) => setDraft({ ...draft, firingMode: e.target.value })}
              />
              <TextField label="Destaca por" placeholder="Formato compacto" value={draft.highlight} onChange={(e) => setDraft({ ...draft, highlight: e.target.value })} />
            </div>
            <TextareaField
              label="Descripción"
              rows={3}
              maxLength={4000}
              value={draft.description}
              onChange={(e) => setDraft({ ...draft, description: e.target.value })}
            />
            <TextField
              label="Resumen para celular"
              hint="Una línea. Si lo dejas vacío se usa la descripción."
              maxLength={160}
              value={draft.short}
              onChange={(e) => setDraft({ ...draft, short: e.target.value })}
            />

            <div>
              <span className={styles.muted}>Ficha técnica (opcional)</span>
              {draft.sheet.map((f, i) => (
                <div key={i} className={styles.grid2}>
                  <TextField label={`Dato ${i + 1}`} placeholder="Formato" value={f.label} onChange={(e) => setSheet(i, { label: e.target.value })} />
                  <TextField
                    label="Valor"
                    placeholder="Compacto"
                    value={f.value}
                    onChange={(e) => setSheet(i, { value: e.target.value })}
                  />
                </div>
              ))}
              <div className={styles.actions}>
                <Button variant="text" onClick={() => setDraft({ ...draft, sheet: [...draft.sheet, { label: '', value: '' }] })}>
                  Agregar dato
                </Button>
                {draft.sheet.length > 0 && (
                  <Button variant="text" onClick={() => setDraft({ ...draft, sheet: draft.sheet.slice(0, -1) })}>
                    Quitar último
                  </Button>
                )}
              </div>
            </div>

            <TextField
              label="Precio (MXN)"
              hint="Déjalo vacío para mostrar «Por cotizar»."
              inputMode="decimal"
              value={draft.price}
              onChange={(e) => setDraft({ ...draft, price: e.target.value.replace(/[^\d.,]/g, '') })}
            />

            <Button size="lg" fullWidth loading={saving || uploading} onClick={() => void onSave()}>
              {draft.id ? 'Guardar cambios' : 'Crear modelo'}
            </Button>

            {current && (
              <>
                <Switch label="Visible en el catálogo" showLabel checked={current.active} disabled={toggling} onChange={(a) => void onToggle(a)} />
                <p className={styles.muted}>Para que no se vea, ocúltalo del catálogo. Solo los modelos creados por error (sin historial) se pueden eliminar.</p>
                <DeleteProductButton id={current.id} name={current.name} hideHint="ocúltalo con «Visible en el catálogo»" onDeleted={() => { setDraft(null); void load(); }} />
              </>
            )}
          </Panel>
        )
      }
    />
  );
}
