import { useCallback, useEffect, useRef, useState } from 'react';
import { Button, EmptyState, FormAlert, Photo, Switch, TextareaField, TextField } from '@/components/ui';
import { Panel, Section } from '@/features/admin/ui';
import { ApiError } from '@/lib/api';
import { albumsGestion, crearFoto, fotosGestion, guardarAlbum, guardarFoto, subirFoto, type Album, type Foto } from './api';
import styles from './GaleriaAdmin.module.css';

const errorText = (e: unknown) => e instanceof ApiError ? e.message : 'No pudimos completar la operación. Intenta de nuevo.';
const vacio = (): Album => ({ id: '', nombre: '', descripcion: '', orden: 0, publicado: false, version: '' });
type Carga = { id: string; albumId: string; archivo: File; fotoId?: string; operacion: string; estado: 'pendiente' | 'cargando' | 'error' | 'lista'; error?: string; conflicto?: boolean };

export function AdminGaleria() {
  const [albums, setAlbums] = useState<Album[]>([]); const [totalAlbums, setTotalAlbums] = useState(0); const [paginaAlbums, setPaginaAlbums] = useState(1);
  const [album, setAlbum] = useState<Album>(vacio); const [seleccionado, setSeleccionado] = useState('');
  const [fotos, setFotos] = useState<Foto[]>([]); const [totalFotos, setTotalFotos] = useState(0); const [paginaFotos, setPaginaFotos] = useState(1);
  const [error, setError] = useState(''); const [mensaje, setMensaje] = useState(''); const [ocupado, setOcupado] = useState(false); const bloqueo = useRef(false);
  const [cargando, setCargando] = useState(true); const [revision, setRevision] = useState(0); const [cargas, setCargas] = useState<Carga[]>([]);
  const contextoFotos = useRef('');
  const refrescar = useCallback(() => setRevision(n => n + 1), []);
  useEffect(() => { let viva = true; setCargando(true); albumsGestion(paginaAlbums).then(p => { if (viva) { setAlbums(p.elementos); setTotalAlbums(p.total); } }).catch(e => { if (viva) setError(errorText(e)); }).finally(() => { if (viva) setCargando(false); }); return () => { viva = false; }; }, [paginaAlbums, revision]);
  useEffect(() => { let viva = true; const contexto = `${seleccionado}:${paginaFotos}`; if (contextoFotos.current !== contexto) { contextoFotos.current = contexto; setFotos([]); setTotalFotos(0); } if (seleccionado) fotosGestion(seleccionado, paginaFotos).then(p => { if (viva) { setFotos(p.elementos); setTotalFotos(p.total); } }).catch(e => { if (viva) setError(errorText(e)); }); return () => { viva = false; }; }, [seleccionado, paginaFotos, revision]);
  async function guardar() {
    if (bloqueo.current) return; bloqueo.current = true; setOcupado(true); setError(''); setMensaje('');
    try { const a = await guardarAlbum({ ...album, id: album.id || undefined }); setAlbum(a); setSeleccionado(a.id); setPaginaFotos(1); refrescar(); setMensaje('Álbum guardado.'); }
    catch (e) { setError(errorText(e)); if (e instanceof ApiError && e.status === 409) { refrescar(); setError('El álbum cambió. Selecciónalo de nuevo en la lista para revisar su versión actual antes de guardar.'); } }
    finally { bloqueo.current = false; setOcupado(false); }
  }
  function agregar(files: FileList | null) {
    if (!files || !seleccionado) return;
    setCargas(a => [...a, ...Array.from(files).map(archivo => ({ id: crypto.randomUUID(), albumId: seleccionado, archivo, operacion: crypto.randomUUID(), estado: 'pendiente' as const }))]);
  }
  async function procesar(lista: Carga[]) {
    if (bloqueo.current) return; bloqueo.current = true; setOcupado(true); setError('');
    for (const carga of lista) {
      const actualizar = (datos: Partial<Carga>) => setCargas(a => a.map(c => c.id === carga.id ? { ...c, ...datos } : c));
      actualizar({ estado: 'cargando', error: undefined });
      try {
        if (carga.archivo.size > 5 * 1024 * 1024 || carga.archivo.size === 0 || !/\.(png|jpe?g|webp)$/i.test(carga.archivo.name)) throw new Error('Usa JPG, PNG o WebP de hasta 5 MiB, con contenido válido.');
        let fotoId = carga.fotoId;
        if (!fotoId) { const titulo = carga.archivo.name.replace(/\.[^.]+$/, '').trim() || 'Fotografía'; const foto = await crearFoto(carga.albumId, titulo, carga.id); fotoId = foto.id; actualizar({ fotoId }); }
        const operacion = carga.conflicto ? crypto.randomUUID() : carga.operacion; actualizar({ operacion, conflicto: false });
        await subirFoto(fotoId, carga.archivo, operacion);
        actualizar({ estado: 'lista' });
      } catch (e) { actualizar({ estado: 'error', error: e instanceof ApiError ? e.message : e instanceof Error ? e.message : errorText(e), conflicto: e instanceof ApiError && e.status === 409 }); }
    }
    refrescar(); bloqueo.current = false; setOcupado(false);
  }
  return <Section title="Galería" actions={<Button variant="secondary" disabled={ocupado} onClick={refrescar}>Recargar</Button>}>
    {error && <FormAlert tone="error">{error}</FormAlert>}{mensaje && <FormAlert tone="success">{mensaje}</FormAlert>}
    <div className={styles.layout}>
      <Panel title="Álbumes">
        <Button disabled={ocupado} onClick={() => { setAlbum(vacio()); setSeleccionado(''); setPaginaFotos(1); setError(''); setMensaje(''); }}>Nuevo álbum</Button>
        {cargando && <p role="status">Cargando álbumes…</p>}
        {!cargando && albums.length === 0 && <p>No hay álbumes en esta página.</p>}
        <ul className={styles.list}>{albums.map(a => <li key={a.id}><button className={styles.albumButton} aria-pressed={seleccionado === a.id} disabled={ocupado} onClick={() => { setAlbum(a); setSeleccionado(a.id); setPaginaFotos(1); setError(''); setMensaje(''); }}><strong>{a.nombre}</strong><span>{a.publicado ? 'Publicado' : 'Oculto'} · Orden {a.orden}</span></button></li>)}</ul>
        <div className={styles.actions}><Button variant="secondary" disabled={ocupado || paginaAlbums === 1} onClick={() => setPaginaAlbums(n => n - 1)}>Anterior</Button><span>Página {paginaAlbums}</span><Button variant="secondary" disabled={ocupado || paginaAlbums * 20 >= totalAlbums} onClick={() => setPaginaAlbums(n => n + 1)}>Siguiente</Button></div>
      </Panel>
      <Panel title={album.id ? 'Editar álbum' : 'Crear álbum'}>
        <form className={styles.form} onSubmit={e => { e.preventDefault(); void guardar(); }}>
          <TextField label="Nombre" required maxLength={120} value={album.nombre} disabled={ocupado} onChange={e => setAlbum(a => ({ ...a, nombre: e.target.value }))} />
          <TextareaField label="Descripción" maxLength={500} value={album.descripcion ?? ''} disabled={ocupado} onChange={e => setAlbum(a => ({ ...a, descripcion: e.target.value }))} />
          <TextField label="Orden" type="number" min={0} step={1} max={2147483647} required value={album.orden} disabled={ocupado} onChange={e => setAlbum(a => ({ ...a, orden: Number(e.target.value) }))} />
          <Switch label="Álbum publicado" checked={album.publicado} disabled={ocupado} onChange={v => setAlbum(a => ({ ...a, publicado: v }))} />
          <p>Solo aparecerá cuando tenga fotos publicadas. La primera por orden será su portada.</p>
          <Button type="submit" disabled={ocupado}>{ocupado ? 'Guardando…' : 'Guardar álbum'}</Button>
        </form>
      </Panel>
    </div>
    {seleccionado ? <>
      <Panel title="Subir fotografías">
        <label className={styles.fileLabel}>Seleccionar JPG, PNG o WebP (máximo 5 MiB cada una)<input type="file" accept="image/jpeg,image/png,image/webp" multiple disabled={ocupado} onChange={e => { agregar(e.target.files); e.target.value = ''; }} /></label>
        <p>Las fotos se guardan ocultas. Revisa título y texto alternativo antes de publicarlas.</p>
        <ul className={styles.list}>{cargas.map(c => <li key={c.id} className={styles.job}><span>{c.archivo.name} — {c.estado === 'lista' ? 'Cargada' : c.estado === 'cargando' ? 'Subiendo…' : c.estado === 'error' ? 'No se completó' : 'Pendiente'}</span>{c.error && <p role="alert">{c.error}</p>}{c.estado === 'error' && <Button variant="secondary" disabled={ocupado} onClick={() => void procesar([c])}>{c.conflicto ? 'Reintentar con versión actual' : 'Reintentar carga'}</Button>}</li>)}</ul>
        <Button disabled={ocupado || !cargas.some(c => c.estado === 'pendiente')} onClick={() => void procesar(cargas.filter(c => c.estado === 'pendiente'))}>{ocupado ? 'Procesando archivos…' : 'Subir seleccionadas'}</Button>
      </Panel>
      <Section title="Fotografías del álbum">
        {fotos.length === 0 && <EmptyState title="Sin fotografías en esta página" description="Selecciona archivos para comenzar." />}
        <div className={styles.photos}>{fotos.map(f => <FotoEditor key={f.id + f.version} foto={f} disabled={ocupado} onSaved={refrescar} />)}</div>
        <div className={styles.actions}><Button variant="secondary" disabled={ocupado || paginaFotos === 1} onClick={() => setPaginaFotos(p => p - 1)}>Anterior</Button><span>Página {paginaFotos}</span><Button variant="secondary" disabled={ocupado || paginaFotos * 20 >= totalFotos} onClick={() => setPaginaFotos(p => p + 1)}>Siguiente</Button></div>
      </Section>
    </> : <EmptyState title="Selecciona o crea un álbum" description="Después podrás subir fotografías y decidir cuáles publicar." />}
  </Section>;
}

