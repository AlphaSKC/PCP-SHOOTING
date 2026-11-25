import { useCallback, useEffect, useState } from 'react';
import { IconClose } from '@/components/icons/icons';
import { DATA_SOURCE } from '@/lib/api';
import { listPublicNotices, type PublicNotice } from './api';
import styles from './Avisos.module.css';

const DISMISSED = 'pcp.avisos.cerrados';
const REFRESH_MS = 5 * 60 * 1000;

const readDismissed = (): string[] => {
  try {
    return JSON.parse(sessionStorage.getItem(DISMISSED) ?? '[]') as string[];
  } catch {
    return [];
  }
};

/** Franja visual de avisos (también la usa la vista previa del panel). Texto plano: React lo escapa. */
export function NoticeStrip({ notices, onClose }: { notices: { id: string; title: string; text: string }[]; onClose?: (id: string) => void }) {
  if (notices.length === 0) return null;
  return (
    <div className={styles.strip}>
      {notices.map((n) => (
        <div key={n.id} className={styles.notice}>
          <p className={styles.noticeText}>
            {n.title && <strong>{n.title}: </strong>}
            {n.text}
          </p>
          {onClose && (
            <button type="button" className={styles.close} aria-label="Cerrar aviso" onClick={() => onClose(n.id)}>
              <IconClose size={16} />
            </button>
          )}
        </div>
      ))}
    </div>
  );
}

/**
 * Avisos públicos vigentes arriba del sitio (GET /api/avisos-publicos).
 * Se vuelve a consultar cada 5 min y al regresar a la pestaña; un aviso se oculta solo al llegar su fin.
 * Cerrar un aviso lo oculta durante la sesión del navegador. Si la API falla, no se muestra nada.
 */
export function NoticeBanner() {
  const [notices, setNotices] = useState<PublicNotice[]>([]);
  const [dismissed, setDismissed] = useState<string[]>(readDismissed);
  const [now, setNow] = useState(() => Date.now());

  const refresh = useCallback(() => {
    listPublicNotices().then(
      (list) => {
        setNotices(list.sort((a, b) => a.order - b.order));
        setNow(Date.now());
      },
      () => undefined,
    );
  }, []);

  useEffect(() => {
    if (DATA_SOURCE !== 'api') return;
    refresh();
    const timer = window.setInterval(refresh, REFRESH_MS);
    const tick = window.setInterval(() => setNow(Date.now()), 30_000);
    const onVisible = () => document.visibilityState === 'visible' && refresh();
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      window.clearInterval(timer);
      window.clearInterval(tick);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, [refresh]);

  const visible = notices.filter(
    (n) => !dismissed.includes(n.id) && (!n.endUtc || new Date(n.endUtc).getTime() > now) && (!n.startUtc || new Date(n.startUtc).getTime() <= now),
  );

  function close(id: string) {
    const next = [...dismissed, id];
    setDismissed(next);
    try {
      sessionStorage.setItem(DISMISSED, JSON.stringify(next));
    } catch {
      /* sin almacenamiento: se cierra solo en esta vista */
    }
  }

  if (visible.length === 0) return null;
  return (
    <section aria-label="Avisos del club" className={styles.banner}>
      <NoticeStrip notices={visible.slice(0, 3)} onClose={close} />
    </section>
  );
}
