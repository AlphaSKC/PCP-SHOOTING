/*
 * Autenticación en MODO DEMO: se usa solo si no hay credenciales de Supabase en .env.local.
 * Reglas: cualquier correo + contraseña de 8+ caracteres; admin@… → admin, recepcion@… → recepción.
 */

import { AuthError, type ProfileData, type Role, type SessionUser, type SignUpData, type SignUpResult } from './types';


const STORAGE_KEY = 'pcp.auth.demo';
const delay = (ms = 500) => new Promise((r) => setTimeout(r, ms));

/** El almacenamiento puede fallar (navegación privada, bloqueado): nunca rompe la app. */
function read(storage: Storage): SessionUser | null {
  try {
    const raw = storage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as SessionUser) : null;
  } catch {
    return null;
  }
}

function write(user: SessionUser | null, remember: boolean) {
  for (const storage of [localStorage, sessionStorage]) {
    try {
      storage.removeItem(STORAGE_KEY);
    } catch {
      /* sin almacenamiento disponible */
    }
  }
  if (!user) return;
  try {
    (remember ? localStorage : sessionStorage).setItem(STORAGE_KEY, JSON.stringify(user));
  } catch {
    /* la sesión vive solo en memoria */
  }
}

function isRemembered(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) !== null;
  } catch {
    return false;
  }
}

function roleFromEmail(email: string): Role {
  const local = email.split('@')[0]?.toLowerCase() ?? '';
  if (local === 'admin') return 'admin';
  if (local === 'recepcion') return 'recepcion';
  return 'cliente';
}

function nameFromEmail(email: string): string {
  const local = email.split('@')[0] ?? 'Usuario';
  const pretty = local.replace(/[._-]+/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  return pretty || 'Usuario';
}

function demoUser(email: string, name?: string, phone = ''): SessionUser {
  const role = roleFromEmail(email);
  return {
    id: `demo-${email}`,
    name: name ?? nameFromEmail(email),
    email,
    phone,
    role,
    profileSynced: true,
    // Membresía de EJEMPLO solo para clientes, para ver la tarjeta de socio.
    membership:
      role === 'cliente'
        ? { plan: 'Socio Oro', number: '0142', validUntil: '2027-01-23', gold: true }
        : null,
  };
}

export async function getSession(): Promise<SessionUser | null> {
  return read(localStorage) ?? read(sessionStorage);
}

export async function signIn(email: string, password: string, remember: boolean): Promise<SessionUser> {
  await delay();
  if (password.length < 8) throw new AuthError('Correo o contraseña incorrectos.');
  const user = demoUser(email.trim().toLowerCase());
  write(user, remember);
  return user;
}

export async function signInWithGoogle(): Promise<SessionUser> {
  await delay();
  const user = demoUser('cliente.google@ejemplo.com', 'Cliente Google');
  write(user, true);
  return user;
}

export async function signUp(data: SignUpData): Promise<SignUpResult> {
  await delay();
  const user = demoUser(data.email.trim().toLowerCase(), data.nombre.trim(), data.telefono);
  // Las cuentas nuevas no tienen membresía.
  user.membership = null;
  write(user, true);
  return { status: 'signed_in', user };
}

export async function signOut(): Promise<void> {
  write(null, false);
}

export async function resetPassword(email: string): Promise<void> {
  await delay();
  if (import.meta.env.DEV) console.info('[demo] enlace de recuperación para', email);
}

export async function updatePassword(password: string): Promise<void> {
  await delay();
  if (password.length < 8) throw new AuthError('La contraseña debe tener al menos 8 caracteres.');
}

/**
 * Activa o renueva la membresía tras el pago (DEMO). Con la API real lo hace el backend
 * al confirmar el pago (POST /api/membresias + webhook) y el front solo vuelve a leer el perfil.
 * Renovación manual: si la membresía sigue vigente, el nuevo periodo empieza al terminar el actual.
 */
export async function activateMembership(
  current: SessionUser,
  plan: { name: string; gold: boolean },
  months: number,
): Promise<SessionUser> {
  await delay(900);
  const today = new Date();
  const base =
    current.membership && new Date(current.membership.validUntil) > today
      ? new Date(current.membership.validUntil)
      : today;
  base.setMonth(base.getMonth() + months);
  const user: SessionUser = {
    ...current,
    membership: {
      plan: plan.name,
      gold: plan.gold,
      number: current.membership?.number ?? String(Math.floor(Math.random() * 9000) + 1000).padStart(4, '0'),
      validUntil: base.toISOString().slice(0, 10),
    },
  };
  write(user, isRemembered());
  return user;
}

export async function updateProfile(current: SessionUser, data: ProfileData): Promise<SessionUser> {
  await delay();
  const user: SessionUser = { ...current, name: data.nombre.trim(), email: data.email.trim(), phone: data.telefono };
  write(user, isRemembered());
  return user;
}

export async function resendConfirmation(): Promise<void> {
  await delay();
}
