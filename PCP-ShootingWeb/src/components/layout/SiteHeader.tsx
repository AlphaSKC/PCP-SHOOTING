import { Link, NavLink, useLocation } from 'react-router';
import { useAuth } from '@/features/auth/AuthProvider';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import { mainNav } from '@/app/navigation';
import { IconCart, IconMenu, IconUser } from '@/components/icons/icons';
import styles from './SiteHeader.module.css';

type Props = {
  cartCount: number;
  menuOpen: boolean;
  onOpenMenu: () => void;
  /** En tableta/escritorio el carrito abre el panel lateral; en celular va a /tienda/carrito. */
  onOpenCart: () => void;
};

const opensDrawer = () => window.matchMedia('(min-width: 768px)').matches;

/**
 * Header público. Un solo componente para los 3 tamaños:
 * - escritorio: menú, logo, navegación, cuenta, carrito y "Reservar línea"
 * - tableta: igual pero sin la navegación central
 * - celular (MobileTopBar del diseño): menú, logo centrado y carrito
 */
const ACCOUNT_PATHS = ['/login', '/registro', '/recuperar', '/perfil'];

export function SiteHeader({ cartCount, menuOpen, onOpenMenu, onOpenCart }: Props) {
  const { user } = useAuth();
  const { pathname } = useLocation();
  const accountActive = ACCOUNT_PATHS.includes(pathname);

  return (
    <header className={styles.header}>
      <div className={`container ${styles.inner}`}>
        <div className={styles.start}>
          <button
            type="button"
            className={styles.iconButton}
            aria-label="Abrir menú"
            aria-haspopup="dialog"
            aria-expanded={menuOpen}
            onClick={onOpenMenu}
          >
            <IconMenu />
          </button>
          <Link to="/" className={styles.logoLink} aria-label="PCP Shooting, inicio">
            <img src={logoWhite} alt="PCP Shooting" className={styles.logo} width={80} height={44} />
          </Link>
        </div>

        <nav aria-label="Principal" className={styles.nav}>
          {mainNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className={styles.end}>
          <Link
            to={user ? '/perfil' : '/login'}
            className={`${styles.iconButton} ${styles.account} ${accountActive ? styles.iconButtonActive : ''}`}
            aria-label={user ? `Mi cuenta: ${user.name}` : 'Iniciar sesión'}
            aria-current={accountActive ? 'page' : undefined}
          >
            <IconUser />
          </Link>
          <Link
            to="/tienda/carrito"
            className={`${styles.iconButton} ${styles.cart}`}
            aria-label={cartCount > 0 ? `Carrito, ${cartCount} productos` : 'Carrito vacío'}
            onClick={(e) => {
              if (opensDrawer()) {
                e.preventDefault();
                onOpenCart();
              }
            }}
          >
            <IconCart />
            {cartCount > 0 && (
              <span className={styles.badge} aria-hidden="true">
                {cartCount}
              </span>
            )}
          </Link>
          <Link to="/reservar" className={styles.cta}>
            Reservar línea
          </Link>
        </div>
      </div>
    </header>
  );
}
