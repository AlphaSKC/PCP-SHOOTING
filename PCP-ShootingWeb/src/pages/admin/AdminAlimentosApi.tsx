import { useCallback, useEffect, useMemo, useState } from 'react';
import { DeleteProductButton } from '@/features/admin/DeleteProductButton';
import { Link } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, Chip, ChipGroup, DataTable, EmptyState, FormAlert, KpiCard, KpiGrid, Photo, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { money } from '@/features/admin/format';
import { IMAGE_ACCEPT, IMAGE_MAX_BYTES, addStock, setProductActive, suggestSku, uploadProductImage } from '@/features/admin/productosApi';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, DemoNote, Panel, Split, UploadDrop } from '@/features/admin/ui';
import { emptyFood, listFoodProducts, saveFood, toFoodDraft, validateFood, type FoodDraft, type FoodProduct } from '@/features/alimentos/alimentosApi';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
type Filter = 'todos' | 'carta' | 'agotados' | 'ocultos';
const errorText = (e: unknown, fallback: string) => (e instanceof ApiError ? (e.status === 409 ? 'Ese SKU ya existe o el producto cambió. Recargamos los datos: revisa y vuelve a guardar.' : e.message) : fallback);

/** Carta de alimentos y bebidas (productos categoría 1). Administrador y Recepción. docs/ALIMENTOS.md */
export default function AdminAlimentosApi() {
  const [items, setItems] = useState<FoodProduct[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<Filter>('todos');
  const [q, setQ] = useState('');
  const [draft, setDraft] = useState<FoodDraft | null>(null);
  const [skuTouched, setSkuTouched] = useState(true);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState<'save' | 'active' | 'photo' | 'stock' | null>(null);
  const [photo, setPhoto] = useState<{ file: File; preview: string; op: string } | null>(null);
  const [entry, setEntry] = useState({ qty: '', reason: 'Compra / reposición', op: crypto.randomUUID() });

  const load = useCallback(async () => {
    try {
      const list = await listFoodProducts();
      setItems(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar la carta.'));
      setStatus('error');
      return null;
    }
  }, []);
  useEffect(() => void load(), [load]);

  const preview = photo?.preview;
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

  const current = draft?.id ? items.find((i) => i.id === draft.id) ?? null : null;

  const visible = useMemo(() => {
    const t = q.trim().toLowerCase();
    return items.filter((i) => {
      const f = filter === 'todos' || (filter === 'carta' ? i.active : filter === 'ocultos' ? !i.active : i.active && i.stock <= 0);
      return f && (!t || `${i.name} ${i.sku}`.toLowerCase().includes(t));
    });
  }, [items, filter, q]);

  function open(d: FoodDraft, isNew = false) {
    setDraft(d);
    setSkuTouched(!isNew);
    setMsg(null);
    setPhoto(null);
    setEntry({ qty: '', reason: 'Compra / reposición', op: crypto.randomUUID() });
    revealEditor();
  }

  async function onSave() {
    if (!draft) return;
    const problem = validateFood(draft);
    if (problem) return setMsg({ tone: 'error', text: problem });
    setBusy('save');
    setMsg(null);
    try {
      const id = await saveFood(draft);
      const list = await load();
      const fresh = list?.find((i) => i.id === id);
      if (fresh) setDraft(toFoodDraft(fresh));
      setSkuTouched(true);
      setMsg({ tone: 'success', text: draft.id ? 'Cambios guardados.' : 'Producto creado. Ya aparece en la carta (registra existencias para que no salga agotado).' });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409 && draft.id) await load();
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el producto.') });
    } finally {
      setBusy(null);
    }
  }

  async function onToggle(active: boolean) {
    if (!current) return;
    setBusy('active');
    setMsg(null);
    try {
      await setProductActive(current.id, active);
      await load();
      setMsg({ tone: 'success', text: active ? 'Visible en la carta.' : 'Oculto de la carta.' });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos cambiar la visibilidad.') });
    } finally {
      setBusy(null);
    }
  }

  function pickPhoto(files: File[]) {
    if (busy !== null) return setMsg({ tone: 'error', text: 'Espera a que termine la acción en curso y vuelve a elegir la foto.' });
    const f = files[0];
    if (!f || !current) return;
    if (!IMAGE_ACCEPT.split(',').includes(f.type)) return setMsg({ tone: 'error', text: 'La foto debe ser JPG, PNG o WebP.' });
    if (f.size > IMAGE_MAX_BYTES) return setMsg({ tone: 'error', text: 'La foto pesa más de 5 MB.' });
    const next = { file: f, preview: URL.createObjectURL(f), op: crypto.randomUUID() };
    setPhoto(next);
    void uploadPhoto(next);
  }

  async function uploadPhoto(p: { file: File; preview: string; op: string }) {
    if (!current) return;
    setBusy('photo');
    setMsg(null);
    try {
      const url = await uploadProductImage(current.id, p.file, p.op);
      setDraft((d) => (d && d.id === current.id ? { ...d, imageUrl: url } : d));
      await load();
      setPhoto(null);
      setMsg({ tone: 'success', text: 'Foto subida.' });
    } catch (e) {
      setMsg({
        tone: 'error',
        text: e instanceof ApiError && e.status === 503 ? 'Subir fotos todavía no está activo en el servidor (falta configurar el almacenamiento). Puedes intentarlo de nuevo más tarde.' : errorText(e, 'No pudimos subir la foto.'),
      });
    } finally {
      setBusy(null);
    }
  }

  async function onEntry() {
    if (!current) return;
    const qty = Number(entry.qty);
    if (!Number.isInteger(qty) || qty <= 0) return setMsg({ tone: 'error', text: 'Escribe cuántas piezas entran (número entero).' });
    if (!entry.reason.trim()) return setMsg({ tone: 'error', text: 'Escribe el motivo de la entrada.' });
    setBusy('stock');
    setMsg(null);
    try {
      await addStock(current.id, qty, entry.reason.trim(), entry.op);
      await load();
      setEntry({ qty: '', reason: entry.reason, op: crypto.randomUUID() });
      setMsg({ tone: 'success', text: `Entrada registrada: +${qty}.` });
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos registrar la entrada.') });
    } finally {
      setBusy(null);
    }
  }

  const cols: Column<FoodProduct>[] = [
    {
      key: 'name',
      header: 'Producto',
      primary: true,
      render: (i) => (
        <span className={styles.stack}>
          <strong>{i.name}</strong>
          <span>{i.sku}</span>
        </span>
      ),
    },
    { key: 'price', header: 'Precio', strong: true, render: (i) => (i.price == null ? '—' : money(i.price)) },
    { key: 'stock', header: 'Existencia', render: (i) => i.stock },
    {
      key: 'status',
      header: 'En carta',
      render: (i) =>
        !i.active ? <StatusBadge status="pausado" /> : i.stock <= 0 ? <StatusBadge status="agotado" /> : i.low ? <StatusBadge status="bajo_minimo" /> : <StatusBadge tone="success">Disponible</StatusBadge>,
    },
  ];

  const inMenu = items.filter((i) => i.active);
  const image = photo?.preview ?? draft?.imageUrl ?? null;

  return (
    <>
      <title>Alimentos · Panel PCP</title>
      <AdminPageHeader
        title="Alimentos y bebidas"
        subtitle="Carta del club: productos, precios, fotos y existencias"
        actions={
          <>
            <ButtonLink to="/alimentos" variant="secondary" target="_blank">
              Ver carta
            </ButtonLink>
            <Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyFood(), true)}>
              Nuevo producto
            </Button>
          </>
        }
      />
      <AdminBody>
        <DemoNote>
          La carta ya es real. Pendiente de definir con el cliente (OQ-05): pedido en línea, pago, preparación y entrega. Por ahora se pide y paga en barra; las ventas pagadas en Pedidos aparecen en Reportes → Alimentos.
        </DemoNote>
        {status === 'error' ? (
          <EmptyState title="No pudimos cargar la carta" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <>
            <KpiGrid>
              <KpiCard label="En carta" value={String(inMenu.length)} hint="Visibles en el sitio" highlight />
              <KpiCard label="Agotados" value={String(inMenu.filter((i) => i.stock <= 0).length)} hint="Se muestran como agotados" />
              <KpiCard label="Ocultos" value={String(items.length - inMenu.length)} hint="No aparecen en la carta" />
            </KpiGrid>
            <Split
              asideWidth={420}
              main={
                <>
                  <div className={styles.toolbar}>
                    <ChipGroup label="Filtrar">
                      <Chip selected={filter === 'todos'} onClick={() => setFilter('todos')}>Todos ({items.length})</Chip>
                      <Chip selected={filter === 'carta'} onClick={() => setFilter('carta')}>En carta</Chip>
                      <Chip selected={filter === 'agotados'} onClick={() => setFilter('agotados')}>Agotados</Chip>
                      <Chip selected={filter === 'ocultos'} onClick={() => setFilter('ocultos')}>Ocultos</Chip>
                    </ChipGroup>
                    <TextField label="Buscar" placeholder="Nombre o SKU" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
                  </div>
                  <div className={styles.tableCard}>
                    <DataTable
                      caption="Alimentos y bebidas"
                      columns={cols}
                      rows={visible}
                      rowKey={(i) => i.id}
                      onRowClick={(i) => open(toFoodDraft(i))}
                      selectedKey={draft?.id ?? undefined}
                      empty={<p className={styles.empty}>{status === 'loading' ? 'Cargando carta…' : items.length === 0 ? 'Aún no hay productos. Crea el primero con «Nuevo producto».' : 'No hay productos con este filtro.'}</p>}
                    />
                  </div>
                </>
              }
              aside={
                draft ? (
                  <Panel title={draft.id ? 'Editar producto' : 'Nuevo producto'} id="editor" actions={current ? (current.active ? <StatusBadge tone="success">En carta</StatusBadge> : <StatusBadge status="pausado" />) : undefined}>
                    {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                    <TextField
                      label="Nombre"
                      maxLength={200}
                      value={draft.name}
                      onChange={(e) => {
                        const name = e.target.value;
                        setDraft((d) => d && { ...d, name, sku: skuTouched ? d.sku : suggestSku(`ALI ${name}`) });
                      }}
                    />
                    <TextField
                      label="SKU"
                      hint="Código interno único (ej. ALI-AGUA-500)."
                      maxLength={60}
                      value={draft.sku}
                      onChange={(e) => { setSkuTouched(true); setDraft((d) => d && { ...d, sku: e.target.value.toUpperCase() }); }}
                    />
                    <TextareaField label="Descripción (opcional)" rows={2} maxLength={1000} value={draft.description} onChange={(e) => setDraft((d) => d && { ...d, description: e.target.value })} />
                    <TextField label="Precio (MXN)" inputMode="decimal" placeholder="45" value={draft.price} onChange={(e) => setDraft((d) => d && { ...d, price: e.target.value })} />
                    <TextareaField
                      label="Notas (opcional)"
                      hint="Tamaño, ingredientes, alérgenos… Usa - para listas y **negritas**."
                      rows={3}
                      maxLength={4000}
                      value={draft.notes}
                      onChange={(e) => setDraft((d) => d && { ...d, notes: e.target.value })}
                    />
                    <Button size="lg" fullWidth loading={busy === 'save'} disabled={busy !== null} onClick={() => void onSave()}>
                      {draft.id ? 'Guardar cambios' : 'Crear producto'}
                    </Button>

                    {current ? (
                      <>
                        <div className={styles.subsection}>
                          <h3 className={styles.subTitle}>Foto</h3>
                          {image && (
                            <div className={styles.photos}>
                              <div className={styles.photo}>
                                <Photo src={image} alt={draft.name} />
                              </div>
                            </div>
                          )}
                          <UploadDrop title={busy === 'photo' ? 'Subiendo foto…' : image ? 'Cambiar foto' : 'Subir foto'} hint="JPG, PNG o WebP · hasta 5 MB." accept={IMAGE_ACCEPT} onFiles={pickPhoto} />
                          {photo && busy !== 'photo' && (
                            <>
                              <FormAlert inline>Esta foto todavía NO está guardada: es solo una vista previa. No se verá en la carta hasta que la subida funcione.</FormAlert>
                              <Button variant="secondary" onClick={() => void uploadPhoto(photo)}>
                                Reintentar subida
                              </Button>
                            </>
                          )}
                        </div>

                        <Switch label="Visible en la carta" showLabel checked={current.active} disabled={busy !== null} onChange={(a) => void onToggle(a)} />
                        <DeleteProductButton id={current.id} name={current.name} hideHint="apaga «Visible en la carta»" onDeleted={() => { setDraft(null); void load(); }} />

                        <div className={styles.subsection}>
                          <h3 className={styles.subTitle}>Existencias</h3>
                          <p>
                            <strong>{current.stock} pzas.</strong>
                            {current.minStock != null && <span className={styles.muted}> · mínimo {current.minStock}</span>}
                          </p>
                          <div className={styles.grid2}>
                            <TextField label="Piezas que entran" inputMode="numeric" placeholder="24" value={entry.qty} onChange={(e) => setEntry({ ...entry, qty: e.target.value.replace(/\D/g, '') })} />
                            <TextField label="Motivo" value={entry.reason} onChange={(e) => setEntry({ ...entry, reason: e.target.value })} />
                          </div>
                          <Button variant="secondary" fullWidth loading={busy === 'stock'} disabled={busy !== null} onClick={() => void onEntry()}>
                            Registrar entrada
                          </Button>
                          <p className={styles.muted}>
                            Salidas, mermas, ajustes y mínimo de alerta: en <Link to="/admin/inventario">Inventario</Link>.
                          </p>
                        </div>
                      </>
                    ) : (
                      <p className={styles.muted}>Después de crearlo podrás subir la foto y registrar existencias.</p>
                    )}
                  </Panel>
                ) : (
                  <Panel title="Producto" id="editor">
                    <p className={styles.muted}>Elige un producto de la lista o crea uno nuevo.</p>
                  </Panel>
                )
              }
            />
          </>
        )}
      </AdminBody>
    </>
  );
}
