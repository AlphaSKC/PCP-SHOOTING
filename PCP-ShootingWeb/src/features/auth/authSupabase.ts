/*
 * Autenticación real: Supabase Auth (identidad) + API C# (perfil y rol).
 *
 * Seguridad:
 * - Supabase guarda contraseñas y emite el JWT (ES256). El frontend nunca ve contraseñas guardadas.
 * - El ROL viene de la API (GET /api/perfiles/yo), nunca de user_metadata (que el usuario puede editar).
 *   La API vuelve a validar el rol en cada petición; aquí solo decide qué se muestra.
 * - Si la API no responde, el usuario queda como "cliente" (mínimo privilegio) — nunca se asume un rol mayor.
 * - Mensajes genéricos en registro y recuperación para no revelar qué correos existen.
 */
import type { AuthError as SupabaseAuthError, Session, User } from '@supabase/supabase-js';
import { ApiError, apiGet, apiPut } from '@/lib/api';
import { setRememberSession, supabase } from '@/lib/supabase';
import { AuthError, type Membership, type ProfileData, type Role, type SessionUser, type SignUpData, type SignUpResult } from './types';

function client() {
  if (!supabase) throw new AuthError('El inicio de sesión no está configurado.');
  return supabase;
}

const origin = () => window.location.origin;

/* ---------- Errores de Supabase → mensajes en español ---------- */

function translate(e: SupabaseAuthError | Error): AuthError {
  const code = 'code' in e ? (e as SupabaseAuthError).code : undefined;
  const status = 'status' in e ? (e as SupabaseAuthError).status : undefined;
  const msg = e.message.toLowerCase();
  if (code === 'invalid_credentials' || msg.includes('invalid login credentials')) return new AuthError('Correo o contraseña incorrectos.');
  if (code === 'email_not_confirmed' || msg.includes('email not confirmed'))
    return new AuthError('Aún no confirmas tu correo. Revisa tu bandeja (y spam) o reenvía el enlace.');
  if (code === 'weak_password' || msg.includes('password should'))
    return new AuthError('La contraseña es muy débil. Usa al menos 8 caracteres combinando letras y números.');
  if (code === 'same_password') return new AuthError('La nueva contraseña debe ser distinta a la actual.');
  if (code === 'reauthentication_needed') return new AuthError('Por seguridad, vuelve a iniciar sesión antes de cambiar tu contraseña.');
  if (code === 'over_email_send_rate_limit' || code === 'over_request_rate_limit' || status === 429)
    return new AuthError('Demasiados intentos. Espera unos minutos e intenta de nuevo.');
  if (code === 'signup_disabled') return new AuthError('El registro está deshabilitado temporalmente.');
  if (code === 'session_not_found' || code === 'refresh_token_not_found') return new AuthError('Tu sesión expiró. Inicia sesión de nuevo.');
  if (msg.includes('provider is not enabled')) return new AuthError('Este método de acceso no está habilitado.');
  if (msg.includes('fetch')) return new AuthError('No pudimos conectar. Revisa tu conexión e intenta de nuevo.');
  return new AuthError('No pudimos completar la operación. Intenta de nuevo.');
}

/* ---------- Perfil desde la API ---------- */

/** PerfilConsulta de la API. Rol: 0 = Cliente, 1 = Administrador (enum numérico de C#). */
type ApiProfile = { id: string; nombre: string; telefono: string | null; rol: number | string; activo: boolean };
type ApiMembership = { id: string; inicioVigencia: string; finVigenciaExclusivo: string };

function mapRole(rol: number | string): Role {
  // Acepta número (actual) o texto (si el backend agrega JsonStringEnumConverter o el rol Recepción).
  if (rol === 1 || rol === 'Administrador') return 'admin';
  if (rol === 'Recepcion' || rol === 'Recepción') return 'recepcion';
  return 'cliente';
}

const today = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

