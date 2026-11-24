import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import * as service from './authService';
import { buildUser, onSessionChange } from './authSupabase';
import type { ProfileData, SessionUser, SignUpData, SignUpResult } from './types';

type Status = 'loading' | 'authenticated' | 'anonymous';

type AuthContextValue = {
  user: SessionUser | null;
  status: Status;
  /** true mientras el usuario llegó desde el enlace de "recuperar contraseña". */
  recovering: boolean;
  signIn: (email: string, password: string, remember: boolean) => Promise<SessionUser>;
  signInWithGoogle: (next?: string) => Promise<SessionUser>;
  signUp: (data: SignUpData) => Promise<SignUpResult>;
  resendConfirmation: (email: string) => Promise<void>;
  signOut: () => Promise<void>;
  resetPassword: (email: string) => Promise<void>;
  updatePassword: (password: string) => Promise<void>;
  updateProfile: (data: ProfileData) => Promise<SessionUser>;
  activateMembership: (plan: { name: string; gold: boolean }, months: number) => Promise<SessionUser>;
  /** Vuelve a leer perfil, rol y membresía desde la API. */
  refresh: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [status, setStatus] = useState<Status>('loading');
  const [recovering, setRecovering] = useState(false);
  const userRef = useRef<SessionUser | null>(null);
  userRef.current = user;

  const apply = useCallback((u: SessionUser | null) => {
    setUser(u);
    setStatus(u ? 'authenticated' : 'anonymous');
    return u;
  }, []);

  useEffect(() => {
    let active = true;
    service.getSession().then((u) => active && apply(u));

    if (service.AUTH_MODE !== 'supabase') return () => void (active = false);

    const unsubscribe = onSessionChange((event, session) => {
      // Supabase recomienda no llamar a la API de auth dentro del callback: se difiere.
      setTimeout(() => {
        if (!active) return;
        if (event === 'PASSWORD_RECOVERY') setRecovering(true);
        if (event === 'SIGNED_OUT' || !session) {
          setRecovering(false);
          apply(null);
          return;
        }
        // Inicio de sesión en otra pestaña, confirmación de correo, OAuth o cambio de datos.
        const same = userRef.current?.id === session.user.id;
        if (event === 'SIGNED_IN' && same) return;
        if (event === 'SIGNED_IN' || event === 'USER_UPDATED' || event === 'PASSWORD_RECOVERY') {
          buildUser(session).then((u) => active && apply(u)).catch(() => active && apply(null));
        }
      }, 0);
    });

    return () => {
      active = false;
      unsubscribe();
    };
  }, [apply]);

  const accept = useCallback((u: SessionUser) => apply(u) as SessionUser, [apply]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      status,
      recovering,
      signIn: (email, password, remember) => service.signIn(email, password, remember).then(accept),
      signInWithGoogle: (next) => service.signInWithGoogle(next).then(accept),
      signUp: async (data) => {
        const result = await service.signUp(data);
        if (result.status === 'signed_in') accept(result.user);
        return result;
      },
      resendConfirmation: service.resendConfirmation,
      signOut: async () => {
        await service.signOut();
        setRecovering(false);
        apply(null);
      },
      resetPassword: service.resetPassword,
      updatePassword: async (password) => {
        await service.updatePassword(password);
        setRecovering(false);
      },
      updateProfile: async (data) => {
        if (!user) throw new Error('Sin sesión');
        return accept(await service.updateProfile(user, data));
      },
      activateMembership: async (plan, months) => {
        if (!user) throw new Error('Sin sesión');
        return accept(await service.activateMembership(user, plan, months));
      },
      refresh: async () => {
        apply(await service.getSession());
      },
    }),
    [user, status, recovering, accept, apply],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de <AuthProvider>.');
  return ctx;
}
