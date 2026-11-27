/*
 * Cliente HTTP de la API C# (ASP.NET Core).
 * - Envía el JWT de Supabase en `Authorization: Bearer` cuando hay sesión.
 * - Respuestas en camelCase; los enums llegan como número.
 * - Errores en formato ProblemDetails (title/detail/errors) → ApiError con mensaje en español.
 * El rol que ve el frontend es solo para la interfaz: la API vuelve a validar permisos en cada petición.
 */
import { getAccessToken } from './supabase';
import { apiToast as toast } from './toast';

export const DATA_SOURCE: 'api' | 'demo' = import.meta.env.VITE_DATA_SOURCE === 'api' ? 'api' : 'demo';
const BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

/** Página paginada del backend (PaginaConsulta<T>). */
export type Page<T> = { elementos: T[]; total: number; pagina: number; tamanoPagina: number };

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly fields: Record<string, string[]> = {},
    /** 404 sin mensaje de la API o 405: la ruta no existe en el servidor publicado (función pendiente de backend). */
    readonly missingRoute = false,
  ) {
    super(message);
  }
}

export type Query = Record<string, string | number | boolean | null | undefined>;

function buildUrl(path: string, query?: Query) {
  const params = new URLSearchParams();
  for (const [k, v] of Object.entries(query ?? {})) {
    if (v !== undefined && v !== null && v !== '') params.set(k, String(v));
  }
  const qs = params.toString();
  return `${BASE}${path}${qs ? `?${qs}` : ''}`;
}

async function toError(res: Response): Promise<ApiError> {
  let body: { title?: string; detail?: string; errors?: Record<string, string[]> } = {};
  try {
    body = await res.json();
  } catch {
    /* sin cuerpo JSON */
  }
  const fallback =
    res.status === 401
      ? 'Tu sesión expiró. Inicia sesión de nuevo.'
      : res.status === 403
        ? 'No tienes permiso para esta acción.'
        : res.status === 404
          ? 'No encontrado.'
          : res.status >= 500
            ? 'El servidor no pudo completar la solicitud. Intenta más tarde.'
            : 'Revisa los datos e intenta de nuevo.';
  // Los 5xx nunca muestran detalles internos al usuario.
  // Errores de validación (400): el primer mensaje por campo es más útil que el título genérico.
  // Los mensajes técnicos en inglés de ASP.NET (JSON mal formado, campo requerido por el binder) no se muestran al usuario.
  const technical = /^The .+ field|could not be converted|JSON value|is required\.$|^The JSON|^'.+' is invalid/i;
  const firstFieldError = Object.entries(body.errors ?? {})
    .filter(([key]) => !key.startsWith('$'))
    .flatMap(([, msgs]) => msgs)
    .find((m) => !technical.test(m));
  // Títulos estándar en inglés de ASP.NET («Not Found», «Bad Request»…) no se muestran: se usa el texto en español.
  const genericTitle = /One or more validation errors|^(Not Found|Bad Request|Unauthorized|Forbidden|Method Not Allowed|Conflict|Internal Server Error|Service Unavailable|Bad Gateway|Gateway Timeout|Unsupported Media Type|Too Many Requests)$/i;
  const title = body.title && !genericTitle.test(body.title) ? body.title : undefined;
  // 502/503/504 traen mensajes redactados por la API (p. ej. «Falta configurar Supabase Storage»): se muestran con su código
  // para poder diagnosticar. Un 500 nunca muestra detalles internos.
  const gateway = res.status >= 502 && res.status <= 504 && body.detail ? `${body.detail} (código ${res.status})` : undefined;
  const message = gateway ?? (res.status >= 500 ? fallback : body.detail || firstFieldError || title || fallback);
  const missingRoute = res.status === 405 || (res.status === 404 && !body.detail && !title);
  return new ApiError(message, res.status, body.errors, missingRoute);
}

type Options = { query?: Query; body?: unknown; signal?: AbortSignal; auth?: boolean };

/*
 * Alerta automática de cada acción (guardar, editar, agregar, eliminar, subir). `false` = sin alerta, porque la pantalla
 * ya da su propio aviso o la llamada ocurre en segundo plano. Se busca la primera regla que coincida.
 */
