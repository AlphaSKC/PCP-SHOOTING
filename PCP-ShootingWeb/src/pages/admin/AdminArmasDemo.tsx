import { useState } from 'react';
import { Button, DataTable, FormAlert, StatusBadge, Switch, TextField, type Column } from '@/components/ui';
import { money } from '@/features/admin/format';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminRifle } from '@/features/admin/seed';
import { update, useAdmin } from '@/features/admin/store';
import { Panel, Split, UploadDrop } from '@/features/admin/ui';
import styles from './AdminShared.module.css';

/** Catálogo de armas (informativo, "Por cotizar"). Sigue en DEMOSTRACIÓN: la API aún no tiene este catálogo. */
export function AdminArmasDemo() {
  const rifles = useAdmin((d) => d.rifles);
  const [rifle, setRifle] = useState<AdminRifle | null>(rifles[0] ?? null);
  const [msg, setMsg] = useState<string | null>(null);

  const cols: Column<AdminRifle>[] = [
    { key: 'name', header: 'Modelo', primary: true, render: (r) => <strong>{r.name}</strong> },
    { key: 'type', header: 'Tipo', render: (r) => r.type },
    { key: 'price', header: 'Precio', render: (r) => (r.price != null ? money(r.price) : 'Por cotizar') },
    { key: 'vis', header: 'Estado', render: (r) => <StatusBadge status={r.visible ? 'publicado' : 'borrador'} /> },
    {
      key: 'edit',
      header: 'Acción',
      hideOnMobile: true,
      render: (r) => (
        <button type="button" className={styles.linkBtn} onClick={() => select(r)}>
          Editar
        </button>
      ),
    },
  ];

  function select(r: AdminRifle) {
    setRifle(r);
    setMsg(null);
    revealEditor();
  }

  return (
    <Split
      main={
        <div className={styles.tableCard}>
          <DataTable caption="Catálogo de armas" columns={cols} rows={rifles} rowKey={(r) => r.id} onRowClick={select} selectedKey={rifle?.id} />
        </div>
      }
      aside={
        rifle && (
          <Panel title="Editar modelo" id="editor">
            <p className={styles.muted}>El catálogo es informativo: los clientes solicitan cotización.</p>
            {msg && <FormAlert tone="success">{msg}</FormAlert>}
            <UploadDrop title="Foto del modelo" />
            <TextField label="Modelo" value={rifle.name} onChange={(e) => setRifle({ ...rifle, name: e.target.value })} />
            <TextField label="Tipo" value={rifle.type} onChange={(e) => setRifle({ ...rifle, type: e.target.value })} />
            <TextField
              label="Precio (MXN)"
              hint="Déjalo vacío para mostrar «Por cotizar»."
              inputMode="numeric"
              value={rifle.price ?? ''}
              onChange={(e) => setRifle({ ...rifle, price: e.target.value ? Number(e.target.value.replace(/\D/g, '')) : null })}
            />
            <Switch label="Visible en el catálogo" showLabel checked={rifle.visible} onChange={(visible) => setRifle({ ...rifle, visible })} />
            <Button
              size="lg"
              fullWidth
              onClick={() => {
                update((d) => ({ ...d, rifles: d.rifles.map((r) => (r.id === rifle.id ? rifle : r)) }));
                setMsg('Modelo guardado.');
              }}
            >
              Guardar cambios
            </Button>
          </Panel>
        )
      }
    />
  );
}
