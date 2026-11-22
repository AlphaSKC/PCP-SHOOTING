import { useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, DataTable, FormAlert, SelectField, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { IconClose, IconPlus } from '@/components/icons/icons';
import { money } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminPackage } from '@/features/admin/seed';
import { newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import { DATA_SOURCE } from '@/lib/api';
import AdminPaquetesApi from './AdminPaquetesApi';
import styles from './AdminShared.module.css';

const emptyPkg = (): AdminPackage => ({ id: '', name: '', description: '', includes: '', price: 0, memberPrice: 0, rentalAvailable: true, active: false, consumables: [] });

/**
 * Paquetes y actividades de tiro (CLAUDE.md §7, sin diseño).
 * Regla: se venden por actividad o tirada, NO por duración. Cada paquete define sus consumibles
 * por persona, que se descuentan del inventario al registrar la llegada.
 */
export default function AdminPaquetesPage() {
  return DATA_SOURCE === 'api' ? <AdminPaquetesApi /> : <AdminPaquetesDemo />;
}

function AdminPaquetesDemo() {
  const packages = useAdmin((d) => d.packages);
  const inventory = useAdmin((d) => d.inventory);
  const [editing, setEditing] = useState<AdminPackage | null>(packages[0] ?? null);
  const [msg, setMsg] = useState<string | null>(null);
  const consumables = inventory.filter((i) => i.category === 'consumible');
  const itemName = (id: string) => inventory.find((i) => i.id === id)?.name ?? id;

  const cols: Column<AdminPackage>[] = [
    { key: 'name', header: 'Paquete', primary: true, render: (p) => <span className={styles.stack}><strong>{p.name}</strong><span>{p.includes}</span></span> },
    { key: 'price', header: 'Precio por persona', strong: true, render: (p) => money(p.price) },
    { key: 'member', header: 'Precio socio', render: (p) => money(p.memberPrice) },
    { key: 'cons', header: 'Consumibles', hideOnMobile: true, render: (p) => p.consumables.map((c) => `${itemName(c.itemId)} ×${c.perPerson}`).join(', ') || '—' },
    { key: 'status', header: 'Estado', render: (p) => <StatusBadge status={p.active ? 'activo' : 'pausado'} /> },
  ];

  const save = () => {
    if (!editing) return;
    if (!editing.name.trim() || editing.price <= 0) return setMsg('Escribe nombre y precio.');
    const isNew = !editing.id;
    const p = isNew ? { ...editing, id: newId('PK') } : editing;
    update((d) => ({ ...d, packages: isNew ? [...d.packages, p] : d.packages.map((x) => (x.id === p.id ? p : x)) }));
    setEditing(p);
    setMsg('Paquete guardado.');
  };

  return (
    <>
      <title>Paquetes · Panel PCP</title>
      <AdminPageHeader
        title="Paquetes"
        subtitle="Actividades de tiro: precio, condiciones y consumibles por persona"
        actions={<Button iconStart={<IconPlus size={16} />} onClick={() => { setEditing(emptyPkg()); setMsg(null); revealEditor(); }}>Nuevo paquete</Button>}
      />
      <AdminBody>
        <Split
          asideWidth={400}
          main={
            <div className={styles.tableCard}>
              <DataTable caption="Paquetes" columns={cols} rows={packages} rowKey={(p) => p.id} onRowClick={(p) => { setEditing(p); setMsg(null); revealEditor(); }} selectedKey={editing?.id} />
            </div>
          }
          aside={
            editing && (
              <Panel title={editing.id ? 'Editar paquete' : 'Nuevo paquete'} id="editor">
                {msg && <FormAlert tone={msg.startsWith('Escribe') ? 'error' : 'success'}>{msg}</FormAlert>}
                <TextField label="Nombre" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
                <TextareaField label="Descripción" rows={2} value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} />
                <TextField label="Qué incluye" hint="Se muestra en el sitio en lugar de la duración." value={editing.includes} onChange={(e) => setEditing({ ...editing, includes: e.target.value })} />
                <div className={styles.grid2}>
                  <TextField label="Precio por persona" inputMode="numeric" value={editing.price || ''} onChange={(e) => setEditing({ ...editing, price: Number(e.target.value.replace(/\D/g, '')) })} />
                  <TextField label="Precio socio" inputMode="numeric" value={editing.memberPrice || ''} onChange={(e) => setEditing({ ...editing, memberPrice: Number(e.target.value.replace(/\D/g, '')) })} />
                </div>
                <div>
                  <p className={styles.muted}>Consumibles por persona (se descuentan al registrar la llegada)</p>
                  <ul className={styles.list}>
                    {editing.consumables.map((c, i) => (
                      <li key={i} className={styles.grid2}>
                        <SelectField
                          label="Consumible"
                          hideLabel
                          value={c.itemId}
                          onChange={(e) => setEditing({ ...editing, consumables: editing.consumables.map((x, j) => (j === i ? { ...x, itemId: e.target.value } : x)) })}
                          options={consumables.map((x) => ({ value: x.id, label: x.name }))}
                        />
                        <div className={styles.row}>
                          <TextField
                            label="Cantidad por persona"
                            hideLabel
                            inputMode="numeric"
                            value={c.perPerson}
                            onChange={(e) => setEditing({ ...editing, consumables: editing.consumables.map((x, j) => (j === i ? { ...x, perPerson: Number(e.target.value.replace(/\D/g, '')) } : x)) })}
                          />
                          <Button variant="text" aria-label="Quitar consumible" onClick={() => setEditing({ ...editing, consumables: editing.consumables.filter((_, j) => j !== i) })}>
                            <IconClose size={16} />
                          </Button>
                        </div>
                      </li>
                    ))}
                  </ul>
                  <Button
                    variant="secondary"
                    iconStart={<IconPlus size={16} />}
                    disabled={!consumables.length}
                    onClick={() => setEditing({ ...editing, consumables: [...editing.consumables, { itemId: consumables[0]!.id, perPerson: 1 }] })}
                  >
                    Agregar consumible
                  </Button>
                </div>
                <Switch label="Renta de rifle disponible" showLabel checked={editing.rentalAvailable} onChange={(rentalAvailable) => setEditing({ ...editing, rentalAvailable })} />
                <Switch label="Activo (visible al reservar)" showLabel checked={editing.active} onChange={(active) => setEditing({ ...editing, active })} />
                <Button size="lg" fullWidth onClick={save}>Guardar paquete</Button>
                <p className={styles.muted}>Pendiente con el cliente: consumibles exactos por paquete y condiciones (CLAUDE.md §8).</p>
              </Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
