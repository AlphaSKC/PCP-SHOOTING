import { IconLock } from '@/components/icons/icons';
import styles from './PaymentMethods.module.css';

const METHODS = [
  { title: 'Tarjeta', description: 'Crédito o débito' },
  { title: 'Efectivo', description: 'Paga en OXXO o tiendas' },
  { title: 'Mercado Pago', description: 'Con tu cuenta' },
];

/**
 * Métodos que acepta Mercado Pago (Checkout Pro). Es informativo: el cliente elige el método
 * en la página de Mercado Pago. La plataforma NO captura ni guarda datos de tarjeta (CLAUDE.md §6).
 */
export function PaymentMethods({ intro = 'Pagas el 100% en Mercado Pago, con el método que prefieras:' }: { intro?: string }) {
  return (
    <div className={styles.wrap}>
      <p className={styles.intro}>{intro}</p>
      <ul className={styles.methods}>
        {METHODS.map((m) => (
          <li key={m.title} className={styles.method}>
            <strong>{m.title}</strong>
            <span>{m.description}</span>
          </li>
        ))}
      </ul>
      <p className={styles.secure}>
        <IconLock size={16} />
        Pago protegido por Mercado Pago. No guardamos datos de tu tarjeta. Recibirás tu confirmación por correo y
        WhatsApp.
      </p>
    </div>
  );
}

/** Aviso "Redirigiendo a Mercado Pago…" mientras se crea el pago. */
export function PaymentRedirect() {
  return (
    <div className={styles.redirect} role="status">
      <span className={styles.spinner} aria-hidden="true" />
      Redirigiendo a Mercado Pago… <small>(simulación: el pago se aprueba solo)</small>
    </div>
  );
}
