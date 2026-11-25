import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonLink, DataTable, FormAlert, Photo, SelectField, StatusBadge, Switch, TextareaField, TextField, type Column } from '@/components/ui';
import { IconDownload, IconPlus } from '@/components/icons/icons';
import { revealEditor } from '@/features/admin/scroll';
import type { AdminEvent, Enrollee } from '@/features/admin/seed';
import { downloadCsv, newId, update, useAdmin } from '@/features/admin/store';
import { AdminBody, Panel, ProgressBar, Section, Split, UploadDrop } from '@/features/admin/ui';
import { cx } from '@/lib/format';
import styles from './AdminShared.module.css';
import { DATA_SOURCE } from '@/lib/api';
import AdminEventosApi from './AdminEventosApi';

const TYPES = { torneo: 'Torneo', clinica: 'Clínica', social: 'Social' } as const;

const emptyEvent = (): AdminEvent => ({
  id: '', name: '', type: 'torneo', date: '', start: '09:00', end: '13:00', capacity: 20, price: 0, memberPrice: 0,
  description: '', status: 'borrador', blockLines: true, enrollees: [],
});

/** Con la API: eventos informativos (AdminEventosApi). En demo: almacén local con inscritos. */
export default function AdminEventosPage() {
  if (DATA_SOURCE === 'api') return <AdminEventosApi />;
  return <AdminEventosPageDemo />;
}

