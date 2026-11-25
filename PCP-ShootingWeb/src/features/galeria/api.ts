import { apiGet, apiPost, apiPut, apiUpload, type Page } from '@/lib/api';

export type Album = { id: string; nombre: string; descripcion: string | null; orden: number; publicado: boolean; version: string };
export type AlbumPublico = { id: string; nombre: string; descripcion: string | null; orden: number; portadaUrl: string; cantidadFotos: number };
export type Foto = { id: string; albumId: string; titulo: string; textoAlternativo: string; descripcion: string | null; orden: number; publicada: boolean; imagenUrl: string | null; version: string };
export type FotoPublica = Pick<Foto, 'id' | 'albumId' | 'titulo' | 'textoAlternativo' | 'descripcion' | 'orden'> & { imagenUrl: string };
const admin = '/api/administracion/galeria';
export const albumsPublicos = (pagina = 1) => apiGet<Page<AlbumPublico>>('/api/galeria/albums', { pagina, tamanoPagina: 100 });
export const fotosPublicas = (albumId: string | null, pagina = 1) => apiGet<Page<FotoPublica>>('/api/galeria/fotos', { albumId, pagina, tamanoPagina: 20 });
export const albumsGestion = (pagina = 1) => apiGet<Page<Album>>(`${admin}/albums`, { pagina, tamanoPagina: 20 });
export const fotosGestion = (albumId: string, pagina = 1) => apiGet<Page<Foto>>(`${admin}/albums/${albumId}/fotos`, { pagina, tamanoPagina: 20 });
/**
 * Crea o edita un álbum. Solo se envían los campos editables: `version` va únicamente al editar
 * (la API espera un UUID; una cadena vacía invalida toda la petición con 400).
 */
export function guardarAlbum(a: Omit<Album, 'id'> & { id?: string }) {
  const body = { nombre: a.nombre.trim(), descripcion: a.descripcion?.trim() || null, orden: a.orden, publicado: a.publicado };
  return a.id ? apiPut<Album>(`${admin}/albums/${a.id}`, { ...body, version: a.version || null }) : apiPost<Album>(`${admin}/albums`, body);
}
export const guardarFoto = (f: Foto) =>
  apiPut<Foto>(`${admin}/fotos/${f.id}`, {
    titulo: f.titulo.trim(),
    textoAlternativo: f.textoAlternativo.trim(),
    descripcion: f.descripcion?.trim() || null,
    orden: f.orden,
    publicada: f.publicada,
    version: f.version || null,
  });
export const crearFoto = (albumId: string, titulo: string, idOperacion: string) => apiPost<Foto>(`${admin}/albums/${albumId}/fotos`, { idOperacion, titulo: titulo.slice(0, 150), textoAlternativo: titulo.slice(0, 250), publicada: false, orden: 0 });
export async function subirFoto(id: string, archivo: File, idOperacion: string) {
  const form = new FormData(); form.append('archivo', archivo); form.append('idOperacion', idOperacion);
  return apiUpload<{ url: string; version: string }>(`${admin}/fotos/${id}/imagen`, form);
}
