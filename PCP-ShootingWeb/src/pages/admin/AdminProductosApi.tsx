import { useCallback, useEffect, useMemo, useState } from 'react';
import { DeleteProductButton } from '@/features/admin/DeleteProductButton';
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
  SIZES,
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
import { AdminCategoriasTienda } from './AdminCategoriasTienda';
import { listAdminCategories, type AdminStoreCategory } from '@/features/tienda/categoriasApi';
import { listAdminBrands, type BrandDraft } from '@/features/marcas/api';
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
  // Las playeras nuevas manejan tallas S/M/L/XL por defecto.
  usesSizes: true,
  storeCategoryId: '',
  brandId: '',
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
  usesSizes: p.usesSizes,
  storeCategoryId: p.storeCategoryId ?? '',
  brandId: p.brandId ?? '',
});

const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? e.message : fallback);

/** Productos de la tienda conectados a la API real. */
export default function AdminProductosApi() {
  const [params, setParams] = useSearchParams();
  const [products, setProducts] = useState<ApiAdminProduct[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [tab, setTab] = useState<'tienda' | 'categorias' | 'armas'>('tienda');
  // F24: categorías de la tienda y marcas para el editor. `null` = backend sin F24 (no se envían al guardar).
  const [storeCats, setStoreCats] = useState<AdminStoreCategory[] | null>(null);
  const [brands, setBrands] = useState<BrandDraft[]>([]);
  const [q, setQ] = useState('');

  const [draft, setDraft] = useState<ProductDraft | null>(null);
  const [skuTouched, setSkuTouched] = useState(false);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);
  const [toggling, setToggling] = useState(false);

  // Foto pendiente (en productos nuevos se sube al guardar). El id de operación se conserva en reintentos.
  const [pendingImage, setPendingImage] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [uploadFailed, setUploadFailed] = useState(false);
  const [uploading, setUploading] = useState(false);

  const [entry, setEntry] = useState({ qty: '', reason: 'Entrada de mercancía', size: 'M', op: crypto.randomUUID() });
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

  // Categorías y marcas se recargan al volver de la pestaña «Categorías» (pudieron cambiar).
  useEffect(() => {
    if (tab !== 'tienda') return;
    void listAdminCategories().then(setStoreCats, () => setStoreCats(null));
    void listAdminBrands().then(setBrands, () => setBrands([]));
  }, [tab]);
  const taxonomy = storeCats !== null;
  const catName = (id: string | null) => (id ? storeCats?.find((c) => c.id === id)?.name : undefined);
  const brandName = (id: string | null) => (id ? brands.find((b) => b.id === id)?.name : undefined);

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
    setEntry({ qty: '', reason: 'Entrada de mercancía', size: entry.size, op: crypto.randomUUID() });
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
    if (!draft.name.trim()) return setMsg({ tone: 'error', text: 'Escribe el nombre del producto.' });
    if (!draft.sku.trim()) return setMsg({ tone: 'error', text: 'Escribe el SKU (código interno único).' });
    if (parsePrice(draft.price) == null) return setMsg({ tone: 'error', text: 'Escribe un precio mayor a 0 (ej. 450 o 380.50).' });
    setSaving(true);
    setMsg(null);
    try {
      const id = await saveProduct(draft, taxonomy);
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
      await addStock(current.id, qty, entry.reason.trim(), entry.op, current.usesSizes ? entry.size : null);
      await load();
      setEntry({ qty: '', reason: 'Entrada de mercancía', size: entry.size, op: crypto.randomUUID() });
      setEntryMsg({ tone: 'success', text: `Se agregaron ${qty} pzas.${current.usesSizes ? ` en talla ${entry.size}` : ''}` });
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
    {
      key: 'cat',
      header: 'Categoría',
      hideOnMobile: true,
      render: (p) =>
        taxonomy ? (
          <span className={styles.stack}>
            <strong>{catName(p.storeCategoryId) ?? 'Sin categoría'}</strong>
            <span>{brandName(p.brandId) ?? 'Sin marca'}</span>
          </span>
        ) : (
          categoryLabel(p.category)
        ),
    },
    { key: 'price', header: 'Precio', strong: true, render: (p) => (p.price != null ? money(p.price) : 'Sin precio') },
    {
      key: 'stock',
      header: 'Existencias',
      render: (p) => (
        <span className={styles.stack}>
          <strong>
            {p.stock} pzas.{' '}
            {p.stock === 0 ? <StatusBadge status="agotado" /> : (p.lowStock || p.stock <= LOW) && <StatusBadge status="pocas_piezas" />}
          </strong>
          {p.sizeStock && <span>{p.sizeStock.map((t) => `${t.size} ${t.stock}`).join(' · ')}</span>}
        </span>
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
        subtitle="Tienda, categorías y catálogo de armas"
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
              { value: 'tienda', label: `Tienda (${products.length})` },
              { value: 'categorias', label: 'Categorías' },
              { value: 'armas', label: 'Catálogo de armas' },
            ]}
          />
          {tab === 'tienda' && (
            <TextField label="Buscar producto" placeholder="Nombre, SKU o categoría" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
          )}
        </div>

        {tab === 'categorias' ? (
          <AdminCategoriasTienda />
        ) : tab === 'armas' ? (
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
                  {pendingImage && uploadFailed && (
                    <FormAlert inline>Esta foto todavía NO está guardada: es solo una vista previa. No se verá en la tienda hasta que la subida funcione.</FormAlert>
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
                      label="Tipo de producto"
                      value={String(draft.category)}
                      onChange={(e) => setDraft({ ...draft, category: Number(e.target.value) as StoreCategory })}
                      options={STORE_CATEGORIES.map((c) => ({ value: String(c.value), label: c.label }))}
                    />
                  </div>

                  {taxonomy ? (
                    <div className={styles.grid2}>
                      <SelectField
                        label="Categoría de la tienda"
                        hint={storeCats!.length === 0 ? 'Crea categorías en la pestaña «Categorías».' : undefined}
                        value={draft.storeCategoryId}
                        onChange={(e) => setDraft((d) => d && { ...d, storeCategoryId: e.target.value })}
                        options={[{ value: '', label: 'Sin categoría' }, ...storeCats!.map((c) => ({ value: c.id, label: c.active ? c.name : `${c.name} (oculta)` }))]}
                      />
                      <SelectField
                        label="Marca"
                        hint={brands.length === 0 ? 'Agrega marcas en Contenido → Marcas.' : undefined}
                        value={draft.brandId}
                        onChange={(e) => setDraft((d) => d && { ...d, brandId: e.target.value })}
                        options={[{ value: '', label: 'Sin marca' }, ...brands.filter((b) => b.id).map((b) => ({ value: b.id!, label: b.active ? b.name : `${b.name} (oculta)` }))]}
                      />
                    </div>
                  ) : (
                    <p className={styles.muted}>
                      <strong>Categoría de la tienda y marca:</strong> pendiente de backend (F24). Cuando Kevin lo publique, aquí elegirás en qué botón de la tienda aparece el producto (Rifles, Municiones…) y su marca.
                    </p>
                  )}

                  {draft.category === 2 && (
                    <div>
                      <Switch
                        label="Maneja tallas (S, M, L, XL)"
                        showLabel
                        checked={draft.usesSizes}
                        onChange={(usesSizes) => setDraft({ ...draft, usesSizes })}
                      />
                      <p className={styles.muted}>
                        {draft.id
                          ? 'Para activar o quitar tallas en una playera existente, sus existencias deben estar en 0 y sin pedidos pendientes.'
                          : 'Cada talla lleva sus propias existencias; el precio es el mismo para todas.'}
                      </p>
                    </div>
                  )}

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
                        {current.sizeStock && (
                          <div className={styles.sizes}>
                            {current.sizeStock.map((t) => (
                              <div key={t.size} className={styles.box}>
                                <span>{t.size}</span>
                                <strong>{t.stock}</strong>
                              </div>
                            ))}
                          </div>
                        )}
                        {entryMsg && <FormAlert tone={entryMsg.tone}>{entryMsg.text}</FormAlert>}
                        {current.usesSizes && (
                          <SelectField
                            label="Talla"
                            value={entry.size}
                            onChange={(e) => setEntry({ ...entry, size: e.target.value })}
                            options={SIZES.map((t) => ({ value: t, label: t }))}
                          />
                        )}
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
                          {current.usesSizes ? 'Las existencias se llevan por talla.' : 'Las existencias son por producto.'} Salidas y ajustes, en Inventario.
                        </p>
                      </div>
                      <p className={styles.muted}>Un producto con ventas o movimientos no se puede borrar (se conserva en el historial): ocúltalo de la tienda. Solo los creados por error se pueden eliminar.</p>
                      <DeleteProductButton id={current.id} name={current.name} hideHint="ocúltalo con «Visible en la tienda»" onDeleted={() => { setDraft(null); void load(); }} />
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
