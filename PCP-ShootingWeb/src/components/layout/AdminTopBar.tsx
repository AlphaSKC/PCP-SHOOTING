import { IconMenu } from '@/components/icons/icons';
import { initialsOf, ROLE_LABEL, type SessionUser } from '@/features/auth/types';
import styles from './AdminTopBar.module.css';

type Props = {
  user: SessionUser;
  menuOpen: boolean;
  onOpenMenu: () => void;
};

/** Barra superior negra del panel en tableta y celular (< 1024px). */
export function AdminTopBar({ user, menuOpen, onOpenMenu }: Props) {
  return (
    <header className={styles.bar}>
      <button
        type="button"
        className={styles.menuButton}
        aria-label="Abrir menú del panel"
        aria-haspopup="dialog"
        aria-expanded={menuOpen}
        onClick={onOpenMenu}
      >
        <IconMenu />
      </button>
      <span className={styles.title}>Panel</span>
      <span className={styles.avatar} title={`${user.name} · ${ROLE_LABEL[user.role]}`} aria-hidden="true">
        {initialsOf(user.name)}
      </span>
    </header>
  );
}
