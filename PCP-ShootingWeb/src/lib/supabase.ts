/*
 * Cliente de Supabase — SOLO para autenticación (y más adelante subida de archivos a Storage).
 * Regla de oro (CLAUDE.md §2): el frontend NO lee ni escribe tablas con este cliente;
 * todos los datos de negocio pasan por la API C#, que recibe el JWT en `Authorization: Bearer`.
 */
import { createClient, type SupabaseClient, type SupportedStorage } from '@supabase/supabase-js';

const url = import.meta.env.VITE_SUPABASE_URL?.trim().replace(/\/$/, '') ?? '';
const key = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY?.trim() ?? '';

const looksValid =
  /^https:\/\/[a-z0-9-]+\.supabase\.co$/.test(url) && key.length > 20 && !url.includes('TU-PROYECTO') && !key.includes('PEGA_AQUI');

// Defensa: nunca permitir una clave secreta en el navegador.
if (key.startsWith('sb_secret_') || /service_role/.test(atobSafe(key.split('.')[1] ?? ''))) {
  throw new Error('VITE_SUPABASE_PUBLISHABLE_KEY contiene una clave SECRETA. Usa la clave pública (publishable/anon).');
}

function atobSafe(s: string) {
  try {
    return atob(s.replace(/-/g, '+').replace(/_/g, '/'));
  } catch {
    return '';
  }
}

export const isSupabaseConfigured = looksValid;
export const supabaseUrl = url;

/*
 * "Mantener sesión iniciada":
 *   marcado   → la sesión se guarda en localStorage (sobrevive al cerrar el navegador).
 *   desmarcado → sessionStorage (se borra al cerrar la pestaña/navegador).
 */
const REMEMBER_KEY = 'pcp.auth.recordar';

export function setRememberSession(remember: boolean) {
  try {
    localStorage.setItem(REMEMBER_KEY, remember ? '1' : '0');
  } catch {
    /* sin almacenamiento */
  }
}

function remembers() {
  try {
    return localStorage.getItem(REMEMBER_KEY) !== '0';
  } catch {
    return false;
  }
}

const authStorage: SupportedStorage = {
  getItem(k) {
    try {
      return sessionStorage.getItem(k) ?? localStorage.getItem(k);
    } catch {
      return null;
    }
  },
  setItem(k, v) {
    try {
      // El verificador PKCE va siempre en localStorage: el enlace del correo puede abrirse en otra pestaña.
      const target = remembers() || k.endsWith('-code-verifier') ? localStorage : sessionStorage;
      const other = target === localStorage ? sessionStorage : localStorage;
      target.setItem(k, v);
      other.removeItem(k);
    } catch {
      /* sin almacenamiento: la sesión vive en memoria */
    }
  },
  removeItem(k) {
    try {
      localStorage.removeItem(k);
      sessionStorage.removeItem(k);
    } catch {
      /* nada que limpiar */
    }
  },
};

export const supabase: SupabaseClient | null = looksValid
  ? createClient(url, key, {
      auth: {
        // PKCE: los enlaces de correo/OAuth regresan con un código de un solo uso, no con tokens en la URL.
        flowType: 'pkce',
        persistSession: true,
        autoRefreshToken: true,
        detectSessionInUrl: true,
        storage: authStorage,
        storageKey: 'pcp.auth.sesion',
      },
    })
  : null;

/** Token de acceso vigente (se renueva solo antes de expirar). */
export async function getAccessToken(): Promise<string | null> {
  if (!supabase) return null;
  const { data } = await supabase.auth.getSession();
  return data.session?.access_token ?? null;
}
