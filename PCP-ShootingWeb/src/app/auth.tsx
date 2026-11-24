import { Navigate, Outlet, useLocation } from 'react-router';
import { useAuth } from '@/features/auth/AuthProvider';
import type { Role } from '@/features/auth/types';
import { ForbiddenPage } from '@/pages/ForbiddenPage';
import { LoadingScreen } from '@/pages/LoadingScreen';

export type { Role, SessionUser } from '@/features/auth/types';
export { useAuth } from '@/features/auth/AuthProvider';

/** URL de login que regresa a la página actual después de entrar. */
function useLoginRedirect() {
  const { pathname, search } = useLocation();
  return `/login?next=${encodeURIComponent(pathname + search)}`;
}

/** Rutas que requieren sesión (p. ej. /perfil). */
export function RequireAuth() {
  const { status } = useAuth();
  const loginUrl = useLoginRedirect();
  if (status === 'loading') return <LoadingScreen />;
  if (status === 'anonymous') return <Navigate to={loginUrl} replace />;
  return <Outlet />;
}

/** Rutas por rol (panel: admin y recepción; algunas secciones solo admin). */
export function RequireRole({ roles }: { roles: Role[] }) {
  const { status, user } = useAuth();
  const loginUrl = useLoginRedirect();
  if (status === 'loading') return <LoadingScreen />;
  if (!user) return <Navigate to={loginUrl} replace />;
  if (!roles.includes(user.role)) return <ForbiddenPage />;
  return <Outlet />;
}
