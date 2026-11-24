import { apiGet, apiPut, type Page, type Query } from '@/lib/api';
export type EstadoContacto = 'Pendiente' | 'Atendida' | 'Archivada';
export type ContactoResumen = { id: string; nombre: string; email: string; motivo: string; estado: EstadoContacto; creadoEnUtc: string };
export type ContactoDetalle = ContactoResumen & { telefono: string | null; mensaje: string; privacidadAceptadaEnUtc: string; notasInternas: string | null; version: string; actualizadoEnUtc: string };
export const listarSolicitudes = (query: Query) => apiGet<Page<ContactoResumen>>('/api/administracion/contacto', query);
export const consultarSolicitud = (id: string) => apiGet<ContactoDetalle>(`/api/administracion/contacto/${id}`);
export const actualizarSolicitud = (id: string, estado: EstadoContacto, notasInternas: string, version: string) =>
  apiPut<ContactoDetalle>(`/api/administracion/contacto/${id}`, { estado, notasInternas: notasInternas.trim() || null, version });
