import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { IconChevronLeft, IconChevronRight } from '@/components/icons/icons';
import type { Brand } from '@/data/brands';
import { cx } from '@/lib/format';
import styles from './BrandCarousel.module.css';

/** Velocidad del desplazamiento continuo (px por segundo). */
const SPEED = 45;
const GAP = 16;

/**
 * "Marcas que nos acompañan": carrusel en bucle infinito.
 * - Se mueve solo y sin saltos: la lista se repite y, al terminar una vuelta, regresa
 *   a la posición equivalente sin que se note.
 * - Pausa al pasar el cursor, al enfocar con teclado, al tocar y cuando no está en pantalla.
 * - Flechas (tableta/escritorio) y deslizamiento con el dedo (celular).
 * - Si el usuario pidió reducir el movimiento, no se anima (se desplaza a mano).
 */
export function BrandCarousel({ brands }: { brands: Brand[] }) {
  const track = useRef<HTMLUListElement>(null);
  const pos = useRef(0);
  const [copies, setCopies] = useState(2);
  const [paused, setPaused] = useState(false);
  const [visible, setVisible] = useState(false);
  const resumeTimer = useRef<number | undefined>(undefined);

  /** Ancho de una vuelta (una copia de la lista, incluido el espacio entre tarjetas). */
  const loopWidth = useCallback(() => {
    const el = track.current;
    if (!el || brands.length === 0) return 0;
    const first = el.children[0] as HTMLElement | undefined;
    const second = el.children[brands.length] as HTMLElement | undefined;
    return first && second ? second.offsetLeft - first.offsetLeft : 0;
  }, [brands.length]);

  // Repite la lista las veces necesarias para llenar al menos dos anchos de pantalla.
  useLayoutEffect(() => {
    const measure = () => {
      const el = track.current;
      if (!el || brands.length === 0) return;
      const card = (el.children[0] as HTMLElement | undefined)?.offsetWidth ?? 200;
      const set = brands.length * (card + GAP);
      setCopies(Math.max(2, Math.ceil((el.clientWidth * 2) / set) + 1));
    };
    measure();
    window.addEventListener('resize', measure);
    return () => window.removeEventListener('resize', measure);
  }, [brands.length]);

  useEffect(() => {
    const el = track.current;
    if (!el || typeof IntersectionObserver === 'undefined') return;
    const io = new IntersectionObserver(([e]) => setVisible(Boolean(e?.isIntersecting)), { threshold: 0.2 });
    io.observe(el);
    return () => io.disconnect();
  }, []);

  // Animación continua con requestAnimationFrame (posición con decimales para que sea suave).
  useEffect(() => {
    const el = track.current;
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (!el || reduce || paused || !visible || brands.length === 0) return;
    pos.current = el.scrollLeft;
    let last = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const dt = Math.min(64, now - last) / 1000;
      last = now;
      // Si el usuario movió el carrusel a mano, se continúa desde ahí.
      if (Math.abs(el.scrollLeft - pos.current) > 2) pos.current = el.scrollLeft;
      pos.current += SPEED * dt;
      const w = loopWidth();
      if (w > 0 && pos.current >= w) pos.current -= w;
      el.scrollLeft = pos.current;
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [paused, visible, brands.length, copies, loopWidth]);

  /** Flechas: avanza o retrocede una tarjeta, sin llegar nunca a un extremo. */
  const step = (dir: 1 | -1) => {
    const el = track.current;
    if (!el) return;
    const w = loopWidth();
    if (w > 0 && dir === -1 && el.scrollLeft < 240) el.scrollLeft += w;
    if (w > 0 && dir === 1 && el.scrollLeft >= w) el.scrollLeft -= w;
    const card = (el.children[0] as HTMLElement | undefined)?.offsetWidth ?? 200;
    el.scrollBy({ left: dir * (card + GAP), behavior: 'smooth' });
    pos.current = el.scrollLeft;
  };

  const pauseForTouch = () => {
    setPaused(true);
    window.clearTimeout(resumeTimer.current);
  };
  const resumeAfterTouch = () => {
    window.clearTimeout(resumeTimer.current);
    resumeTimer.current = window.setTimeout(() => setPaused(false), 2500);
  };
  useEffect(() => () => window.clearTimeout(resumeTimer.current), []);

  if (brands.length === 0) return null;

  const items = Array.from({ length: copies }, (_, copy) =>
    brands.map((b, i) => {
      const clone = copy > 0;
      const content = (
        <>
          {b.logo ? (
            <img src={b.logo} alt={clone ? '' : b.name} className={styles.logo} loading="lazy" decoding="async" />
          ) : (
            <span className={styles.wordmark}>{b.name}</span>
          )}
          {b.tagline && <span className={styles.tagline}>{b.tagline}</span>}
        </>
      );
      return (
        // Las copias son decorativas: los lectores de pantalla solo leen la primera vuelta.
        <li key={`${copy}-${i}`} className={styles.item} aria-hidden={clone || undefined}>
          {b.url ? (
            <a
              href={b.url}
              target="_blank"
              rel="noopener noreferrer"
              className={styles.card}
              tabIndex={clone ? -1 : undefined}
              aria-label={clone ? undefined : `${b.name} (abre su sitio)`}
            >
              {content}
            </a>
          ) : (
            <div className={styles.card}>{content}</div>
          )}
        </li>
      );
    }),
  ).flat();

  return (
    <section className={styles.band} aria-labelledby="marcas-titulo">
      <div className={`container ${styles.inner}`}>
        <div className={styles.head}>
          <span className={styles.eyebrow}>Aliados del club</span>
          <h2 id="marcas-titulo" className={styles.title}>
            Marcas que nos acompañan
          </h2>
        </div>

        <div
          className={styles.viewport}
          onMouseEnter={() => setPaused(true)}
          onMouseLeave={() => setPaused(false)}
          onFocus={() => setPaused(true)}
          onBlur={() => setPaused(false)}
          onTouchStart={pauseForTouch}
          onTouchEnd={resumeAfterTouch}
        >
          <button type="button" className={cx(styles.arrow, styles.prev)} aria-label="Marcas anteriores" onClick={() => step(-1)}>
            <IconChevronLeft size={22} />
          </button>
          <ul ref={track} className={styles.track} aria-label="Marcas aliadas">
            {items}
          </ul>
          <button type="button" className={cx(styles.arrow, styles.next)} aria-label="Más marcas" onClick={() => step(1)}>
            <IconChevronRight size={22} />
          </button>
        </div>
      </div>
    </section>
  );
}
