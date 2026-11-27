/**
 * Alertas emergentes (toasts) de todo el sitio y el panel.
 * Uso: `toast.success('Producto guardado.')`, `toast.error('…')`, `toast.info('…')`.
 *
 * Regla de experiencia (un solo aviso por acción):
 * - `src/lib/api.ts` avisa cada acción que no sea GET con un texto genérico («Cambios guardados.»).
 * - Si la pantalla tiene un mensaje más específico (`FormAlert`), ese REEMPLAZA al genérico: nunca salen dos.
 * - `FormAlert` ya no pinta mensajes verdes dentro de la pantalla: se convierten en alerta (ver Field.tsx).
 */
export type ToastTone = 'success' | 'error' | 'info';
export type ToastItem = { id: number; tone: ToastTone; message: string };
type Entry = ToastItem & { source: 'api' | 'ui'; at: number };

const MAX_VISIBLE = 3;
/** Ventana en la que el aviso de la pantalla sustituye al aviso genérico de la API de la misma acción. */
const MERGE_MS = 4000;
const DURATION: Record<ToastTone, number> = { success: 4000, info: 6000, error: 9000 };

let items: Entry[] = [];
let nextId = 1;
let lastApiError = 0;
const listeners = new Set<(list: ToastItem[]) => void>();
const timers = new Map<number, ReturnType<typeof setTimeout>>();

function emit() {
  const list = items.map(({ id, tone, message }) => ({ id, tone, message }));
  for (const l of listeners) l(list);
}

function remove(id: number) {
  const t = timers.get(id);
  if (t) clearTimeout(t);
  timers.delete(id);
  items = items.filter((i) => i.id !== id);
}

export function dismissToast(id: number) {
  remove(id);
  emit();
}

function show(tone: ToastTone, message: string, source: Entry['source']) {
  const text = message.trim();
  if (!text) return;
  const now = Date.now();
  if (source === 'api') {
    if (tone === 'error') lastApiError = now;
    // La pantalla ya mostró su propio aviso de esta acción: no se repite con el genérico.
    if (items.some((i) => i.source === 'ui' && i.tone === tone && now - i.at < 1000)) return;
  } else {
    // El aviso específico de la pantalla sustituye a los genéricos de la API de la misma acción.
    for (const i of [...items]) if (i.source === 'api' && i.tone === tone && now - i.at < MERGE_MS) remove(i.id);
  }
  for (const i of [...items]) if (i.tone === tone && i.message === text) remove(i.id);
  const id = nextId++;
  items = [...items, { id, tone, message: text, source, at: now }];
  while (items.length > MAX_VISIBLE) remove(items[0].id);
  timers.set(id, setTimeout(() => dismissToast(id), DURATION[tone]));
  emit();
}

export const toast = {
  success: (message: string) => show('success', message, 'ui'),
  error: (message: string) => show('error', message, 'ui'),
  info: (message: string) => show('info', message, 'ui'),
};

/** Solo para `src/lib/api.ts`: aviso genérico de una acción, reemplazable por el de la pantalla. */
export const apiToast = {
  success: (message: string) => show('success', message, 'api'),
  error: (message: string) => show('error', message, 'api'),
};

/** ¿Acaba de fallar una acción? Así un mensaje de error de la pantalla sabe que es el resultado de esa acción. */
export const recentActionError = () => Date.now() - lastApiError < MERGE_MS;

export function subscribeToasts(listener: (list: ToastItem[]) => void) {
  listeners.add(listener);
  emit();
  return () => {
    listeners.delete(listener);
  };
}
