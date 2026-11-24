import { useEffect, useRef, useState } from 'react';
import { Button, Chip, ChipGroup, EmptyState, FormAlert, PageHero, Photo } from '@/components/ui';
import { albumsPublicos, fotosPublicas, type AlbumPublico, type FotoPublica } from './api';
import { Lightbox } from '@/pages/public/galeria/Lightbox';
import styles from '@/pages/public/galeria/GaleriaPage.module.css';

export function GaleriaPublica() {
  const [albums, setAlbums] = useState<AlbumPublico[]>([]);
  const [albumPagina, setAlbumPagina] = useState(1); const [albumTotal, setAlbumTotal] = useState(0);
  const [albumsCargando, setAlbumsCargando] = useState(true);
  const [filtro, setFiltro] = useState<string | null>(null);
  const [fotos, setFotos] = useState<FotoPublica[]>([]); const [pagina, setPagina] = useState(1); const [total, setTotal] = useState(0);
  const [cargando, setCargando] = useState(true); const [error, setError] = useState(''); const [revision, setRevision] = useState(0);
  const [index, setIndex] = useState<number | null>(null); const request = useRef(0);
  useEffect(() => { let viva = true; setAlbumsCargando(true); albumsPublicos(albumPagina).then(p => { if (viva) { setAlbums(a => albumPagina === 1 ? p.elementos : [...new Map([...a, ...p.elementos].map(x => [x.id, x])).values()]); setAlbumTotal(p.total); } }).catch(() => { if (viva) setError('No pudimos cargar los álbumes. Intenta de nuevo.'); }).finally(() => { if (viva) setAlbumsCargando(false); }); return () => { viva = false; }; }, [albumPagina, revision]);
  useEffect(() => {
    const id = ++request.current; setCargando(true); setError('');
    fotosPublicas(filtro, pagina).then(p => { if (id === request.current) { setFotos(a => pagina === 1 ? p.elementos : [...new Map([...a, ...p.elementos].map(x => [x.id, x])).values()]); setTotal(p.total); } }).catch(() => { if (id === request.current) setError('No pudimos cargar las fotos. Intenta de nuevo.'); }).finally(() => { if (id === request.current) setCargando(false); });
    return () => { request.current++; };
  }, [filtro, pagina, revision]);
  const items = fotos.map(f => ({ id: f.id, category: f.albumId, title: f.titulo, kind: 'foto' as const, featured: false, imageUrl: f.imagenUrl, alt: f.textoAlternativo }));
  function seleccionar(id: string | null) { if (id === filtro) return; setFiltro(id); setPagina(1); setFotos([]); setTotal(0); setIndex(null); }
  return <>
    <title>Galería · PCP Shooting</title>
    <PageHero tone="light" eyebrow="Galería" title="Así se vive el campo" lead="Fotografías del club y sus actividades." />
    <div className={`container ${styles.main}`}>
      <ChipGroup label="Filtrar galería"><Chip selected={filtro === null} onClick={() => seleccionar(null)}>Todo</Chip>{albums.map(a => <Chip key={a.id} selected={filtro === a.id} onClick={() => seleccionar(a.id)}>{a.nombre}</Chip>)}</ChipGroup>
      {albums.length < albumTotal && <Button variant="secondary" disabled={albumsCargando} onClick={() => setAlbumPagina(p => p + 1)}>Más álbumes</Button>}
      {error && <FormAlert tone="error">{error} <Button variant="secondary" onClick={() => setRevision(r => r + 1)}>Reintentar</Button></FormAlert>}
      {cargando && <p role="status">Cargando fotografías…</p>}
      {!cargando && !error && fotos.length === 0 && <EmptyState title="Aún no hay fotografías publicadas" description="Vuelve pronto para conocer los momentos del club." />}
      <ul className={styles.grid} aria-label="Fotografías">{fotos.map((f, i) => <li key={f.id} className={styles.cell}><button type="button" className={styles.tile} onClick={() => setIndex(i)} aria-label={`Abrir foto: ${f.titulo}`}><Photo src={f.imagenUrl} alt={f.textoAlternativo} /><span className={styles.tag}>{f.titulo}</span></button></li>)}</ul>
      {fotos.length < total && !error && <Button variant="secondary" disabled={cargando} onClick={() => setPagina(p => p + 1)}>Ver más fotografías</Button>}
    </div>
    <Lightbox items={items} index={index} onChange={setIndex} categoryLabel={i => albums.find(a => a.id === i.category)?.nombre ?? 'Galería'} />
  </>;
}
