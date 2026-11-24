import { useCallback, useEffect, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, DataTable, EmptyState, FormAlert, SelectField, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { IconClose, IconPlus } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { money } from '@/features/admin/format';
import {
  createConsumable,
  emptyPackage,
  listConsumables,
  listPackages,
  parsePrice,
  savePackage,
  suggestCode,
  validatePackage,
  type Consumable,
  type PackageDraft,
} from '@/features/admin/paquetesApi';
import { revealEditor } from '@/features/admin/scroll';
import { AdminBody, Panel, Split } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import styles from './AdminShared.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError
    ? e.status === 409 && /versi/i.test(e.message)
      ? 'Alguien más editó este paquete. Recargamos los datos: revisa y vuelve a guardar.'
      : e.message
    : fallback;

const priceLabel = (text: string) => {
  const n = parsePrice(text);
  return n == null ? 'Pendiente' : money(n);
};

/** Paquetes y consumibles conectados a la API (R02). Recepción solo consulta. */
export default function AdminPaquetesApi() {
  const { user } = useAuth();
  const canEdit = user?.role === 'admin';
  const [packages, setPackages] = useState<PackageDraft[]>([]);
  const [consumables, setConsumables] = useState<Consumable[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [editing, setEditing] = useState<PackageDraft | null>(null);
  const [codeTouched, setCodeTouched] = useState(true);
  const [msg, setMsg] = useState<Msg>(null);
  const [saving, setSaving] = useState(false);
  const [newConsumable, setNewConsumable] = useState('');
  const [creatingConsumable, setCreatingConsumable] = useState(false);

  const load = useCallback(async () => {
    try {
      const [p, c] = await Promise.all([listPackages(), canEdit ? listConsumables() : Promise.resolve([])]);
      setPackages(p);
      setConsumables(c);
      setStatus('ready');
      return p;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los paquetes.'));
      setStatus('error');
      return null;
    }
  }, [canEdit]);

  useEffect(() => {
    void load().then((p) => {
      if (p && p[0]) setEditing((d) => d ?? p[0]!);
    });
  }, [load]);

  function open(p: PackageDraft, isNew = false) {
    setEditing(p);
    setCodeTouched(!isNew);
    setMsg(null);
    revealEditor();
  }

  async function onSave() {
    if (!editing) return;
    const problem = validatePackage(editing);
    if (problem) return setMsg({ tone: 'error', text: problem });
    setSaving(true);
    setMsg(null);
    try {
      const id = await savePackage(editing);
      const list = await load();
      const fresh = list?.find((p) => p.id === id);
      if (fresh) setEditing(fresh);
      setCodeTouched(true);
      setMsg({
        tone: 'success',
        text: fresh?.visible
          ? 'Paquete guardado. Ya se puede elegir al reservar.'
          : fresh?.active
            ? 'Paquete guardado. Está activo pero aún no aparece al reservar: revisa precios y consumibles.'
            : 'Paquete guardado como inactivo (no aparece al reservar).',
      });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        const list = await load();
        const fresh = list?.find((p) => p.id === editing.id);
        if (fresh) setEditing(fresh);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el paquete.') });
    } finally {
      setSaving(false);
    }
  }

  async function onCreateConsumable() {
    if (!editing || !newConsumable.trim()) return;
    setCreatingConsumable(true);
    setMsg(null);
    try {
      const id = await createConsumable(newConsumable);
      const list = await listConsumables();
      setConsumables(list);
      const c = list.find((x) => x.id === id);
      if (c) setEditing({ ...editing, consumables: [...editing.consumables, { productId: c.id, name: c.name, perPerson: '1', active: c.active }] });
      setNewConsumable('');
    } catch (e) {
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos crear el consumible.') });
    } finally {
      setCreatingConsumable(false);
    }
  }

  const stateBadge = (p: PackageDraft) =>
    p.visible ? (
      <StatusBadge tone="success">Visible al reservar</StatusBadge>
    ) : p.active ? (
      <StatusBadge tone="warning">Activo · oculto</StatusBadge>
    ) : (
      <StatusBadge status="pausado" />
    );

  const cols: Column<PackageDraft>[] = [
    {
      key: 'name',
      header: 'Paquete',
      primary: true,
      render: (p) => (
        <span className={styles.stack}>
          <strong>{p.name}</strong>
          <span>{p.code}</span>
        </span>
      ),
    },
    { key: 'price', header: 'Por persona', strong: true, render: (p) => priceLabel(p.price) },
    { key: 'member', header: 'Socio CARTAQ', render: (p) => (p.memberPrice ? priceLabel(p.memberPrice) : 'Precio general') },
    { key: 'cons', header: 'Consumibles', hideOnMobile: true, render: (p) => p.consumables.map((c) => `${c.name} ×${c.perPerson}`).join(', ') || '—' },
    { key: 'status', header: 'Estado', render: stateBadge },
  ];

  const available = consumables.filter((c) => !editing?.consumables.some((x) => x.productId === c.id));
  const setLine = (i: number, patch: Partial<PackageDraft['consumables'][number]>) =>
    editing && setEditing({ ...editing, consumables: editing.consumables.map((c, j) => (j === i ? { ...c, ...patch } : c)) });

  return (
    <>
      <title>Paquetes · Panel PCP</title>
      <AdminPageHeader
        title="Paquetes"
        subtitle="Actividades de tiro: precio por persona, qué incluyen y consumibles"
        actions={
          canEdit && (
            <Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyPackage(), true)}>
              Nuevo paquete
            </Button>
          )
        }
      />
      <AdminBody>
        {status === 'error' ? (
          <EmptyState title="No pudimos cargar los paquetes" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />
        ) : (
          <Split
            asideWidth={420}
            main={
              <div className={styles.tableCard}>
                <DataTable
                  caption="Paquetes"
                  columns={cols}
                  rows={packages}
                  rowKey={(p) => p.id ?? 'nuevo'}
                  onRowClick={(p) => open(p)}
                  selectedKey={editing?.id ?? undefined}
                  empty={
                    <EmptyState
                      title={status === 'loading' ? 'Cargando paquetes…' : 'Aún no hay paquetes'}
                      description={status === 'loading' ? undefined : canEdit ? 'Crea el primero con «Nuevo paquete».' : undefined}
                    />
                  }
                />
              </div>
            }
            aside={
              editing && (
                <Panel title={editing.id ? (canEdit ? 'Editar paquete' : 'Paquete') : 'Nuevo paquete'} id="editor" actions={editing.id ? stateBadge(editing) : undefined}>
                  {!canEdit && <p className={styles.muted}>Recepción puede consultar los paquetes; solo un Administrador los edita.</p>}
                  <fieldset disabled={!canEdit} className={styles.fieldset}>
                    <TextField
                      label="Nombre"
                      maxLength={200}
                      value={editing.name}
                      onChange={(e) => {
                        const name = e.target.value;
                        setEditing({ ...editing, name, code: codeTouched ? editing.code : suggestCode(name) });
                      }}
                    />
                    <TextField
                      label="Código"
                      hint="Único. Letras, números y guiones (ej. TIRADA-BASICA)."
                      maxLength={80}
                      value={editing.code}
                      onChange={(e) => {
                        setCodeTouched(true);
                        setEditing({ ...editing, code: e.target.value.toUpperCase() });
                      }}
                    />
                    <TextareaField label="Descripción" rows={2} maxLength={4000} value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} />
                    <TextareaField
                      label="Qué incluye"
                      hint="Se muestra al reservar en lugar de la duración. Un punto por línea, empezando con «- »."
                      rows={4}
                      maxLength={10000}
                      placeholder={'- 30 diábolos\n- Renta de rifle\n- Acompañamiento del personal'}
                      value={editing.includes}
                      onChange={(e) => setEditing({ ...editing, includes: e.target.value })}
                    />
                    <TextareaField
                      label="Requisitos (opcional)"
                      rows={2}
                      maxLength={10000}
                      placeholder="- Responsiva firmada por participante"
                      value={editing.requirements}
                      onChange={(e) => setEditing({ ...editing, requirements: e.target.value })}
                    />
                    <div className={styles.grid2}>
                      <TextField
                        label="Precio por persona"
                        hint="Vacío = pendiente."
                        inputMode="decimal"
                        value={editing.price}
                        onChange={(e) => setEditing({ ...editing, price: e.target.value.replace(/[^\d.,]/g, '') })}
                      />
                      <TextField
                        label="Precio socio CARTAQ"
                        hint="Vacío = paga el general."
                        inputMode="decimal"
                        value={editing.memberPrice}
                        onChange={(e) => setEditing({ ...editing, memberPrice: e.target.value.replace(/[^\d.,]/g, '') })}
                      />
                    </div>

                    <div>
                      <p className={styles.muted}>Consumibles por persona (se descontarán del inventario al registrar la llegada)</p>
                      <ul className={styles.list}>
                        {editing.consumables.map((c, i) => (
                          <li key={c.productId} className={styles.row}>
                            <span className={styles.stack}>
                              <strong>{c.name}</strong>
                              {!c.active && <span>Inactivo: actívalo en Inventario</span>}
                            </span>
                            <span className={styles.row}>
                              <TextField
                                label={`Cantidad por persona de ${c.name}`}
                                hideLabel
                                inputMode="numeric"
                                value={c.perPerson}
                                onChange={(e) => setLine(i, { perPerson: e.target.value.replace(/\D/g, '') })}
                              />
                              <Button
                                variant="text"
                                aria-label={`Quitar ${c.name}`}
                                onClick={() => setEditing({ ...editing, consumables: editing.consumables.filter((_, j) => j !== i) })}
                              >
                                <IconClose size={16} />
                              </Button>
                            </span>
                          </li>
                        ))}
                      </ul>
                      {available.length > 0 && (
                        <SelectField
                          label="Agregar consumible"
                          value=""
                          placeholder="Elige un consumible…"
                          onChange={(e) => {
                            const c = consumables.find((x) => x.id === e.target.value);
                            if (c) setEditing({ ...editing, consumables: [...editing.consumables, { productId: c.id, name: c.name, perPerson: '1', active: c.active }] });
                          }}
                          options={available.map((c) => ({ value: c.id, label: c.active ? c.name : `${c.name} (inactivo)` }))}
                        />
                      )}
                      <div className={styles.grid2}>
                        <TextField
                          label="Nuevo consumible"
                          placeholder="Ej. Diábolos .22, Blanco de papel"
                          maxLength={200}
                          value={newConsumable}
                          onChange={(e) => setNewConsumable(e.target.value)}
                        />
                        <Button variant="secondary" iconStart={<IconPlus size={16} />} loading={creatingConsumable} disabled={!newConsumable.trim()} onClick={() => void onCreateConsumable()}>
                          Crear y agregar
                        </Button>
                      </div>
                      <p className={styles.muted}>Los consumibles son internos (no se venden). Sus existencias se cargan en Inventario.</p>
                    </div>

                    <Switch
                      label="Activo (se puede elegir al reservar)"
                      showLabel
                      checked={editing.active}
                      disabled={!canEdit}
                      onChange={(active) => setEditing({ ...editing, active })}
                    />
                  </fieldset>

                  {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
                  {canEdit && (
                    <Button size="lg" fullWidth loading={saving} onClick={() => void onSave()}>
                      {editing.id ? 'Guardar paquete' : 'Crear paquete'}
                    </Button>
                  )}
                  <p className={styles.muted}>Pendiente con el cliente: nombres, precios y consumibles exactos de cada paquete.</p>
                </Panel>
              )
            }
          />
        )}
      </AdminBody>
    </>
  );
}
