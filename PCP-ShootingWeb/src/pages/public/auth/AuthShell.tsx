import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import { Photo } from '@/components/ui';
import { IconCheck } from '@/components/icons/icons';
import { AUTH_MODE } from '@/features/auth/authService';
import styles from './AuthShell.module.css';

const BENEFITS = [
  'Reserva y paga tus sesiones en línea',
  'Consulta tus próximas reservaciones',
  'Sigue tus pedidos de la tienda',
  'Inscríbete a eventos y torneos',
];

type Props = {
  /** Muestra las pestañas Iniciar sesión / Crear cuenta. */
  tabs?: boolean;
  /** Conserva ?next= al cambiar de pestaña. */
  search?: string;
  children: ReactNode;
};

/**
 * Marco de las pantallas de acceso (Login.dc / MLogin / MRegistro).
 * Escritorio: panel negro con beneficios + formulario. Tableta y celular: franja con logo + formulario.
 */
export function AuthShell({ tabs = false, search = '', children }: Props) {
  return (
    <>
      <div className={styles.mobileBand}>
        <img src={logoWhite} alt="PCP Shooting" className={styles.bandLogo} width={131} height={72} />
        <p>Reserva, compra y sigue tus pedidos desde tu cuenta.</p>
      </div>

      <div className={`container ${styles.page}`}>
        <div className={styles.card}>
          <aside className={styles.side}>
            <div className={styles.sidePhoto}>
              <Photo alt="Foto: tirador en el campo" className={styles.darkPlaceholder} />
            </div>
            <div className={styles.sideText}>
              <p className={styles.sideTitle}>Tu cuenta PCP</p>
              <p className={styles.sideLead}>Todo tu historial en un solo lugar.</p>
            </div>
            <ul className={styles.benefits}>
              {BENEFITS.map((b) => (
                <li key={b}>
                  <span className={styles.check} aria-hidden="true">
                    <IconCheck size={16} strokeWidth={3} />
                  </span>
                  {b}
                </li>
              ))}
            </ul>
          </aside>

          <div className={`${styles.main} ${tabs ? styles.withTabs : ''}`}>
            {tabs && (
              <nav aria-label="Acceso" className={styles.tabs}>
                <NavLink to={`/login${search}`} className={({ isActive }) => `${styles.tab} ${isActive ? styles.tabActive : ''}`}>
                  Iniciar sesión
                </NavLink>
                <NavLink
                  to={`/registro${search}`}
                  className={({ isActive }) => `${styles.tab} ${isActive ? styles.tabActive : ''}`}
                >
                  Crear cuenta
                </NavLink>
              </nav>
            )}
            {children}
            {AUTH_MODE === 'demo' && (
              <p className={styles.demo}>
                <strong>Modo demostración:</strong> entra con cualquier correo y una contraseña de 8 caracteres o más.
                Usa <code>admin@…</code> para el panel de administrador o <code>recepcion@…</code> para recepción.
              </p>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

/** Título y subtítulo de cada formulario de acceso. */
export function AuthHeading({ title, lead }: { title: string; lead: string }) {
  return (
    <div className={styles.heading}>
      <h1 className={styles.title}>{title}</h1>
      <p className={styles.lead}>{lead}</p>
    </div>
  );
}

/** Separador "o" entre el formulario y Google. */
export function OrDivider() {
  return (
    <div className={styles.divider} aria-hidden="true">
      <span />o<span />
    </div>
  );
}

/** "¿No tienes cuenta? Crea una aquí". */
export function AuthSwitch({ children }: { children: ReactNode }) {
  return <p className={styles.switch}>{children}</p>;
}
