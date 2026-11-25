import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Chip, ChipGroup, EmptyState, FormAlert, StatusBadge, Switch, TextareaField, TextField } from '@/components/ui';
import { IconPlus } from '@/components/icons/icons';
import { revealEditor } from '@/features/admin/scroll';
import { Panel, Section, Split } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import { cx } from '@/lib/format';
import shared from '@/pages/admin/AdminShared.module.css';
import { NoticeStrip } from './NoticeBanner';
import {
  STATE_TONE,
  STATE_LABEL,
  TEXT_MAX,
  TITLE_MAX,
  clubLabel,
  emptyNotice,
  fromClubInput,
  listAdminNotices,
  saveNotice,
  validateNotice,
  type NoticeDraft,
  type NoticeState,
} from './api';
import styles from './Avisos.module.css';

type Msg = { tone: 'error' | 'success'; text: string } | null;
type Filter = 'todos' | NoticeState;
const FILTERS: { id: Filter; label: string }[] = [
  { id: 'todos', label: 'Todos' },
  { id: 'Vigente', label: 'Visibles ahora' },
  { id: 'Programado', label: 'Programados' },
  { id: 'Vencido', label: 'Vencidos' },
  { id: 'Inactivo', label: 'Desactivados' },
];

const errorText = (e: unknown, fallback: string) =>
  e instanceof ApiError ? (e.status === 409 ? 'Alguien más editó este aviso. Recargamos los datos: revisa y vuelve a guardar.' : e.message) : fallback;

/** Cuándo se muestra, en palabras («Desde … hasta …», «Sin fecha de fin»). */
function windowText(d: NoticeDraft): string {
  const s = fromClubInput(d.start);
  const e = fromClubInput(d.end);
  if (!s && !e) return 'Siempre (mientras esté activo)';
  if (s && e) return `Del ${clubLabel(s)} al ${clubLabel(e)}`;
  return s ? `Desde el ${clubLabel(s)} · sin fecha de fin` : `Hasta el ${clubLabel(e!)}`;
}

