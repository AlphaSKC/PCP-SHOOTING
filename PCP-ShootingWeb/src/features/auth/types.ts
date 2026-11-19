export type Role = 'cliente' | 'recepcion' | 'admin';

export const ROLE_LABEL: Record<Role, string> = {
  cliente: 'Cliente',
  recepcion: 'Recepción',
  admin: 'Administrador',
};

export type Membership = {
  /** Nombre del plan CARTAQ (los nombres reales están pendientes: CLAUDE.md §8). */
  plan: string;
  number: string;
  /** ISO yyyy-mm-dd */
  validUntil: string;
  gold: boolean;
};

export type SessionUser = {
  id: string;
  name: string;
  email: string;
  phone: string;
  role: Role;
  membership: Membership | null;
  /**
   * false si no se pudo leer el perfil de la API (caída o sin conexión).
   * En ese caso el rol es "cliente" (mínimo privilegio) hasta reintentar.
   */
  profileSynced: boolean;
};

export type SignUpResult =
  | { status: 'signed_in'; user: SessionUser }
  /** Supabase pide confirmar el correo antes de iniciar sesión. */
  | { status: 'confirm_email'; email: string };

export type SignUpData = {
  nombre: string;
  email: string;
  telefono: string;
  password: string;
};

export type ProfileData = {
  nombre: string;
  email: string;
  telefono: string;
};

/** Error mostrable al usuario (mensaje ya en español). */
export class AuthError extends Error {}

export const initialsOf = (name: string) =>
  name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]!.toUpperCase())
    .join('') || '?';

export const firstNameOf = (name: string) => name.split(/\s+/)[0] ?? name;

export const isStaff = (role: Role) => role === 'admin' || role === 'recepcion';
