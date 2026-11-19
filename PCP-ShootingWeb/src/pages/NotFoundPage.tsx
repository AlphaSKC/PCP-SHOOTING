import { Link } from 'react-router';
import styles from './StatusPage.module.css';

export function NotFoundPage({ admin = false }: { admin?: boolean }) {
  return (
    <section className={`${styles.wrap} ${admin ? styles.admin : ''}`}>
      <title>Página no encontrada · PCP Shooting</title>
      <span className={styles.code}>404</span>
      <h1 className={styles.title}>Página no encontrada</h1>
      <p className={styles.text}>La dirección no existe o cambió de lugar.</p>
      <Link to={admin ? '/admin' : '/'} className={styles.button}>
        {admin ? 'Volver al panel' : 'Volver al inicio'}
      </Link>
    </section>
  );
}
