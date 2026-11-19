import styles from './StatusPage.module.css';

/** Mientras se verifica la sesión. */
export function LoadingScreen() {
  return (
    <div className={styles.loading} role="status">
      <span className={styles.spinner} aria-hidden="true" />
      <span className="visually-hidden">Cargando…</span>
    </div>
  );
}