/** Avisos públicos del sitio (franja superior). Solo Administrador. docs/AVISOS-PUBLICOS.md */
export function AdminAvisosApi() {
  const [notices, setNotices] = useState<NoticeDraft[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [loadError, setLoadError] = useState('');
  const [filter, setFilter] = useState<Filter>('todos');
  const [edit, setEdit] = useState<NoticeDraft | null>(null);
  const [msg, setMsg] = useState<Msg>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const list = await listAdminNotices();
      setNotices(list);
      setStatus('ready');
      return list;
    } catch (e) {
      setLoadError(errorText(e, 'No pudimos cargar los avisos.'));
      setStatus('error');
      return null;
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const visible = useMemo(() => notices.filter((n) => filter === 'todos' || n.state === filter), [notices, filter]);
  const count = (s: NoticeState) => notices.filter((n) => n.state === s).length;
  const nextOrder = notices.length ? Math.max(...notices.map((n) => n.order)) + 1 : 0;

  function open(n: NoticeDraft) {
    setEdit(n);
    setMsg(null);
    revealEditor();
  }

  async function onSave() {
    if (!edit) return;
    const problem = validateNotice(edit);
    if (problem) return setMsg({ tone: 'error', text: problem });
    setBusy(true);
    setMsg(null);
    try {
      const saved = await saveNotice(edit);
      setEdit(saved);
      await load();
      const where =
        saved.state === 'Vigente' ? 'Ya se ve en el sitio.' : saved.state === 'Programado' ? 'Se mostrará cuando llegue la fecha de inicio.' : saved.state === 'Vencido' ? 'Ya venció: no se muestra.' : 'Está desactivado: no se muestra.';
      setMsg({ tone: 'success', text: `Aviso guardado. ${where}` });
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        const list = await load();
        const fresh = list?.find((n) => n.id === edit.id);
        if (fresh) setEdit(fresh);
      }
      setMsg({ tone: 'error', text: errorText(e, 'No pudimos guardar el aviso.') });
    } finally {
      setBusy(false);
    }
  }

  if (status === 'error') return <EmptyState title="No pudimos cargar los avisos" description={loadError} action={<Button onClick={() => void load()}>Reintentar</Button>} />;

  return (
    <Split
      asideWidth={420}
      main={
        <Section title="Avisos del sitio" actions={<Button iconStart={<IconPlus size={16} />} onClick={() => open(emptyNotice(nextOrder))}>Nuevo aviso</Button>}>
          <p className={shared.muted}>
            Mensajes cortos que aparecen en una franja arriba de todas las páginas públicas (cierres, horarios especiales, promociones). Se muestran solo si están activos y dentro de sus fechas.
          </p>
          <ChipGroup label="Estado del aviso">
            {FILTERS.map((f) => (
              <Chip key={f.id} selected={filter === f.id} onClick={() => setFilter(f.id)}>
                {f.label}
                {f.id !== 'todos' && ` (${count(f.id)})`}
              </Chip>
            ))}
          </ChipGroup>
          {status === 'loading' ? (
            <p className={shared.muted}>Cargando avisos…</p>
          ) : visible.length === 0 ? (
            <p className={shared.muted}>{notices.length === 0 ? 'Aún no hay avisos. Crea el primero con «Nuevo aviso».' : 'No hay avisos con este filtro.'}</p>
          ) : (
            <ul className={styles.list}>
              {visible.map((n) => (
                <li key={n.id}>
                  <button type="button" className={cx(styles.item, n.id === edit?.id && styles.itemSelected)} onClick={() => open(n)}>
                    <span className={styles.itemHead}>
                      <strong className={shared.wrapAny}>{n.title || 'Sin título'}</strong>
                      {n.state && <StatusBadge tone={STATE_TONE[n.state]}>{STATE_LABEL[n.state]}</StatusBadge>}
                    </span>
                    <span className={styles.itemText}>{n.text}</span>
                    <span className={shared.muted}>
                      {windowText(n)} · orden {n.order}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Section>
      }
      aside={
        edit ? (
          <Panel title={edit.id ? 'Editar aviso' : 'Nuevo aviso'} id="editor" actions={edit.state ? <StatusBadge tone={STATE_TONE[edit.state]}>{STATE_LABEL[edit.state]}</StatusBadge> : undefined}>
            <span className={shared.muted}>Así se verá en el sitio</span>
            <NoticeStrip notices={[{ id: 'preview', title: edit.title.trim(), text: edit.text.trim() || 'Escribe el texto del aviso…' }]} />
            {msg && <FormAlert tone={msg.tone}>{msg.text}</FormAlert>}
            <TextField label="Título (opcional)" placeholder="Horario especial" maxLength={TITLE_MAX} value={edit.title} onChange={(e) => setEdit({ ...edit, title: e.target.value })} />
            <TextareaField
              label="Texto del aviso"
              rows={4}
              maxLength={TEXT_MAX}
              hint={`${edit.text.length}/${TEXT_MAX} · texto simple, sin formato`}
              value={edit.text}
              onChange={(e) => setEdit({ ...edit, text: e.target.value })}
            />
            <TextField label="Mostrar desde (opcional)" type="datetime-local" value={edit.start} max={edit.end || undefined} onChange={(e) => setEdit({ ...edit, start: e.target.value })} />
            <TextField label="Mostrar hasta (opcional)" type="datetime-local" value={edit.end} min={edit.start || undefined} onChange={(e) => setEdit({ ...edit, end: e.target.value })} />
            <p className={shared.muted}>Hora del club (Quintana Roo). Sin «desde» se muestra de inmediato; sin «hasta» no vence. Al llegar «hasta» deja de mostrarse solo.</p>
            <TextField
              label="Orden"
              type="number"
              min={0}
              step={1}
              hint="0 aparece primero."
              value={String(edit.order)}
              onChange={(e) => setEdit({ ...edit, order: e.target.value === '' ? 0 : Math.trunc(Number(e.target.value)) })}
            />
            <Switch label="Activo" showLabel checked={edit.active} onChange={(active) => setEdit({ ...edit, active })} />
            <Button size="lg" fullWidth loading={busy} onClick={() => void onSave()}>
              {edit.id ? 'Guardar aviso' : 'Crear aviso'}
            </Button>
            <p className={shared.muted}>Los avisos no se borran: desactívalos para ocultarlos (se conserva el historial).</p>
          </Panel>
        ) : (
          <Panel title="Aviso" id="editor">
            <p className={shared.muted}>Elige un aviso de la lista o crea uno nuevo.</p>
          </Panel>
        )
      }
    />
  );
}
