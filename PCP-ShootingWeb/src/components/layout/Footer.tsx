import { Link } from 'react-router';
import logoWhite from '@/assets/logo-pcp-blanco.png';
import { club, footerNav } from '@/app/navigation';
import styles from './Footer.module.css';

/**
 * Footer público. En escritorio/tableta: columnas de enlaces.
 * En celular: versión compacta del diseño (dirección, teléfono, WhatsApp y redes).
 */
export function Footer({ compact = false }: { compact?: boolean }) {
  const year = new Date().getFullYear();

  // Versión mínima de las pantallas de acceso (Login.dc): solo la franja inferior.
  if (compact) {
    return (
      <footer className={`${styles.footer} ${styles.compact}`}>
        <div className={`container ${styles.bottomInner}`}>
          <span>
            © {year} PCP Shooting · {club.address} · {club.phone}
          </span>
          <span className={styles.legal}>
            <a href={club.privacyHref} target="_blank" rel="noreferrer">
              Políticas de privacidad
            </a>
            <a href={club.rulesHref} target="_blank" rel="noreferrer">
              Reglamento del campo
            </a>
          </span>
        </div>
      </footer>
    );
  }

  return (
    <footer className={styles.footer}>
      <div className={`container ${styles.grid}`}>
        <div className={styles.brand}>
          <img src={logoWhite} alt="PCP Shooting" className={styles.logo} width={116} height={64} />
          <p className={`${styles.about} ${styles.desktopOnly}`}>
            El primer club de tiro exclusivo para armas de aire comprimido en México. Rifles PCP para tiro
            deportivo y recreativo, con energía máxima de 140 joules.
          </p>
        </div>

        {footerNav.map((column) => (
          <div key={column.heading} className={`${styles.column} ${styles.desktopOnly}`}>
            <h2 className={styles.heading}>{column.heading}</h2>
            {column.items.map((item) => (
              <Link key={item.label} to={item.to} className={styles.link}>
                {item.label}
              </Link>
            ))}
          </div>
        ))}

        <div className={styles.column}>
          <h2 className={`${styles.heading} ${styles.desktopOnly}`}>Visítanos</h2>
          <span>{club.address}</span>
          <span>{club.hours}</span>
          <div className={styles.contactRow}>
            <a href={club.phoneHref} className={styles.link}>
              {club.phone}
            </a>
            <a href={club.whatsappHref} className={`${styles.link} ${styles.mobileOnly}`} target="_blank" rel="noreferrer">
              WhatsApp
            </a>
          </div>
          <a href={`mailto:${club.email}`} className={`${styles.link} ${styles.desktopOnly}`}>
            {club.email}
          </a>
          <div className={styles.social}>
            {club.social.map((s) => (
              <a key={s.label} href={s.href} target="_blank" rel="noreferrer">
                {s.label}
              </a>
            ))}
          </div>
        </div>
      </div>

      <div className={styles.bottomBar}>
        <div className={`container ${styles.bottomInner}`}>
          <span>© {year} PCP Shooting. Todos los derechos reservados.</span>
          <span className={styles.legal}>
            <a href={club.privacyHref} target="_blank" rel="noreferrer">
              Políticas de privacidad
            </a>
            <a href={club.rulesHref} target="_blank" rel="noreferrer">
              Reglamento del campo
            </a>
          </span>
        </div>
      </div>
    </footer>
  );
}
