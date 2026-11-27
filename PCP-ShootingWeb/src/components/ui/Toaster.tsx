import { useEffect, useState } from 'react';
import { dismissToast, subscribeToasts, type ToastItem } from '@/lib/toast';
import styles from './Toaster.module.css';

const ICON: Record<ToastItem['tone'], string> = { success: '✓', error: '!', info: 'i' };

/** Contenedor de alertas emergentes. Se monta una sola vez en `main.tsx`. */
export function Toaster() {
  const [list, setList] = useState<ToastItem[]>([]);
  useEffect(() => subscribeToasts(setList), []);
  return (
    <div className={styles.region} aria-live="polite" aria-relevant="additions">
      {list.map((t) => (
        <div key={t.id} role={t.tone === 'error' ? 'alert' : 'status'} className={`${styles.toast} ${styles[t.tone]}`}>
          <span className={styles.icon} aria-hidden="true">{ICON[t.tone]}</span>
          <p className={styles.message}>{t.message}</p>
          <button type="button" className={styles.close} aria-label="Cerrar aviso" onClick={() => dismissToast(t.id)}>
            ×
          </button>
        </div>
      ))}
    </div>
  );
}
