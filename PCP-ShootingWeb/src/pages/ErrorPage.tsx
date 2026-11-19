import { isRouteErrorResponse, Link, useRouteError } from 'react-router';
import styles from './StatusPage.module.css';

/** Error inesperado al cargar o mostrar una pantalla. Se muestra sin layout por si el layout falló. */
export function ErrorPage() {
  const error = useRouteError();
  const detail = isRouteErrorResponse(error)
    ? `${error.status} ${error.statusText}`
    : error instanceof Error
      ? error.message
      : null;

  return (
    <section className={`${styles.wrap} ${styles.full}`}>
      <title>Algo salió mal · PCP Shooting</title>
      <span className={styles.code}>Error</span>
      <h1 className={styles.title}>Algo salió mal</h1>
      <p className={styles.text}>No pudimos mostrar esta página. Intenta de nuevo en un momento.</p>
      {import.meta.env.DEV && detail && <pre className={styles.detail}>{detail}</pre>}
      <Link to="/" className={styles.button} reloadDocument>
        Volver al inicio
      </Link>
    </section>
  );
}