const minusOneDay = (iso: string) => {
  const d = new Date(`${iso}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() - 1);
  return d.toISOString().slice(0, 10);
};

async function loadMembership(): Promise<Membership | null> {
  try {
    const m = await apiGet<ApiMembership>('/api/membresias/vigente', { fechaLocal: today() });
    // La API aún no expone el nombre del plan (pendiente CLAUDE.md §8).
    return { plan: 'Socio CARTAQ', number: m.id.slice(0, 8).toUpperCase(), validUntil: minusOneDay(m.finVigenciaExclusivo), gold: false };
  } catch {
    return null; // 404 = sin membresía; otros errores no deben impedir el acceso.
  }
}

const meta = (u: User) => (u.user_metadata ?? {}) as { nombre?: string; telefono?: string; full_name?: string; name?: string };
const fallbackName = (u: User) => meta(u).nombre || meta(u).full_name || meta(u).name || u.email?.split('@')[0] || 'Usuario';

/**
 * Construye el usuario de la sesión: lee el perfil de la API y, si aún no existe
 * (primer inicio de sesión tras confirmar el correo), lo crea con los datos del registro.
 */
async function buildUser(session: Session): Promise<SessionUser> {
  const u = session.user;
  const base: SessionUser = {
    id: u.id,
    email: u.email ?? '',
    name: fallbackName(u),
    phone: meta(u).telefono ?? '',
    role: 'cliente',
    membership: null,
    profileSynced: false,
  };

  let profile: ApiProfile;
  try {
    try {
      profile = await apiGet<ApiProfile>('/api/perfiles/yo');
    } catch (e) {
      if (!(e instanceof ApiError) || e.status !== 404) throw e;
      // Primer acceso: el registro público crea siempre un CLIENTE (la API asigna el rol).
      profile = await apiPut<ApiProfile>('/api/perfiles/yo', {
        nombre: base.name.slice(0, 150),
        telefono: base.phone || null,
      });
    }
  } catch (e) {
    if (e instanceof ApiError && e.status === 403) {
      await client().auth.signOut({ scope: 'local' });
      throw new AuthError('Tu cuenta está desactivada. Contacta al club.');
    }
    return base; // API no disponible: acceso mínimo, se reintenta en el próximo inicio.
  }

  if (!profile.activo) {
    await client().auth.signOut({ scope: 'local' });
    throw new AuthError('Tu cuenta está desactivada. Contacta al club.');
  }

  return {
    ...base,
    name: profile.nombre,
    phone: profile.telefono ?? '',
    role: mapRole(profile.rol),
    membership: await loadMembership(),
    profileSynced: true,
  };
}

/* ---------- Operaciones ---------- */

export async function getSession(): Promise<SessionUser | null> {
  const { data, error } = await client().auth.getSession();
  if (error || !data.session) return null;
  try {
    return await buildUser(data.session);
  } catch {
    return null;
  }
}

/** Notifica inicios/cierres de sesión (otras pestañas, expiración, confirmación de correo). */
export function onSessionChange(cb: (event: string, session: Session | null) => void): () => void {
  const { data } = client().auth.onAuthStateChange((event, session) => cb(event, session));
  return () => data.subscription.unsubscribe();
}

export { buildUser };

export async function signIn(email: string, password: string, remember: boolean): Promise<SessionUser> {
  setRememberSession(remember);
  const { data, error } = await client().auth.signInWithPassword({ email: email.trim().toLowerCase(), password });
  if (error || !data.session) throw translate(error ?? new Error('sin sesión'));
  return buildUser(data.session);
}

export async function signInWithGoogle(next?: string): Promise<never> {
  setRememberSession(true);
  const redirectTo = `${origin()}/login${next ? `?next=${encodeURIComponent(next)}` : ''}`;
  const { error } = await client().auth.signInWithOAuth({ provider: 'google', options: { redirectTo } });
  if (error) throw translate(error);
  // El navegador se va a Google; esta promesa no se resuelve.
  return new Promise<never>(() => {});
}

export async function signUp(data: SignUpData): Promise<SignUpResult> {
  setRememberSession(true);
  const email = data.email.trim().toLowerCase();
  const { data: res, error } = await client().auth.signUp({
    email,
    password: data.password,
    options: {
      // Solo datos de presentación; el rol NUNCA se toma de aquí.
      data: { nombre: data.nombre.trim(), telefono: data.telefono },
      emailRedirectTo: `${origin()}/login?confirmado=1`,
    },
  });
  if (error) throw translate(error);
  if (res.session) return { status: 'signed_in', user: await buildUser(res.session) };
  // Sin sesión = hay que confirmar el correo. (Si el correo ya existía, Supabase responde igual
  // para no revelarlo; el mensaje es el mismo.)
  return { status: 'confirm_email', email };
}

export async function resendConfirmation(email: string): Promise<void> {
  const { error } = await client().auth.resend({
    type: 'signup',
    email: email.trim().toLowerCase(),
    options: { emailRedirectTo: `${origin()}/login?confirmado=1` },
  });
  if (error && error.status === 429) throw translate(error);
}

export async function signOut(): Promise<void> {
  // "local": cierra la sesión en este navegador; los demás dispositivos siguen activos.
  await client().auth.signOut({ scope: 'local' });
}

export async function resetPassword(email: string): Promise<void> {
  const { error } = await client().auth.resetPasswordForEmail(email.trim().toLowerCase(), {
    redirectTo: `${origin()}/restablecer`,
  });
  // Solo se informa el límite de envíos; nunca si el correo existe o no.
  if (error && error.status === 429) throw translate(error);
}

export async function updatePassword(password: string): Promise<void> {
  const { error } = await client().auth.updateUser({ password });
  if (error) throw translate(error);
}

export async function updateProfile(current: SessionUser, data: ProfileData): Promise<SessionUser> {
  const profile = await apiPut<ApiProfile>('/api/perfiles/yo', {
    nombre: data.nombre.trim(),
    telefono: data.telefono || null,
  }).catch((e: unknown) => {
    throw new AuthError(e instanceof ApiError ? e.message : 'No pudimos guardar tus datos.');
  });

  let email = current.email;
  const newEmail = data.email.trim().toLowerCase();
  if (newEmail !== current.email.toLowerCase()) {
    // Supabase envía un enlace de confirmación; el correo cambia hasta que se confirme.
    const { error } = await client().auth.updateUser({ email: newEmail }, { emailRedirectTo: `${origin()}/perfil` });
    if (error) throw translate(error);
    email = current.email;
  }

  return { ...current, name: profile.nombre, phone: profile.telefono ?? '', email };
}

export async function activateMembership(): Promise<SessionUser> {
  throw new AuthError('La contratación de membresías en línea se habilitará cuando el pago esté conectado. Mientras tanto, contrátala en el club.');
}
