/*
 * Envíos de formularios públicos.
 * TODO(backend): la API todavía no tiene endpoints para esto. Propuesta para el desarrollador C#:
 *   POST /api/contacto      { nombre, telefono, email, motivo, mensaje }
 *   POST /api/suscripciones { email }
 * Mientras tanto se simula una respuesta exitosa para poder probar la interfaz.
 */

import type { ContactValues, NewsletterValues } from './schemas';

const simulate = () => new Promise<void>((resolve) => setTimeout(resolve, 700));

export async function sendContactMessage(values: ContactValues): Promise<void> {
  if (import.meta.env.DEV) console.info('[simulado] POST /api/contacto', values);
  await simulate();
}

export async function subscribeNewsletter(values: NewsletterValues): Promise<void> {
  if (import.meta.env.DEV) console.info('[simulado] POST /api/suscripciones', values);
  await simulate();
}
