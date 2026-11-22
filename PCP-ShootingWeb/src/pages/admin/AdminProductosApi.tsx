import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import {
  Button,
  ButtonLink,
  DataTable,
  EmptyState,
  FormAlert,
  Photo,
  SelectField,
  StatusBadge,
  Switch,
  TextareaField,
  TextField,
  type Column,
} from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { money } from '@/features/admin/format';
import {
  IMAGE_ACCEPT,
  IMAGE_MAX_BYTES,
  STORE_CATEGORIES,
  addStock,
  categoryLabel,
  listStoreProducts,
  parsePrice,
  saveProduct,
  setProductActive,
  suggestSku,
  uploadProductImage,
  type ApiAdminProduct,
  type ProductDraft,
  type StoreCategory,
} from '@/features/admin/productosApi';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, Panel, SegmentedTabs, Split, UploadDrop } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import { AdminArmasApi } from './AdminArmasApi';
import styles from './AdminShared.module.css';

const LOW = 5;
type Msg = { tone: 'error' | 'success'; text: string } | null;

const emptyDraft = (): ProductDraft => ({
  id: null,
  sku: '',
  name: '',
  description: '',
  category: 2,
  price: '',
  imageUrl: null,
  notes: null,
  specs: null,
});

const toDraft = (p: ApiAdminProduct): ProductDraft => ({
  id: p.id,
  sku: p.sku,
  name: p.name,
  description: p.description,
  category: (p.category === 2 ? 2 : 0) as StoreCategory,
  price: p.price != null ? String(p.price) : '',
  imageUrl: p.imageUrl,
  notes: p.notes,
  specs: p.specs,
});

const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