function FotoEditor({ foto, disabled, onSaved }: { foto: Foto; disabled: boolean; onSaved: () => void }) {
  const [draft, setDraft] = useState(foto); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const reemplazo = useRef<{ archivo: File; operacion: string } | null>(null);
  const [pendiente, setPendiente] = useState(false);
  const editada = draft.titulo !== foto.titulo || draft.textoAlternativo !== foto.textoAlternativo || draft.descripcion !== foto.descripcion || draft.orden !== foto.orden || draft.publicada !== foto.publicada;
  async function cargarImagen() {
    const carga = reemplazo.current; if (!carga || busy) return; setBusy(true); setError('');
    try { await subirFoto(foto.id, carga.archivo, carga.operacion); reemplazo.current = null; setPendiente(false); onSaved(); }
    catch (e) { setError(errorText(e)); if (e instanceof ApiError && e.status === 409) { reemplazo.current = null; setPendiente(false); } }
    finally { setBusy(false); }
  }
  return <form className={styles.photo} onSubmit={async e => { e.preventDefault(); if (busy) return; setBusy(true); setError(''); try { const f = await guardarFoto(draft); setDraft(f); onSaved(); } catch (err) { setError(err instanceof ApiError && err.status === 409 ? 'La foto cambió. Conservamos lo que escribiste; usa Recargar para revisar la versión actual antes de guardar.' : errorText(err)); } finally { setBusy(false); } }}>
    {draft.imagenUrl ? <Photo src={draft.imagenUrl} alt={draft.textoAlternativo} /> : <p>Imagen pendiente de carga</p>}
    <label className={styles.fileLabel}>{draft.imagenUrl ? 'Reemplazar imagen' : 'Cargar imagen'}<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy || disabled || editada} onChange={e => { const archivo = e.target.files?.[0]; e.target.value = ''; if (archivo) { reemplazo.current = { archivo, operacion: crypto.randomUUID() }; setPendiente(true); void cargarImagen(); } }} /></label>
    {editada && <p>Guarda los datos antes de cambiar la imagen.</p>}
    {pendiente && !busy && <Button type="button" variant="secondary" disabled={disabled || editada} onClick={() => void cargarImagen()}>Reintentar imagen</Button>}
    <TextField label="Título" value={draft.titulo} required maxLength={150} disabled={busy || disabled} onChange={e => setDraft(d => ({ ...d, titulo: e.target.value }))} />
    <TextField label="Texto alternativo" value={draft.textoAlternativo} required maxLength={250} disabled={busy || disabled} onChange={e => setDraft(d => ({ ...d, textoAlternativo: e.target.value }))} />
    <TextareaField label="Descripción" value={draft.descripcion ?? ''} maxLength={1000} disabled={busy || disabled} onChange={e => setDraft(d => ({ ...d, descripcion: e.target.value }))} />
    <TextField label="Orden" type="number" min={0} step={1} max={2147483647} required value={draft.orden} disabled={busy || disabled} onChange={e => setDraft(d => ({ ...d, orden: Number(e.target.value) }))} />
    <Switch label="Foto publicada" checked={draft.publicada} disabled={busy || disabled || !draft.imagenUrl} onChange={v => setDraft(d => ({ ...d, publicada: v }))} />
    {error && <FormAlert tone="error">{error}</FormAlert>}<Button type="submit" disabled={busy || disabled}>{busy ? 'Guardando…' : 'Guardar foto'}</Button>
  </form>;
}