const NOTICES: [method: string, path: RegExp, message: string | false][] = [
  ['PUT', /^\/api\/perfiles\/yo$/, false], // sincroniza el perfil al iniciar sesión
  ['*', /^\/api\/apartados/, false], // apartado de líneas: el asistente de reserva muestra su contador
  ['POST', /^\/api\/administracion\/galeria\/albums\/[^/]+\/fotos$/, false], // ficha previa a la subida de la foto
  ['POST', /^\/api\/pedidos\/[^/]+\/pagos$/, false], // va seguido de la confirmación del pago
  ['POST', /^\/api\/pagos\/[^/]+\/confirmacion$/, 'Pago registrado.'],
  ['POST', /^\/api\/pedidos\/[^/]+\/recogida$/, 'Pedido marcado como entregado.'],
  ['POST', /^\/api\/pedidos\/[^/]+\/cancelacion$/, 'Pedido cancelado.'],
  ['POST', /^\/api\/pedidos$/, 'Pedido creado.'],
  ['POST', /^\/api\/reservas\/mostrador$/, 'Reserva de mostrador creada.'],
  ['POST', /^\/api\/reservas$/, 'Reserva creada.'],
  ['POST', /^\/api\/contacto$/, 'Mensaje enviado.'],
  ['POST', /\/movimientos$/, 'Movimiento de inventario registrado.'],
  ['PUT', /\/minimo$/, 'Existencia mínima actualizada.'],
  ['PATCH', /^\/api\/productos\/[^/]+\/estado$/, 'Visibilidad del producto actualizada.'],
  ['PATCH', /\/acceso$/, 'Acceso de la cuenta actualizado.'],
  ['POST', /^\/api\/membresias\/renovaciones$/, 'Membresía registrada.'],
  ['POST', /\/bloqueos$/, 'Bloqueo agregado.'],
  ['DELETE', /\/(logo|portada|imagen)$/, 'Imagen quitada.'],
  ['DELETE', /./, 'Eliminado correctamente.'],
  ['POST', /./, 'Agregado correctamente.'],
  ['PUT', /./, 'Cambios guardados.'],
  ['PATCH', /./, 'Cambios guardados.'],
];

function mutationNotice(method: string, path: string, isForm: boolean): string | false {
  const rule = NOTICES.find(([m, re]) => (m === '*' || m === method) && re.test(path));
  if (rule && rule[2] === false) return false;
  if (isForm) return 'Imagen subida correctamente.';
  return rule ? rule[2] : 'Cambios guardados.';
}

async function request<T>(method: string, path: string, { query, body, signal, auth = true }: Options = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const isForm = body instanceof FormData;
  const notice = method === 'GET' ? false : mutationNotice(method, path, isForm);
  // Con FormData el navegador pone el Content-Type multipart con su boundary.
  if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json';
  if (auth) {
    const token = await getAccessToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }
  let res: Response;
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
      signal,
      // La API usa Bearer, no cookies: no se envían credenciales del navegador.
      credentials: 'omit',
    });
  } catch (e) {
    if ((e as Error).name === 'AbortError') throw e;
    const offline = new ApiError('No pudimos conectar con el servidor. Revisa tu conexión e intenta de nuevo.', 0);
    if (notice !== false) toast.error(offline.message);
    throw offline;
  }
  if (!res.ok) {
    const error = await toError(res);
    if (notice !== false) {
      // 404 sin mensaje de la API (o 405) = la ruta no existe en el servidor publicado: se indica cuál para reportarla.
      toast.error(error.missingRoute ? `Esta acción todavía no está disponible en el servidor (${error.status} · ${method} ${path}).` : error.message);
    }
    throw error;
  }
  const data = res.status === 204 ? (undefined as T) : ((await res.json()) as T);
  if (notice) toast.success(notice);
  return data;
}

export const apiGet = <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>('GET', path, { query, signal });
export const apiPut = <T>(path: string, body: unknown) => request<T>('PUT', path, { body });
export const apiPost = <T>(path: string, body?: unknown) => request<T>('POST', path, { body });
export const apiPatch = <T>(path: string, body?: unknown) => request<T>('PATCH', path, { body });
export const apiDelete = <T = void>(path: string) => request<T>('DELETE', path);
/** Envío multipart/form-data (subida de archivos). */
export const apiUpload = <T>(path: string, form: FormData) => request<T>('POST', path, { body: form });

/** Descarga un archivo de la API (p. ej. CSV) con la sesión y lo guarda con `filename`. */
export async function apiDownload(path: string, query: Query, filename: string): Promise<void> {
  const token = await getAccessToken();
  let res: Response;
  try {
    res = await fetch(buildUrl(path, query), {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      credentials: 'omit',
    });
  } catch {
    throw new ApiError('No pudimos conectar con el servidor. Revisa tu conexión e intenta de nuevo.', 0);
  }
  if (!res.ok) throw await toError(res);
  const url = URL.createObjectURL(await res.blob());
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** Resultado de las escrituras del backend (EscrituraResultado). */
export type WriteResult = { id: string; estado: string };

/** Recorre todas las páginas (máx. 100 por página, límite del backend). */
export async function apiGetAll<T>(path: string, query?: Query): Promise<T[]> {
  const out: T[] = [];
  for (let pagina = 1; pagina <= 50; pagina++) {
    const page = await apiGet<Page<T>>(path, { ...query, pagina, tamanoPagina: 100 });
    out.push(...page.elementos);
    if (out.length >= page.total || page.elementos.length === 0) break;
  }
  return out;
}
