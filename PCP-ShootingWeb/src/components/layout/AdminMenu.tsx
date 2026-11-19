import { useEffect, useRef } from 'react';
import { Link, NavLink } from 'react-router';
import type { AdminNavItem } from '@/app/navigation';
import { IconClose } from '@/components/icons/icons';
import { initialsOf, ROLE_LABEL, type SessionUser } from '@/features/auth/types';
import styles from './AdminMenu.module.css';

type Props = {
  open: boolean;
  onClose: () => void;
  items: AdminNavItem[];
  user: SessionUser;
  onSignOut: () => void;
};

/**
 * Menú del panel en tableta y celular (MAdminMenu del diseño).
 * Celular: pantalla completa. Tableta: panel lateral de 360px.
 */
export function AdminMenu({ open, onClose, items, user, onSignOut }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      dialog.showModal();
      closeRef.current?.focus();
    }
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      className={styles.dialog}
      aria-label="Menú del panel"
      onClose={onClose}
      onClick={(event) => {
        if (event.target === dialogRef.current) onClose();
      }}
    >
      <div className={styles.panel}>
        <header className={styles.header}>
          <button ref={closeRef} type="button" className={styles.close} aria-label="Cerrar menú" onClick={onClose}>
            <IconClose size={20} />
          </button>
          <span className={styles.title}>Panel administrador</span>
          <span className={styles.spacer} aria-hidden="true" />
        </header>

        <div className={styles.body}>
          <div className={styles.user}>
            <span className={styles.avatar} aria-hidden="true">
              {initialsOf(user.name)}
            </span>
            <span className={styles.userText}>
              <span className={styles.userName}>{user.name}</span>
              <span className={styles.userRole}>{ROLE_LABEL[user.role]}</span>
            </span>
          </div>

          <nav aria-label="Panel" className={styles.nav}>
            {items.map(({ label, to, icon: Icon, end }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                onClick={onClose}
                className={({ isActive }) => `${styles.link} ${isActive ? styles.active : ''}`}
              >
                <Icon className={styles.icon} />
                {label}
              </NavLink>
            ))}
          </nav>

          <div className={styles.actions}>
            <Link to="/" className={styles.secondary} onClick={onClose}>
              Ver sitio
            </Link>
            <button
              type="button"
              className={styles.logout}
              onClick={() => {
                onClose();
                onSignOut();
              }}
            >
              Cerrar sesión
            </button>
          </div>
        </div>
      </div>
    </dialog>
  );
}
