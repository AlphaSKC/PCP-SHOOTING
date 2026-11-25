/*
 * Plantillas de avisos (F10) · API real. Contrato: docs/AVISOS.md
 *   GET /api/administracion/avisos   (solo Administrador) { notifyEmail, waOrderReady, waReminder, version, actualizadoEnUtc }
 *   PUT /api/administracion/avisos   los 4 campos obligatorios (version vigente; 409 = recargar y revisar)
 * Solo guarda textos: NO envía correos ni WhatsApp (eso es R07) ni sustituye marcadores.
 * Distinto de los avisos públicos del sitio (src/features/avisos).
 */
import { apiGet, apiPut } from '@/lib/api';

export type NoticeTemplates = {
  notifyEmail: string;
  waOrderReady: string;
  waReminder: string;
  version: string;
  updatedUtc: string | null;
};

type ApiTemplates = { notifyEmail: string | null; waOrderReady: string; waReminder: string; version: string; actualizadoEnUtc: string | null };

const PATH = '/api/administracion/avisos';
export const EMAIL_MAX = 254;
export const TEMPLATE_MAX = 4000;

/** Texto que se usa en Pedidos si no hay plantilla guardada (o si el rol no puede leerla). */
export const DEFAULT_ORDER_READY = 'Hola [nombre], tu pedido [número] de PCP Shooting está listo para recoger en el club. ¡Te esperamos!';

const fromApi = (a: ApiTemplates): NoticeTemplates => ({
  notifyEmail: a.notifyEmail ?? '',
  waOrderReady: a.waOrderReady ?? '',
  waReminder: a.waReminder ?? '',
  version: a.version,
  updatedUtc: a.actualizadoEnUtc,
});

export const getTemplates = async () => fromApi(await apiGet<ApiTemplates>(PATH));

export async function saveTemplates(t: NoticeTemplates): Promise<NoticeTemplates> {
  return fromApi(
    await apiPut<ApiTemplates>(PATH, {
      notifyEmail: t.notifyEmail.trim() || null,
      waOrderReady: t.waOrderReady,
      waReminder: t.waReminder,
      version: t.version,
    }),
  );
}

export function validateTemplates(t: NoticeTemplates): string | null {
  const email = t.notifyEmail.trim();
  if (email && (email.length > EMAIL_MAX || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) return 'Escribe un correo válido o déjalo vacío.';
  if (t.waOrderReady.length > TEMPLATE_MAX || t.waReminder.length > TEMPLATE_MAX) return `Cada plantilla admite hasta ${TEMPLATE_MAX} caracteres.`;
  return null;
}

/** Rellena los marcadores [nombre], [número], [hora], [línea]. */
export const fillTemplate = (text: string, values: Record<string, string>) =>
  Object.entries(values).reduce((s, [k, v]) => s.replaceAll(`[${k}]`, v), text);
