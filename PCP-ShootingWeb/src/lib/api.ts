/*
 * Cliente HTTP de la API C# (ASP.NET Core).
 * - Envía el JWT de Supabase en `Authorization: Bearer` cuando hay sesión.
 * - Respuestas en camelCase; los enums llegan como número.
 * - Errores en formato ProblemDetails (title/detail/errors) → ApiError con mensaje en español.
 * El rol que ve el frontend es solo para la interfaz: la API vuelve a validar permisos en cada petición.
 */
import { getAccessToken } from './supabase';

export const DATA_SOURCE: 'api' | 'demo' = import.meta.env.VITE_DATA_SOURCE === 'api' ? 'api' : 'demo';
const BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

/** Página paginada del backend (PaginaConsulta<T>). */
export type Page<T> = { elementos: T[]; total: number; pagina: number; tamanoPagina: number };

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly fields: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

type Query = Record<string, string | number | boolean | null | undefined>;

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
  const message = res.status >= 500 ? fallback : body.detail || body.title || fallback;
  return new ApiError(message, res.status, body.errors);
}

type Options = { query?: Query; body?: unknown; signal?: AbortSignal; auth?: boolean };

async function request<T>(method: string, path: string, { query, body, signal, auth = true }: Options = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (auth) {
    const token = await getAccessToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }
  let res: Response;
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
      // La API usa Bearer, no cookies: no se envían credenciales del navegador.
      credentials: 'omit',
    });
  } catch (e) {
    if ((e as Error).name === 'AbortError') throw e;
    throw new ApiError('No pudimos conectar con el servidor. Revisa tu conexión e intenta de nuevo.', 0);
  }
  if (!res.ok) throw await toError(res);
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const apiGet = <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>('GET', path, { query, signal });
export const apiPut = <T>(path: string, body: unknown) => request<T>('PUT', path, { body });
export const apiPost = <T>(path: string, body?: unknown) => request<T>('POST', path, { body });

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
