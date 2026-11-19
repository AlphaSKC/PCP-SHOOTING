import { useEffect, useRef } from 'react';
import { Link } from 'react-router';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import { club, sideMenu } from '@/app/navigation';
import { IconChevronRight, IconClose, IconUser } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { initialsOf, isStaff, ROLE_LABEL } from '@/features/auth/types';
import styles from './SideMenu.module.css';

type Props = {
  open: boolean;
  onClose: () => void;
};

/**
 * Menú lateral (drawer) del sitio público. Usa <dialog> modal:
 * trae el foco atrapado, cierre con Escape y fondo inerte sin código extra.
 */
export function SideMenu({ open, onClose }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const { user } = useAuth();
  const roleText = user ? (user.role === 'cliente' ? user.membership?.plan : ROLE_LABEL[user.role]) : undefined;

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
      aria-label="Menú principal"
      onClose={onClose}
      onClick={(event) => {
        // Clic en el fondo oscuro (fuera del panel) cierra el menú.
        if (event.target === dialogRef.current) onClose();
      }}
    >
      <nav className={styles.panel} aria-label="Menú principal">
        <div className={styles.top}>
          <Link to="/" onClick={onClose} aria-label="PCP Shooting, inicio">
            <img src={logoWhite} alt="PCP Shooting" className={styles.logo} width={73} height={40} />
          </Link>
          <button ref={closeRef} type="button" className={styles.close} aria-label="Cerrar menú" onClick={onClose}>
            <IconClose size={20} />
          </button>
        </div>

        {user ? (
          <Link to="/perfil" className={styles.profile} onClick={onClose}>
            <span className={styles.avatar} aria-hidden="true">
              {initialsOf(user.name)}
            </span>
            <span className={styles.profileText}>
              <span className={styles.profileName}>Mi perfil</span>
              {roleText && (
                <span className={user.membership?.gold ? styles.profileGold : styles.profileHint}>{roleText}</span>
              )}
            </span>
          </Link>
        ) : (
          <Link to="/login" className={styles.profile} onClick={onClose}>
            <span className={styles.avatar} aria-hidden="true">
              <IconUser size={20} />
            </span>
            <span className={styles.profileText}>
              <span className={styles.profileName}>Mi cuenta</span>
              <span className={styles.profileHint}>Inicia sesión o regístrate</span>
            </span>
          </Link>
        )}
        {user && isStaff(user.role) && (
          <Link to="/admin" className={styles.adminLink} onClick={onClose}>
            Ir al panel administrativo
            <IconChevronRight size={16} />
          </Link>
        )}

        <div className={styles.list}>
          {sideMenu.map((group) => (
            <div key={group.heading} className={styles.group}>
              <span className={styles.heading}>{group.heading}</span>
              {group.items.map((item) => (
                <Link key={item.label} to={item.to} className={styles.link} onClick={onClose}>
                  {item.label}
                  <IconChevronRight size={16} className={styles.chevron} />
                </Link>
              ))}
            </div>
          ))}
        </div>

        <div className={styles.bottom}>
          <Link to="/reservar" className={styles.primary} onClick={onClose}>
            Reservar línea de tiro
          </Link>
          <a href={club.whatsappHref} className={styles.secondary} target="_blank" rel="noreferrer">
            WhatsApp · {club.phone}
          </a>
        </div>
      </nav>
    </dialog>
  );
}
