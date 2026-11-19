import { NavLink } from 'react-router';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import type { AdminNavItem } from '@/app/navigation';
import { IconLogout } from '@/components/icons/icons';
import { initialsOf, ROLE_LABEL, type SessionUser } from '@/features/auth/types';
import styles from './AdminSidebar.module.css';

type Props = {
  items: AdminNavItem[];
  user: SessionUser;
  onSignOut: () => void;
};

/** Barra lateral del panel, solo en escritorio (≥ 1024px). */
export function AdminSidebar({ items, user, onSignOut }: Props) {
  return (
    <aside className={styles.sidebar}>
      <div className={styles.brand}>
        <img src={logoWhite} alt="PCP Shooting" className={styles.logo} width={80} height={44} />
        <span className={styles.eyebrow}>Panel administrador</span>
      </div>

      <nav aria-label="Panel" className={styles.nav}>
        {items.map(({ label, to, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) => `${styles.link} ${isActive ? styles.active : ''}`}
          >
            <Icon size={20} className={styles.icon} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className={styles.user}>
        <span className={styles.avatar} aria-hidden="true">
          {initialsOf(user.name)}
        </span>
        <span className={styles.userText}>
          <span className={styles.userName}>{user.name}</span>
          <span className={styles.userRole}>{ROLE_LABEL[user.role]}</span>
        </span>
        <button type="button" className={styles.logout} aria-label="Cerrar sesión" title="Cerrar sesión" onClick={onSignOut}>
          <IconLogout size={18} />
        </button>
      </div>
    </aside>
  );
}