function AdminEventosPageDemo() {
  const [params, setParams] = useSearchParams();
  const events = useAdmin((d) => d.events);
  const [editing, setEditing] = useState<AdminEvent | null>(events[0] ?? null);
  const [msg, setMsg] = useState<string | null>(null);
  const current = events.find((e) => e.id === editing?.id);

  useEffect(() => {
    if (params.get('nuevo')) {
      setEditing(emptyEvent());
      params.delete('nuevo');
      setParams(params, { replace: true });
    }
  }, [params, setParams]);

  const cols: Column<Enrollee>[] = [
    { key: 'name', header: 'Nombre', primary: true, render: (e) => <strong>{e.name}</strong> },
    { key: 'phone', header: 'Teléfono', render: (e) => e.phone },
    { key: 'paid', header: 'Pago', render: (e) => <StatusBadge status={e.paid ? 'pagado' : 'pendiente'} /> },
    { key: 'waiver', header: 'Carta responsiva', render: (e) => <StatusBadge status={e.waiver} /> },
  ];

  const save = (publish?: boolean) => {
    if (!editing) return;
    if (!editing.name.trim() || !editing.date) return setMsg('Escribe nombre y fecha del evento.');
    const isNew = !editing.id;
    const ev = { ...editing, id: isNew ? newId('EV') : editing.id, status: publish ? 'publicado' : editing.status } as AdminEvent;
    update((d) => ({ ...d, events: isNew ? [...d.events, ev] : d.events.map((x) => (x.id === ev.id ? ev : x)) }));
    setEditing(ev);
    setMsg(publish ? 'Evento publicado en el sitio.' : 'Evento guardado.');
  };

  return (
    <>
      <title>Eventos · Panel PCP</title>
      <AdminPageHeader
        title="Eventos"
        subtitle="Crea eventos, controla el cupo y ve quién se inscribió"
        actions={
          <>
            <ButtonLink to="/eventos" variant="secondary">Ver sitio</ButtonLink>
            <Button iconStart={<IconPlus size={16} />} onClick={() => { setEditing(emptyEvent()); setMsg(null); revealEditor(); }}>
              Nuevo evento
            </Button>
          </>
        }
      />
      <AdminBody>
        <Split
          asideWidth={420}
          main={
            <>
              <ul className={styles.list}>
                {events.map((e) => (
                  <li key={e.id}>
                    <button
                      type="button"
                      className={cx(styles.itemCard, e.id === editing?.id && styles.itemSelected)}
                      onClick={() => { setEditing(e); setMsg(null); revealEditor(); }}
                    >
                      <span className={styles.thumb}><Photo alt={e.name} /></span>
                      <span className={styles.itemBody}>
                        <span className={styles.itemHead}>
                          <strong>{e.name}</strong>
                          <StatusBadge status={e.status} />
                        </span>
                        <span className={styles.itemMeta}>
                          {TYPES[e.type]} · {e.date.split('-').reverse().join('/')} · {e.start}
                        </span>
                        <ProgressBar value={e.enrollees.length} max={e.capacity} label={`Cupo de ${e.name}`} />
                        <span className={styles.itemMeta}>{e.enrollees.length} de {e.capacity} inscritos</span>
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
              {current && (
                <Section
                  title={`Inscritos · ${current.name} (${current.enrollees.length})`}
                  actions={
                    <Button
                      variant="secondary"
                      iconStart={<IconDownload size={16} />}
                      disabled={!current.enrollees.length}
                      onClick={() => downloadCsv(`inscritos-${current.id}.csv`, [['Nombre', 'Teléfono', 'Pago', 'Carta'], ...current.enrollees.map((e) => [e.name, e.phone, e.paid ? 'Pagado' : 'Pendiente', e.waiver])])}
                    >
                      Exportar lista
                    </Button>
                  }
                >
                  <DataTable caption="Inscritos" columns={cols} rows={current.enrollees} rowKey={(e) => e.phone + e.name} empty={<p className={styles.muted}>Aún no hay inscritos.</p>} />
                </Section>
              )}
            </>
          }
          aside={
            editing && (
              <Panel title={editing.id ? 'Editar evento' : 'Nuevo evento'} id="editor">
                <UploadDrop title="Foto del evento" />
                {msg && <FormAlert tone={msg.startsWith('Escribe') ? 'error' : 'success'}>{msg}</FormAlert>}
                <TextField label="Nombre del evento" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
                <div className={styles.grid3}>
                  <SelectField label="Tipo" value={editing.type} onChange={(e) => setEditing({ ...editing, type: e.target.value as AdminEvent['type'] })} options={Object.entries(TYPES).map(([value, label]) => ({ value, label }))} />
                  <TextField label="Fecha" type="date" value={editing.date} onChange={(e) => setEditing({ ...editing, date: e.target.value })} />
                  <TextField label="Cupo" inputMode="numeric" value={editing.capacity} onChange={(e) => setEditing({ ...editing, capacity: Number(e.target.value.replace(/\D/g, '')) })} />
                </div>
                <div className={styles.grid2}>
                  <TextField label="Hora de inicio" type="time" value={editing.start} onChange={(e) => setEditing({ ...editing, start: e.target.value })} />
                  <TextField label="Hora de fin" type="time" value={editing.end} onChange={(e) => setEditing({ ...editing, end: e.target.value })} />
                  <TextField label="Precio (MXN)" inputMode="numeric" value={editing.price || ''} onChange={(e) => setEditing({ ...editing, price: Number(e.target.value.replace(/\D/g, '')) })} />
                  <TextField label="Precio socio (MXN)" inputMode="numeric" value={editing.memberPrice || ''} onChange={(e) => setEditing({ ...editing, memberPrice: Number(e.target.value.replace(/\D/g, '')) })} />
                </div>
                <TextareaField label="Descripción" rows={3} value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} />
                <Switch label="Bloquear las líneas de tiro durante el evento" showLabel checked={editing.blockLines} onChange={(blockLines) => setEditing({ ...editing, blockLines })} />
                <Switch label="Publicado en el sitio" showLabel checked={editing.status === 'publicado'} onChange={(on) => setEditing({ ...editing, status: on ? 'publicado' : 'borrador' })} />
                <div className={styles.actions}>
                  <Button size="lg" onClick={() => save()}>Guardar evento</Button>
                  <ButtonLink to="/eventos" size="lg" variant="secondary" target="_blank">Vista previa</ButtonLink>
                </div>
              </Panel>
            )
          }
        />
      </AdminBody>
    </>
  );
}
