import { useEffect, useRef, useState } from 'react';
import { Button } from '@/components/ui';
import { IconClose } from '@/components/icons/icons';
import styles from './QrScanner.module.css';

type Detector = { detect: (src: HTMLVideoElement) => Promise<{ rawValue: string }[]> };
type DetectorCtor = new (opts: { formats: string[] }) => Detector;

/**
 * Escáner de QR con la cámara (API BarcodeDetector del navegador; Chrome/Edge en Android y escritorio).
 * Si el navegador no la soporta, se muestra un aviso para escribir el código.
 */
export function QrScanner({ open, onClose, onCode }: { open: boolean; onClose: () => void; onCode: (code: string) => void }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const Ctor = (window as unknown as { BarcodeDetector?: DetectorCtor }).BarcodeDetector;
    if (!Ctor || !navigator.mediaDevices?.getUserMedia) {
      setError('Este navegador no puede escanear códigos. Escribe el código de la reserva (PCP-…).');
      return;
    }
    setError(null);
    let stream: MediaStream | null = null;
    let raf = 0;
    let stopped = false;
    const detector = new Ctor({ formats: ['qr_code'] });

    navigator.mediaDevices
      .getUserMedia({ video: { facingMode: 'environment' } })
      .then((s) => {
        stream = s;
        const video = videoRef.current;
        if (!video || stopped) return;
        video.srcObject = s;
        void video.play();
        const tick = async () => {
          if (stopped) return;
          try {
            const codes = await detector.detect(video);
            if (codes[0]?.rawValue) {
              onCode(codes[0].rawValue);
              return;
            }
          } catch {
            /* cuadro sin datos todavía */
          }
          raf = requestAnimationFrame(() => void tick());
        };
        void tick();
      })
      .catch(() => setError('No pudimos usar la cámara. Revisa los permisos o escribe el código.'));

    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
      stream?.getTracks().forEach((t) => t.stop());
    };
  }, [open, onCode]);

  return (
    <dialog ref={dialogRef} className={styles.dialog} aria-label="Escanear QR de llegada" onClose={onClose}>
      <div className={styles.head}>
        <strong>Escanear QR de llegada</strong>
        <button type="button" className={styles.close} aria-label="Cerrar" onClick={onClose}>
          <IconClose size={20} />
        </button>
      </div>
      {error ? (
        <p className={styles.error}>{error}</p>
      ) : (
        <div className={styles.viewport}>
          <video ref={videoRef} className={styles.video} muted playsInline />
          <span className={styles.frame} aria-hidden="true" />
        </div>
      )}
      <Button variant="secondary" size="lg" fullWidth onClick={onClose}>
        Cancelar
      </Button>
    </dialog>
  );
}
