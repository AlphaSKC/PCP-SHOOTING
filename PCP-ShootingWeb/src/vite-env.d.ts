/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_SUPABASE_URL?: string;
  readonly VITE_SUPABASE_PUBLISHABLE_KEY?: string;
  /** "true" para mostrar "Continuar con Google" (requiere activar el proveedor en Supabase). */
  readonly VITE_AUTH_GOOGLE?: string;
  readonly VITE_DATA_SOURCE?: 'api' | 'demo';
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
