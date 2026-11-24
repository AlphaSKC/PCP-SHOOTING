import { useEffect, useState } from 'react';
import { Button, EmptyState, FormAlert, TextareaField, TextField } from '@/components/ui';
import { Panel, Section } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import { guardarContenido, leerContenidoGestion, type ContenidoGestion, type ContenidoSitio } from './api';
import { useContenidoSitio } from './ContenidoProvider';
import styles from './AdminTextos.module.css';

type Campo = { key: keyof ContenidoSitio; label: string; max: number; multilinea?: boolean; opcional?: boolean; type?: string };
const grupos: { titulo: string; campos: Campo[] }[] = [
  { titulo: 'Sobre nosotros', campos: [
    { key: 'nosotrosTitulo', label: 'Título principal', max: 300 }, { key: 'nosotrosTexto', label: 'Texto principal', max: 2000, multilinea: true },
    { key: 'seguridadTitulo', label: 'Título de seguridad', max: 150 }, { key: 'seguridadTexto', label: 'Texto de seguridad', max: 2000, multilinea: true },
    { key: 'testimonio', label: 'Testimonio (opcional)', max: 1000, multilinea: true, opcional: true }, { key: 'autorTestimonio', label: 'Autor del testimonio (opcional)', max: 150, opcional: true },
    { key: 'descripcionPie', label: 'Descripción del pie del sitio', max: 500, multilinea: true },
  ] },
  { titulo: 'Contacto y ubicación', campos: [
    { key: 'direccion', label: 'Dirección', max: 300 }, { key: 'horarioPublico', label: 'Horario informativo', max: 300 },
    { key: 'telefono', label: 'Teléfono mostrado', max: 40 }, { key: 'telefonoMarcacion', label: 'Número para llamadas (con + y código de país)', max: 16, type: 'tel' },
    { key: 'whatsAppNumero', label: 'WhatsApp (código de país y número, solo dígitos)', max: 15 }, { key: 'correo', label: 'Correo público de contacto', max: 254, type: 'email' },
    { key: 'ubicacionMapa', label: 'Lugar o dirección para buscar en Google Maps', max: 300 },
  ] },
  { titulo: 'Redes sociales', campos: [
    { key: 'instagramUrl', label: 'Instagram (opcional, HTTPS)', max: 2048, opcional: true, type: 'url' },
    { key: 'facebookUrl', label: 'Facebook (opcional, HTTPS)', max: 2048, opcional: true, type: 'url' },
    { key: 'tikTokUrl', label: 'TikTok (opcional, HTTPS)', max: 2048, opcional: true, type: 'url' },
    { key: 'youTubeUrl', label: 'YouTube (opcional, HTTPS)', max: 2048, opcional: true, type: 'url' },
  ] },
];

export function AdminTextos() {
  const [datos, setDatos] = useState<ContenidoGestion | null>(null); const [error, setError] = useState(''); const [mensaje, setMensaje] = useState('');
  const [busy, setBusy] = useState(false); const [cargando, setCargando] = useState(true); const [revision, setRevision] = useState(0);
  const { actualizar } = useContenidoSitio();
  useEffect(() => { let viva = true; setCargando(true); leerContenidoGestion().then(d => { if (viva) { setDatos(d); setError(''); } }).catch(e => { if (viva) setError(e instanceof Error ? e.message : 'No pudimos cargar los textos.'); }).finally(() => { if (viva) setCargando(false); }); return () => { viva = false; }; }, [revision]);
  function cambiar(key: keyof ContenidoSitio, valor: string, opcional = false) { setDatos(d => d ? { ...d, contenido: { ...d.contenido, [key]: opcional && !valor.trim() ? null : valor } } : d); setMensaje(''); }
  async function guardar() {
    if (!datos || busy) return; setBusy(true); setError(''); setMensaje('');
    try { const nuevo = await guardarContenido(datos); setDatos(nuevo); actualizar(nuevo.contenido); setMensaje('Contenido publicado. Los cambios ya están disponibles en el sitio.'); }
    catch (e) { setError(e instanceof ApiError && e.status === 409 ? 'Otra persona modificó el contenido. Conservamos tu borrador; copia lo que necesites y pulsa Recargar para revisar la versión actual.' : e instanceof Error ? e.message : 'No pudimos guardar. Intenta de nuevo.'); }
    finally { setBusy(false); }
  }
  return <Section title="Páginas y textos" actions={<Button variant="secondary" disabled={busy || cargando} onClick={() => { setMensaje(''); setRevision(n => n + 1); }}>Recargar</Button>}>
    <p>Guardar publica los cambios. El horario aquí es informativo: los turnos de reservas se administran en Configuración.</p>
    {error && <FormAlert tone="error">{error}</FormAlert>}{mensaje && <FormAlert tone="success">{mensaje}</FormAlert>}
    {cargando ? <p role="status">Cargando contenido…</p> : datos ? <form className={styles.form} onSubmit={e => { e.preventDefault(); void guardar(); }}>
      {grupos.map(g => <Panel key={g.titulo} title={g.titulo}><div className={styles.fields}>{g.campos.map(c => c.multilinea
        ? <TextareaField key={c.key} label={c.label} rows={4} maxLength={c.max} required={!c.opcional} disabled={busy} value={datos.contenido[c.key] ?? ''} onChange={e => cambiar(c.key, e.target.value, c.opcional)} />
        : <TextField key={c.key} label={c.label} type={c.type ?? 'text'} maxLength={c.max} required={!c.opcional} disabled={busy} value={datos.contenido[c.key] ?? ''} onChange={e => cambiar(c.key, e.target.value, c.opcional)} />)}</div></Panel>)}
      <p>Para ocultar el testimonio, deja vacíos el texto y su autor. Deja una red vacía para retirar su enlace.</p>
      <div><Button type="submit" disabled={busy}>{busy ? 'Guardando…' : 'Guardar y publicar'}</Button></div>
    </form> : <EmptyState title="No se pudo cargar el contenido" description="Pulsa Recargar para intentarlo nuevamente." />}
  </Section>;
}