/** Productos de la tienda conectados a la API real. */
export default function AdminProductosApi() {
  const [params, setParams] = useSearchParams();
  const [products, setProducts] = useState<ApiAdminProduct[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [tab, setTab] = useState<'tienda' | 'armas'>('tienda');
  const [q, setQ] = useState('');

  const [draft, setDraft] = useState<ProductDraft | null>(null);
  const [skuTouched, setSkuTouched] = useState(false);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);
  const [toggling, setToggling] = useState(false);

  // Foto pendiente (en productos nuevos se sube al guardar). El id de operación se conserva en reintentos.
  const [pendingImage, setPendingImage] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [uploading, setUploading] = useState(false);

  const [entry, setEntry] = useState({ qty: '', reason: 'Entrada de mercancía', op: crypto.randomUUID() });
  const [entryMsg, setEntryMsg] = useState<Msg>(null);
  const [adding, setAdding] = useState(false);

  const current = draft?.id ? products.find((p) => p.id === draft.id) ?? null : null;

  const load = useCallback(async () => {
    setStatus((s) => (s === 'ready' ? s : 'loading'));
    try {
      const list = await listStoreProducts();
      setProducts(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los productos.'));
      setStatus('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load().then((list) => {
      if (list && list[0]) setDraft((d) => d ?? toDraft(list[0]));
    });
  }, [load]);

  useEffect(() => {
    if (params.get('nuevo')) {
      setTab('tienda');
      startNew();
      params.delete('nuevo');
      setParams(params, { replace: true });
    }
  }, [params, setParams]);

  // Libera la vista previa local de la foto.
  useEffect(() => () => { if (pendingImage) URL.revokeObjectURL(pendingImage.preview); }, [pendingImage]);

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase();
    return t ? products.filter((p) => `${p.name} ${p.sku} ${categoryLabel(p.category)}`.toLowerCase().includes(t)) : products;
  }, [products, q]);

  function resetSide() {
    setMsg(null);
    setEntryMsg(null);
    setPendingImage(null);
    setEntry({ qty: '', reason: 'Entrada de mercancía', op: crypto.randomUUID() });
  }

  function edit(p: ApiAdminProduct) {
    setDraft(toDraft(p));
    setSkuTouched(true);
    resetSide();
    revealEditor();
  }

  function startNew() {
    setDraft(emptyDraft());
    setSkuTouched(false);
    resetSide();
    revealEditor();
  }

  function pickImage(files: File[]) {
    const file = files[0];
    if (!file) return;
    if (!IMAGE_ACCEPT.split(',').includes(file.type)) return setMsg({ tone: 'error', text: 'La foto debe ser JPG, PNG o WebP.' });
    if (file.size > IMAGE_MAX_BYTES) return setMsg({ tone: 'error', text: 'La foto pesa más de 5 MB. Redúcela e intenta de nuevo.' });
    setMsg(null);
    setPendingImage({ file, preview: URL.createObjectURL(file), op: crypto.randomUUID() });
  }

  async function uploadPending(id: string) {
    if (!pendingImage) return true;
    setUploading(true);
    try {
      await uploadProductImage(id, pendingImage.file, pendingImage.op);
      setPendingImage(null);
      return true;
    } catch (e) {
      setMsg({ tone: 'error', text: `${errorText(e, 'No pudimos subir la foto.')} Puedes reintentar con «Subir foto».` });
      return false;
    } finally {
      setUploading(false);
    }
  }

  async function onSave() {
    if (!draft) return;
    if (!draft.name.trim()) return setMsg({ tone: 'error', text: 'Escribe el nombre del producto.' });
    if (!draft.sku.trim()) return setMsg({ tone: 'error', text: 'Escribe el SKU (código interno único).' });
    if (parsePrice(draft.price) == null) return setMsg({ tone: 'error', text: 'Escribe un precio mayor a 0 (ej. 450 o 380.50).' });
    setSaving(true);
    setMsg(null);
    try {
      const id = await saveProduct(draft);
      const imageOk = await uploadPending(id);
      const list = await load();
      const saved = list?.find((p) => p.id === id);
      if (saved) setDraft(toDraft(saved));
      setSkuTouched(true);
      if (imageOk) setMsg({ tone: 'success', text: draft.id ? 'Cambios guardados.' : 'Producto creado. Ya aparece en la tienda.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el producto. Intenta de nuevo.') });
    } finally {
      setSaving(false);
    }
  }

  async function onUploadOnly() {
    if (!draft?.id || !pendingImage) return;
    setMsg(null);
    if (await uploadPending(draft.id)) {
      const list = await load();
      const saved = list?.find((p) => p.id === draft.id);
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
      setMsg({ tone: 'success', text: active ? 'El producto ya se ve en la tienda.' : 'El producto se ocultó de la tienda.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar la visibilidad.') });
    } finally {
      setToggling(false);
    }
  }

  async function onAddStock() {
    if (!current) return;
    const qty = Number(entry.qty);
    if (!Number.isInteger(qty) || qty <= 0) return setEntryMsg({ tone: 'error', text: 'Escribe cuántas piezas entran (número entero).' });
    if (!entry.reason.trim()) return setEntryMsg({ tone: 'error', text: 'Escribe el motivo de la entrada.' });
    setAdding(true);
    setEntryMsg(null);
    try {
      await addStock(current.id, qty, entry.reason.trim(), entry.op);
      await load();
      setEntry({ qty: '', reason: 'Entrada de mercancía', op: crypto.randomUUID() });
      setEntryMsg({ tone: 'success', text: `Se agregaron ${qty} pzas.` });
    } catch (e) {
      // Se conserva el mismo id de operación: un reintento no duplica la entrada.
      setEntryMsg({ tone: 'error', text: errorText(e, 'No pudimos registrar la entrada.') });
    } finally {
      setAdding(false);
    }
  }

  const cols: Column<ApiAdminProduct>[] = [
    {
      key: 'name',
      header: 'Producto',
      primary: true,
      render: (p) => (
        <span className={styles.prodCell}>
          <span className={styles.thumb}><Photo src={p.imageUrl ?? undefined} alt={p.name} /></span>
          <span className={styles.stack}>
            <strong>{p.name}</strong>
            <span>{p.sku}</span>
          </span>
        </span>
      ),
    },
    { key: 'cat', header: 'Categoría', render: (p) => categoryLabel(p.category), hideOnMobile: true },
    { key: 'price', header: 'Precio', strong: true, render: (p) => (p.price != null ? money(p.price) : 'Sin precio') },
    {
      key: 'stock',
      header: 'Existencias',
      render: (p) => (
        <strong>
          {p.stock} pzas.{' '}
          {p.stock === 0 ? <StatusBadge status="agotado" /> : (p.lowStock || p.stock <= LOW) && <StatusBadge status="pocas_piezas" />}
        </strong>
      ),
    },
    { key: 'status', header: 'Estado', render: (p) => <StatusBadge status={p.active ? 'activo' : 'pausado'} /> },
    {
      key: 'edit',
      header: 'Acción',
      hideOnMobile: true,
      render: (p) => (
        <button type="button" className={styles.linkBtn} onClick={() => edit(p)}>
          Editar
        </button>
      ),
    },
  ];

  const photo = pendingImage?.preview ?? draft?.imageUrl ?? undefined;
  const busy = saving || uploading;

  return (
    <>
      <title>Productos · Panel PCP</title>
      <AdminPageHeader
        title="Productos"
        subtitle="Tienda de ropa y catálogo de armas"
        actions={
          <>
            <ButtonLink to="/tienda" variant="secondary">Ver sitio</ButtonLink>
            <Button iconStart={<IconPlus size={16} />} onClick={() => { setTab('tienda'); startNew(); }}>
              Nuevo producto
            </Button>
          </>
        }
      />
      <AdminBody>
        <div className={styles.toolbar}>
          <SegmentedTabs
            label="Tipo de producto"
            value={tab}
            onChange={(v) => { setTab(v); setMsg(null); }}
            options={[
              { value: 'tienda', label: `Tienda de ropa (${products.length})` },
              { value: 'armas', label: 'Catálogo de armas' },
            ]}
          />
          {tab === 'tienda' && (
            <TextField label="Buscar producto" placeholder="Nombre, SKU o categoría" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
          )}
        </div>

        {tab === 'armas' ? (
          <AdminArmasApi />
        ) : status === 'error' ? (
          <EmptyState
            title="No pudimos cargar los productos"
            description={loadError}
            action={<Button onClick={() => void load()}>Reintentar</Button>}
          />
        ) : (
          <Split
            main={
              <div className={styles.tableCard}>
                <DataTable
                  caption="Productos de la tienda"
                  columns={cols}
                  rows={visible}
                  rowKey={(p) => p.id}
                  onRowClick={edit}
                  selectedKey={draft?.id ?? undefined}
                  empty={
                    <EmptyState
                      title={status === 'loading' ? 'Cargando productos…' : q ? 'Sin resultados' : 'Aún no hay productos'}
                      description={status === 'loading' ? undefined : q ? 'Prueba con otro nombre o SKU.' : 'Crea el primero con «Nuevo producto».'}
                    />
                  }
                />
              </div>
            }
            aside={
              draft && (
                <Panel title={draft.id ? 'Editar producto' : 'Nuevo producto'} id="editor">
                  <div>
                    <span className={styles.muted}>Foto de portada</span>
                    <div className={styles.photos}>
                      <div className={styles.photo}><Photo src={photo} alt={draft.name || 'Producto'} /></div>
                    </div>
                  </div>
                  <UploadDrop
                    title={photo ? 'Cambiar foto' : 'Agregar foto'}
                    hint="JPG, PNG o WebP · hasta 5 MB"
                    accept={IMAGE_ACCEPT}
                    onFiles={pickImage}
                  />
                  {pendingImage && (
                    <p className={styles.muted}>
                      {draft.id ? 'Foto lista. Súbela o guarda los cambios.' : 'La foto se sube al crear el producto.'}
                    </p>
                  )}
                  {pendingImage && draft.id && (
                    <Button variant="secondary" loading={uploading} disabled={saving} onClick={() => void onUploadOnly()}>
                      Subir foto
                    </Button>
                  )}

                  {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}

                  <TextField
                    label="Nombre"
                    value={draft.name}
                    maxLength={200}
                    onChange={(e) => {
                      const name = e.target.value;
                      setDraft({ ...draft, name, sku: skuTouched ? draft.sku : suggestSku(name) });
                    }}
                  />
                  <TextField
                    label="SKU"
                    hint="Código interno único. No se repite entre productos."
                    value={draft.sku}
                    maxLength={80}
                    onChange={(e) => { setSkuTouched(true); setDraft({ ...draft, sku: e.target.value.toUpperCase() }); }}
                  />
                  <TextareaField
                    label="Descripción"
                    rows={3}
                    maxLength={4000}
                    value={draft.description}
                    onChange={(e) => setDraft({ ...draft, description: e.target.value })}
                  />
                  <div className={styles.grid2}>
                    <TextField
                      label="Precio (MXN)"
                      inputMode="decimal"
                      placeholder="450"
                      value={draft.price}
                      onChange={(e) => setDraft({ ...draft, price: e.target.value.replace(/[^\d.,]/g, '') })}
                    />
                    <SelectField
                      label="Categoría"
                      value={String(draft.category)}
                      onChange={(e) => setDraft({ ...draft, category: Number(e.target.value) as StoreCategory })}
                      options={STORE_CATEGORIES.map((c) => ({ value: String(c.value), label: c.label }))}
                    />
                  </div>

                  <div className={styles.actions}>
                    <Button size="lg" loading={busy} onClick={() => void onSave()}>
                      {draft.id ? 'Guardar cambios' : 'Crear producto'}
                    </Button>
                  </div>

                  {current && (
                    <>
                      <Switch
                        label="Visible en la tienda"
                        showLabel
                        checked={current.active}
                        disabled={toggling}
                        onChange={(active) => void onToggle(active)}
                      />
                      <div>
                        <span className={styles.muted}>Existencias</span>
                        <p>
                          <strong>{current.stock} pzas.</strong>
                          {current.minStock != null && <span className={styles.muted}> · mínimo {current.minStock}</span>}
                        </p>
                        {entryMsg && <FormAlert tone={entryMsg.tone}>{entryMsg.text}</FormAlert>}
                        <div className={styles.grid2}>
                          <TextField
                            label="Piezas que entran"
                            inputMode="numeric"
                            placeholder="10"
                            value={entry.qty}
                            onChange={(e) => setEntry({ ...entry, qty: e.target.value.replace(/\D/g, '') })}
                          />
                          <TextField label="Motivo" value={entry.reason} maxLength={500} onChange={(e) => setEntry({ ...entry, reason: e.target.value })} />
                        </div>
                        <Button variant="secondary" fullWidth loading={adding} onClick={() => void onAddStock()}>
                          Registrar entrada
                        </Button>
                        <p className={styles.muted}>
                          Las existencias son por producto: la API aún no maneja tallas. Salidas y ajustes, en Inventario.
                        </p>
                      </div>
                      <p className={styles.muted}>Los productos no se borran (se conservan en el historial de pedidos): ocúltalos de la tienda.</p>
                    </>
                  )}
                </Panel>
              )
            }
          />
        )}
      </AdminBody>
    </>
  );
}
