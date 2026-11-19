import { useEffect, useRef, useState } from 'react';
import { cx } from '@/lib/format';
import styles from './SignaturePad.module.css';

type Props = {
  /** Imagen PNG (data URL) de la firma o null si está vacía. */
  value: string | null;
  onChange: (value: string | null) => void;
  invalid?: boolean;
  describedBy?: string;
};

/**
 * Área de firma con dedo o mouse (canvas + pointer events).
 * PENDIENTE (CLAUDE.md §8): validez legal y almacenamiento de la firma.
 */
export function SignaturePad({ value, onChange, invalid, describedBy }: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawing = useRef(false);
  const [empty, setEmpty] = useState(!value);

  // Ajusta el canvas a su tamaño real (y a pantallas de alta densidad); redibuja la firma guardada.
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const resize = () => {
      const ratio = window.devicePixelRatio || 1;
      const { width, height } = canvas.getBoundingClientRect();
      canvas.width = Math.round(width * ratio);
      canvas.height = Math.round(height * ratio);
      const ctx = canvas.getContext('2d');
      if (!ctx) return;
      ctx.scale(ratio, ratio);
      ctx.lineWidth = 2.2;
      ctx.lineCap = 'round';
      ctx.lineJoin = 'round';
      ctx.strokeStyle = '#000000';
      if (value) {
        const img = new Image();
        img.onload = () => ctx.drawImage(img, 0, 0, width, height);
        img.src = value;
      }
    };
    resize();
    window.addEventListener('resize', resize);
    return () => window.removeEventListener('resize', resize);
    // Solo al montar: después la firma vive en el canvas.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const point = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  };

  const start = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const ctx = e.currentTarget.getContext('2d');
    if (!ctx) return;
    e.currentTarget.setPointerCapture(e.pointerId);
    drawing.current = true;
    const { x, y } = point(e);
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + 0.1, y + 0.1);
    ctx.stroke();
    setEmpty(false);
  };

  const move = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!drawing.current) return;
    const ctx = e.currentTarget.getContext('2d');
    if (!ctx) return;
    const { x, y } = point(e);
    ctx.lineTo(x, y);
    ctx.stroke();
  };

  const end = () => {
    if (!drawing.current) return;
    drawing.current = false;
    onChange(canvasRef.current?.toDataURL('image/png') ?? null);
  };

  const clear = () => {
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext('2d');
    if (!canvas || !ctx) return;
    ctx.save();
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.restore();
    setEmpty(true);
    onChange(null);
  };

  return (
    <div className={styles.wrap}>
      <div className={cx(styles.pad, invalid && styles.invalid)}>
        <canvas
          ref={canvasRef}
          className={styles.canvas}
          role="img"
          aria-label={empty ? 'Área de firma vacía' : 'Firma capturada'}
          aria-describedby={describedBy}
          onPointerDown={start}
          onPointerMove={move}
          onPointerUp={end}
          onPointerCancel={end}
          onPointerLeave={end}
        />
        {empty && (
          <span className={styles.placeholder} aria-hidden="true">
            Firma aquí con el dedo o el mouse
          </span>
        )}
      </div>
      {!empty && (
        <button type="button" className={styles.clear} onClick={clear}>
          Borrar firma
        </button>
      )}
    </div>
  );
}
