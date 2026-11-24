import { useEffect, useRef, useState } from 'react';
import { club } from '@/app/navigation';
import { Button, ButtonLink, FormAlert, QuantityStepper } from '@/components/ui';
import { IconCheck, IconClose } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { PaymentMethods, PaymentRedirect } from '@/features/pagos/PaymentMethods';
import { formatLongDay, formatMXN } from '@/lib/format';
import { EnrollError, enroll, type ClubEvent, type Enrollment } from './api';
import styles from './EnrollDialog.module.css';

/** Máximo de lugares por inscripción. PENDIENTE de confirmar con el club. */
const MAX_PLACES = 4;

type Props = {
  event: ClubEvent | null;
  onClose: () => void;
  onEnrolled: () => void;
};

/**
 * Inscripción a un evento: lugares → pago en Mercado Pago (simulado) → confirmación.
 * Requiere cuenta ("Inscríbete en línea, aparta tu lugar y paga desde tu cuenta").
 */
export function EnrollDialog({ event, onClose, onEnrolled }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const { user } = useAuth();
  const [places, setPlaces] = useState(1);
  const [phase, setPhase] = useState<'idle' | 'paying'>('idle');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<Enrollment | null>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (event && !dialog.open) {
      setPlaces(1);
      setPhase('idle');
      setError(null);
      setDone(null);
      dialog.showModal();
      closeRef.current?.focus();
    }
    if (!event && dialog.open) dialog.close();
  }, [event]);

  const left = event ? event.capacity - event.taken : 0;
  const max = Math.min(MAX_PLACES, left);

  const pay = async () => {
    if (!event) return;
    setError(null);
    setPhase('paying');
    try {
      setDone(await enroll(event.id, places));
      onEnrolled();
    } catch (e) {
      setError(e instanceof EnrollError ? e.message : 'No pudimos completar tu inscripción. Intenta de nuevo.');
    } finally {
      setPhase('idle');
    }
  };

  return (
    <dialog
      ref={ref}
      className={styles.dialog}
      aria-label={event ? `Inscripción: ${event.name}` : 'Inscripción'}
      onClose={onClose}
      onClick={(e) => {
        if (e.target === ref.current && phase === 'idle') onClose();
      }}
    >
      {event && (
        <div className={styles.panel}>
          <header className={styles.header}>
            <div>
              <span className={styles.eyebrow}>Inscripción</span>
              <h2 className={styles.title}>{event.name}</h2>
              <p className={styles.meta}>
                {formatLongDay(event.date)} · {event.start} – {event.end}
              </p>
            </div>
            <button ref={closeRef} type="button" className={styles.close} aria-label="Cerrar" onClick={onClose}>
              <IconClose size={20} />
            </button>
          </header>

          {done ? (
            <div className={styles.body}>
              <div className={styles.success}>
                <span className={styles.check} aria-hidden="true">
                  <IconCheck size={28} strokeWidth={3} />
                </span>
                <h3>¡Estás inscrito!</h3>
                <p>
                  Folio <strong>{done.folio}</strong> · {done.places} lugar{done.places === 1 ? '' : 'es'} ·{' '}
                  {formatMXN(done.total)}. Te enviamos los detalles por correo y WhatsApp.
                </p>
              </div>
              <div className={styles.actions}>
                <ButtonLink to="/perfil#eventos" size="lg">
                  Ver mis eventos
                </ButtonLink>
                <Button variant="secondary" size="lg" onClick={onClose}>
                  Cerrar
                </Button>
              </div>
            </div>
          ) : !user ? (
            <div className={styles.body}>
              <p className={styles.text}>
                Para apartar tu lugar y pagar necesitas una cuenta. Así verás tus inscripciones en tu perfil.
              </p>
              <div className={styles.actions}>
                <ButtonLink to={`/login?next=${encodeURIComponent('/eventos')}`} size="lg">
                  Iniciar sesión
                </ButtonLink>
                <ButtonLink to={`/registro?next=${encodeURIComponent('/eventos')}`} variant="secondary" size="lg">
                  Crear cuenta
                </ButtonLink>
              </div>
            </div>
          ) : (
            <div className={styles.body}>
              {error && <FormAlert>{error}</FormAlert>}
              <div className={styles.row}>
                <div>
                  <span className={styles.label}>Lugares</span>
                  <span className={styles.hint}>
                    Quedan {left} · máx. {MAX_PLACES} por inscripción
                  </span>
                </div>
                <QuantityStepper label="lugares" value={places} min={1} max={Math.max(1, max)} onChange={setPlaces} />
              </div>
              <div className={styles.total}>
                <span>Total</span>
                <strong>{formatMXN(event.price * places)}</strong>
              </div>
              <PaymentMethods intro="Pagas la inscripción en Mercado Pago:" />
              {phase === 'paying' && <PaymentRedirect />}
              <div className={styles.actions}>
                <Button size="lg" fullWidth loading={phase === 'paying'} onClick={pay}>
                  Pagar {formatMXN(event.price * places)}
                </Button>
              </div>
              <p className={styles.fine}>
                Al inscribirte aceptas el{' '}
                <a href={club.rulesHref} target="_blank" rel="noreferrer">
                  reglamento del campo
                </a>
                . Las políticas de cancelación se definirán con el club.
              </p>
            </div>
          )}
        </div>
      )}
    </dialog>
  );
}
