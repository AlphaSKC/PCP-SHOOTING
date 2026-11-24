import { useEffect, useState } from 'react';
import { AdminPageHeader } from '@/components/layout/AdminPageHeader';
import { Button, ButtonAnchor, EmptyState, FormAlert, SelectField, TextareaField, TextField } from '@/components/ui';
import { AdminBody, Panel, Section } from '@/features/admin/ui';
import { when } from '@/features/admin/format';
import { CONTACT_REASONS } from '@/features/contacto/schemas';
import { actualizarSolicitud, consultarSolicitud, listarSolicitudes, type ContactoDetalle, type ContactoResumen, type EstadoContacto } from '@/features/contacto/adminApi';
import { ApiError } from '@/lib/api';
import styles from './AdminContactoPage.module.css';

const estados = [{ value: 'Pendiente', label: 'Pendiente' }, { value: 'Atendida', label: 'Atendida' }, { value: 'Archivada', label: 'Archivada' }];
const motivoLabel = (value: string) => CONTACT_REASONS.find(r => r.value === value)?.label ?? value;
const errorText = (e: unknown) => e instanceof Error ? e.message : 'No pudimos completar la operación. Intenta de nuevo.';

export default function AdminContactoPage() {
  const [lista, setLista] = useState<ContactoResumen[]>([]); const [total, setTotal] = useState(0); const [pagina, setPagina] = useState(1);
  const [estadoFiltro, setEstadoFiltro] = useState('Pendiente'); const [motivo, setMotivo] = useState(''); const [busqueda, setBusqueda] = useState(''); const [buscar, setBuscar] = useState('');
  const [cargando, setCargando] = useState(true); const [errorLista, setErrorLista] = useState(''); const [revision, setRevision] = useState(0);
  const [id, setId] = useState(''); const [detalle, setDetalle] = useState<ContactoDetalle | null>(null); const [detalleRevision, setDetalleRevision] = useState(0);
  const [estado, setEstado] = useState<EstadoContacto>('Pendiente'); const [notas, setNotas] = useState('');
  const [errorDetalle, setErrorDetalle] = useState(''); const [mensaje, setMensaje] = useState(''); const [guardando, setGuardando] = useState(false); const [cargandoDetalle, setCargandoDetalle] = useState(false);
  useEffect(() => {
    let vivo = true; setCargando(true); setErrorLista('');
    listarSolicitudes({ pagina, tamanoPagina: 20, estado: estadoFiltro, motivo, busqueda }).then(p => { if (vivo) { setLista(p.elementos); setTotal(p.total); } }).catch(e => { if (vivo) setErrorLista(errorText(e)); }).finally(() => { if (vivo) setCargando(false); });
    return () => { vivo = false; };
  }, [pagina, estadoFiltro, motivo, busqueda, revision]);
  useEffect(() => {
    let vivo = true; setDetalle(null); setErrorDetalle(''); setMensaje('');
    if (id) { setCargandoDetalle(true); consultarSolicitud(id).then(d => { if (vivo) { setDetalle(d); setEstado(d.estado); setNotas(d.notasInternas ?? ''); } }).catch(e => { if (vivo) setErrorDetalle(errorText(e)); }).finally(() => { if (vivo) setCargandoDetalle(false); }); }
    return () => { vivo = false; };
  }, [id, detalleRevision]);
  async function guardar() {
    if (!detalle || guardando) return; setGuardando(true); setErrorDetalle(''); setMensaje('');
    try { const nuevo = await actualizarSolicitud(detalle.id, estado, notas, detalle.version); setDetalle(nuevo); setNotas(nuevo.notasInternas ?? ''); setMensaje('Seguimiento guardado.'); setRevision(n => n + 1); }
    catch (e) { setErrorDetalle(e instanceof ApiError && e.status === 409 ? 'La solicitud cambió. Conservamos tus notas; copia lo necesario y pulsa Recargar solicitud para revisar la versión actual.' : errorText(e)); }
    finally { setGuardando(false); }
  }
  return <>
    <AdminPageHeader title="Mensajes de contacto" subtitle="Solicitudes recibidas desde el sitio y seguimiento interno" />
    <AdminBody>
      <Section title="Bandeja" actions={<Button variant="secondary" disabled={cargando || guardando} onClick={() => setRevision(n => n + 1)}>Recargar bandeja</Button>}>
        <form className={styles.filters} onSubmit={e => { e.preventDefault(); setBusqueda(buscar.trim()); setPagina(1); }}>
          <SelectField label="Estado" value={estadoFiltro} options={[{ value: '', label: 'Todos' }, ...estados]} onChange={e => { setEstadoFiltro(e.target.value); setPagina(1); }} />
          <SelectField label="Motivo" value={motivo} options={[{ value: '', label: 'Todos' }, ...CONTACT_REASONS]} onChange={e => { setMotivo(e.target.value); setPagina(1); }} />
          <TextField label="Buscar nombre o correo" value={buscar} maxLength={150} onChange={e => setBuscar(e.target.value)} />
          <Button type="submit">Buscar</Button>
        </form>
        {errorLista && <FormAlert tone="error">{errorLista}</FormAlert>}
        <div className={styles.layout}>
          <Panel title="Solicitudes">
            {cargando && <p role="status">Cargando solicitudes…</p>}
            {!cargando && !errorLista && lista.length === 0 && <EmptyState title="No hay solicitudes con estos filtros" description="Los mensajes nuevos aparecerán aquí." />}
            <ul className={styles.list}>{lista.map(s => <li key={s.id}><button type="button" className={styles.item} aria-pressed={s.id === id} disabled={guardando || cargando} onClick={() => setId(s.id)}><strong>{s.nombre}</strong><span>{motivoLabel(s.motivo)} · {s.estado}</span><span>{s.email}</span><time dateTime={s.creadoEnUtc}>{when(s.creadoEnUtc)}</time></button></li>)}</ul>
            <div className={styles.actions}><Button variant="secondary" disabled={pagina === 1 || cargando || guardando} onClick={() => setPagina(p => p - 1)}>Anterior</Button><span>Página {pagina} · {total} solicitudes</span><Button variant="secondary" disabled={pagina * 20 >= total || cargando || guardando} onClick={() => setPagina(p => p + 1)}>Siguiente</Button></div>
          </Panel>
          <Panel title="Detalle y seguimiento">
            {errorDetalle && <FormAlert tone="error">{errorDetalle}</FormAlert>}{!detalle && id && errorDetalle && <Button variant="secondary" disabled={cargandoDetalle || guardando} onClick={() => setDetalleRevision(n => n + 1)}>Reintentar consulta</Button>}{mensaje && <FormAlert tone="success">{mensaje}</FormAlert>}
            {cargandoDetalle ? <p role="status">Cargando mensaje…</p> : detalle ? <>
              <h3>{detalle.nombre}</h3><p>{motivoLabel(detalle.motivo)} · {when(detalle.creadoEnUtc)}</p>
              <p>{detalle.email}{detalle.telefono ? ` · ${detalle.telefono}` : ''}</p>
              <p className={styles.message}>{detalle.mensaje}</p>
              <p className={styles.meta}>Privacidad aceptada: {when(detalle.privacidadAceptadaEnUtc)}</p>
              <div className={styles.actions}><ButtonAnchor href={`mailto:${encodeURIComponent(detalle.email)}`} variant="secondary">Responder por correo</ButtonAnchor>{detalle.telefono && <ButtonAnchor href={`tel:${detalle.telefono}`} variant="secondary">Llamar</ButtonAnchor>}</div>
              <form className={styles.editor} onSubmit={e => { e.preventDefault(); void guardar(); }}>
                <SelectField label="Estado de atención" value={estado} options={estados} disabled={guardando} onChange={e => setEstado(e.target.value as EstadoContacto)} />
                <TextareaField label="Notas internas" rows={6} maxLength={4000} value={notas} disabled={guardando} onChange={e => setNotas(e.target.value)} />
                <p>Guardar el seguimiento no envía una respuesta al visitante.</p>
                <div className={styles.actions}><Button type="submit" disabled={guardando}>{guardando ? 'Guardando…' : 'Guardar seguimiento'}</Button><Button variant="secondary" disabled={guardando} onClick={() => setDetalleRevision(n => n + 1)}>Recargar solicitud</Button></div>
              </form>
            </> : !errorDetalle && <EmptyState title="Selecciona una solicitud" description="Aquí podrás leer el mensaje y registrar su atención." />}
          </Panel>
        </div>
      </Section>
    </AdminBody>
  </>;
}
