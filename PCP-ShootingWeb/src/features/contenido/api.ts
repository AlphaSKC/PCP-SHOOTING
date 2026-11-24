import { apiGet, apiPut } from '@/lib/api';

export type ContenidoSitio = {
  nosotrosTitulo: string; nosotrosTexto: string; seguridadTitulo: string; seguridadTexto: string;
  testimonio: string | null; autorTestimonio: string | null; descripcionPie: string;
  direccion: string; horarioPublico: string; telefono: string; telefonoMarcacion: string; whatsAppNumero: string; correo: string; ubicacionMapa: string;
  instagramUrl: string | null; facebookUrl: string | null; tikTokUrl: string | null; youTubeUrl: string | null;
};
export type ContenidoGestion = { contenido: ContenidoSitio; version: string; actualizadoEnUtc: string };
export const leerContenido = () => apiGet<ContenidoSitio>('/api/contenido-sitio');
export const leerContenidoGestion = () => apiGet<ContenidoGestion>('/api/administracion/contenido-sitio');
export const guardarContenido = (datos: ContenidoGestion) => apiPut<ContenidoGestion>('/api/administracion/contenido-sitio', datos);
