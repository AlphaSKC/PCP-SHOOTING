import { useNavigate, useSearchParams } from 'react-router';
import { safeNext } from '@/features/auth/schemas';
import { isStaff, type SessionUser } from '@/features/auth/types';

/** A dónde ir después de entrar: ?next= o, si no hay, al panel (staff) o al perfil (clientes). */
export function useAfterLogin() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const next = safeNext(params.get('next'));
  const target = (user: SessionUser) => next ?? (isStaff(user.role) ? '/admin' : '/perfil');
  return {
    target,
    go: (user: SessionUser) => navigate(target(user), { replace: true }),
  };
}
