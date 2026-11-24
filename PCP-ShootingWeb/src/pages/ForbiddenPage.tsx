import { Link } from 'react-router';
import { useAuth } from '@/features/auth/AuthProvider';
import { isStaff } from '@/features/auth/types';
import styles from './StatusPage.module.css';

/** El usuario tiene sesión pero su rol no permite ver esta sección. */
export function ForbiddenPage() {
  const { user } = useAuth();
  const staff = user ? isStaff(user.role) : false;
  return (
    <section className={`${styles.wrap} ${styles.full}`}>
      <title>Sin acceso · PCP Shooting</title>
      <span className={styles.code}>Acceso restringido</span>
      <h1 className={styles.title}>No tienes permiso para ver esta sección</h1>
      <p className={styles.text}>
        {staff
          ? 'Tu rol no incluye esta parte del panel. Si la necesitas, pídele acceso a un administrador.'
          : 'Esta sección es solo para el personal del club.'}
      </p>
      <Link to={staff ? '/admin' : '/'} className={styles.button}>
        {staff ? 'Volver al panel' : 'Volver al inicio'}
      </Link>
    </section>
  );
}
