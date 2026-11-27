import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, DataTable, FormAlert, Photo, SelectField, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { DATA_SOURCE } from '@/lib/api';
import { IconPlus } from '@/components/icons/icons';
import { money } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminProduct } from '@/features/admin/seed';
import { newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, SegmentedTabs, Split, UploadDrop } from '@/features/admin/ui';
import { AdminArmasDemo } from './AdminArmasDemo';
import AdminProductosApi from './AdminProductosApi';
import styles from './AdminShared.module.css';

const SIZES = ['CH', 'M', 'G', 'XG'];
const CATEGORIES: AdminProduct['category'][] = ['Playera', 'Gorra', 'Taza', 'Stickers'];
const LOW = 5;
const total = (p: AdminProduct) => Object.values(p.stock).reduce((s, n) => s + n, 0);

const emptyProduct = (): AdminProduct => ({ id: '', name: '', category: 'Playera', description: '', price: 0, stock: { CH: 0, M: 0, G: 0, XG: 0 }, active: true });

/** Con VITE_DATA_SOURCE=api la tienda usa la API real; si no, el almacén de demostración. */
export default function AdminProductosPage() {
  return DATA_SOURCE === 'api' ? <AdminProductosApi /> : <AdminProductosDemo />;
}

function AdminProductosDemo() {
  const [params, setParams] = useSearchParams();
  const products = useAdmin((d) => d.products);
  const rifles = useAdmin((d) => d.rifles);
  const [tab, setTab] = useState<'tienda' | 'armas'>('tienda');
  const [q, setQ] = useState('');
  const [editing, setEditing] = useState<AdminProduct | null>(products[0] ?? null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    if (params.get('nuevo')) {
      setTab('tienda');
      setEditing(emptyProduct());
      params.delete('nuevo');
      setParams(params, { replace: true });
    }
  }, [params, setParams]);

  const visible = useMemo(
    () => products.filter((p) => `${p.name} ${p.category}`.toLowerCase().includes(q.toLowerCase())),
    [products, q],
  );

  const productCols: Column<AdminProduct>[] = [
    {
      key: 'name',
      header: 'Producto',
      primary: true,
      render: (p) => (
        <span className={styles.prodCell}>
          <span className={styles.thumb}><Photo alt={p.name} /></span>
          <strong>{p.name}</strong>
        </span>
      ),
    },
    { key: 'cat', header: 'Categoría', render: (p) => p.category, hideOnMobile: true },
    { key: 'price', header: 'Precio', strong: true, render: (p) => money(p.price) },
    {
      key: 'stock',
      header: 'Existencias',
      render: (p) => {
        const t = total(p);
        return (
          <span className={styles.stack}>
            <strong>
              {t} pzas. {t === 0 ? <StatusBadge status="agotado" /> : t <= LOW && <StatusBadge status="pocas_piezas" />}
            </strong>
            <span>{Object.entries(p.stock).map(([k, v]) => `${k} ${v}`).join(' · ')}</span>
          </span>
        );
      },
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

  function edit(p: AdminProduct) {
    setEditing(p);
    setMsg(null);
    revealEditor();
  }

  const saveProduct = () => {
    if (!editing) return;
    if (!editing.name.trim() || editing.price <= 0) return setMsg('Escribe nombre y precio.');
    const isNew = !editing.id;
    const p = isNew ? { ...editing, id: newId('P') } : editing;
    update((d) => ({ ...d, products: isNew ? [...d.products, p] : d.products.map((x) => (x.id === p.id ? p : x)) }));
    setEditing(p);
    setMsg('Cambios guardados.');
  };

  return (
    <>
      <title>Productos · Panel PCP</title>
      <AdminPageHeader
        title="Productos"
        subtitle="Fotos, precios, tallas y existencias de la tienda y del catálogo de armas"
        actions={
          <>
            <ButtonLink to="/tienda" variant="secondary">Ver sitio</ButtonLink>
            <Button iconStart={<IconPlus size={16} />} onClick={() => { setTab('tienda'); setEditing(emptyProduct()); setMsg(null); revealEditor(); }}>
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
              { value: 'armas', label: `Catálogo de armas (${rifles.length})` },
            ]}
          />
          {tab === 'tienda' && (
            <TextField label="Buscar producto" placeholder="Nombre o categoría" value={q} onChange={(e) => setQ(e.target.value)} className={styles.search} />
          )}
        </div>

        {tab === 'tienda' ? (
          <Split
            main={
              <div className={styles.tableCard}>
                <DataTable caption="Productos de la tienda" columns={productCols} rows={visible} rowKey={(p) => p.id} onRowClick={edit} selectedKey={editing?.id} />
              </div>
            }
            aside={
              editing && (
                <Panel title={editing.id ? 'Editar producto' : 'Nuevo producto'} id="editor">
                  <div>
                    <span className={styles.muted}>Fotos (la primera es la portada)</span>
                    <div className={styles.photos}>
                      {[1, 2, 3].map((i) => (
                        <div key={i} className={styles.photo}><Photo alt={`Foto ${i}`} /></div>
                      ))}
                    </div>
                  </div>
                  <UploadDrop title="Agregar fotos" multiple onFiles={(f) => setMsg(`${f.length} foto(s) listas para subir (se guardan al integrar Supabase Storage).`)} />
                  {msg && <FormAlert tone={msg.startsWith('Escribe') ? 'error' : 'success'}>{msg}</FormAlert>}
                  <TextField label="Nombre" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
                  <TextareaField label="Descripción" rows={3} value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} />
                  <div className={styles.grid2}>
                    <TextField label="Precio (MXN)" inputMode="numeric" value={editing.price || ''} onChange={(e) => setEditing({ ...editing, price: Number(e.target.value.replace(/\D/g, '')) })} />
                    <SelectField
                      label="Categoría"
                      value={editing.category}
                      onChange={(e) => {
                        const category = e.target.value as AdminProduct['category'];
                        const sized = category === 'Playera';
                        setEditing({ ...editing, category, stock: sized ? { CH: 0, M: 0, G: 0, XG: 0 } : { Única: total(editing) } });
                      }}
                      options={CATEGORIES.map((c) => ({ value: c, label: c }))}
                    />
                  </div>
                  <div>
                    <span className={styles.muted}>Existencias {editing.category === 'Playera' ? 'por talla' : ''}</span>
                    <div className={styles.sizes}>
                      {(editing.category === 'Playera' ? SIZES : ['Única']).map((s) => (
                        <TextField
                          key={s}
                          label={s}
                          inputMode="numeric"
                          value={String(editing.stock[s] ?? 0)}
                          onChange={(e) => setEditing({ ...editing, stock: { ...editing.stock, [s]: Number(e.target.value.replace(/\D/g, '')) } })}
                        />
                      ))}
                    </div>
                    <p className={styles.muted}>Se descuentan solas con cada venta en línea.</p>
                  </div>
                  <Switch label="Visible en la tienda" showLabel checked={editing.active} onChange={(active) => setEditing({ ...editing, active })} />
                  <div className={styles.actions}>
                    <Button size="lg" onClick={saveProduct}>Guardar cambios</Button>
                    {editing.id && (
                      <Button
                        size="lg"
                        variant="secondary"
                        onClick={() => {
                          if (!window.confirm(`¿Eliminar "${editing.name}"?`)) return;
                          update((d) => ({ ...d, products: d.products.filter((p) => p.id !== editing.id) }));
                          setEditing(null);
                        }}
                      >
                        Eliminar
                      </Button>
                    )}
                  </div>
                </Panel>
              )
            }
          />
        ) : (
          <AdminArmasDemo />
        )}
      </AdminBody>
    </>
  );
}
