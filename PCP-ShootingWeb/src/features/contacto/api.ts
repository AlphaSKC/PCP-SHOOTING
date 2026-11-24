/*
 * Envíos de formularios públicos.
 * Contacto usa POST /api/contacto con operación estable y confirmación real del servidor.
 * Newsletter conserva su simulación separada: POST /api/suscripciones sigue pendiente.
 */

import type { ContactValues, NewsletterValues } from './schemas';
import { apiPost, DATA_SOURCE } from '@/lib/api';

const simulate = () => new Promise<void>((resolve) => setTimeout(resolve, 700));

export async function sendContactMessage(values: ContactValues, idOperacion: string, sitioWeb: string): Promise<void> {
  if (DATA_SOURCE === 'api') {
    await apiPost('/api/contacto', { ...values, telefono: values.telefono || null, idOperacion, sitioWeb });
    return;
  }
  await simulate();
}

export async function subscribeNewsletter(values: NewsletterValues): Promise<void> {
  if (import.meta.env.DEV) console.info('[simulado] POST /api/suscripciones', values);
  await simulate();
}
