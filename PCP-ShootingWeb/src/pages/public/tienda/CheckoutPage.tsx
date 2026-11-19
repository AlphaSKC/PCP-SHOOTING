import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { club } from '@/app/navigation';
import { Button, ButtonLink, EmptyState, FormAlert, MobileActionBar, TextField } from '@/components/ui';
import { IconArrowLeft, IconCart, IconLock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { PaymentMethods, PaymentRedirect } from '@/features/pagos/PaymentMethods';
import { OrderError, createOrder, simulatePayment } from '@/features/tienda/api';
import { useCart } from '@/features/tienda/CartProvider';
import { OrderLinesSummary } from '@/features/tienda/components/CartLines';
import { formatMXN } from '@/lib/format';
import styles from './TiendaFlow.module.css';

const schema = z.object({
  nombre: z.string().trim().min(2, 'Escribe tu nombre completo'),
  email: z.string().trim().min(1, 'Escribe tu correo electrónico').email('Escribe un correo válido'),
  telefono: z
    .string()
    .trim()
    .transform((v) => v.replace(/[\s()-]/g, ''))
    .refine((v) => /^(\+?52)?\d{10}$/.test(v), 'Escribe un teléfono de 10 dígitos'),
});
type Input = z.input<typeof schema>;
type Values = z.output<typeof schema>;

/**
 * Finalizar compra. Reglas (CLAUDE.md §6): SOLO recolección en el club (sin envío ni costo de envío)
 * y el pago se hace en Mercado Pago (la plataforma no captura datos de tarjeta).
 */
export default function CheckoutPage() {
  const { user } = useAuth();
  const { lines, count, subtotal, items, clear } = useCart();
  const navigate = useNavigate();
  const [phase, setPhase] = useState<'idle' | 'creating' | 'redirecting'>('idle');
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Input, unknown, Values>({
    resolver: zodResolver(schema),
    defaultValues: { nombre: user?.name ?? '', email: user?.email ?? '', telefono: user?.phone ?? '' },
  });

  const onSubmit = handleSubmit(async (contact) => {
    setError(null);
    setPhase('creating');
    try {
      const order = await createOrder(items, contact);
      // TODO(API): redirigir a `init_point` de la preferencia de Mercado Pago.
      setPhase('redirecting');
      const paid = await simulatePayment(order.id);
      clear();
      navigate(`/tienda/pedido/${paid.id}`, { replace: true });
    } catch (e) {
      setPhase('idle');
      setError(e instanceof OrderError ? e.message : 'No pudimos iniciar el pago. Intenta de nuevo.');
    }
  });

  if (lines.length === 0 && phase === 'idle') {
    return (
      <div className={`container ${styles.page}`}>
        <title>Finalizar compra · PCP Shooting</title>
        <div className={styles.card}>
          <EmptyState
            icon={IconCart}
            title="No hay productos para pagar"
            description="Tu carrito está vacío. Agrega productos de la tienda oficial."
            action={<ButtonLink to="/tienda">Ir a la tienda</ButtonLink>}
          />
        </div>
      </div>
    );
  }

  const busy = phase !== 'idle';
  const payLabel = `Pagar ${formatMXN(subtotal)}`;

  return (
    <>
      <title>Finalizar compra · PCP Shooting</title>
      <div className={`container ${styles.page}`}>
        <Link to="/tienda" className={styles.back}>
          <IconArrowLeft size={18} /> Volver a la tienda
        </Link>
        <div className={styles.titleRow}>
          <h1 className={styles.title}>Finalizar compra</h1>
          <span className={styles.secureTag}>
            <IconLock size={18} /> Pago seguro
          </span>
        </div>

        <div className={styles.layout}>
          <form id="checkout" className={styles.stack} onSubmit={onSubmit} noValidate>
            <section className={`${styles.card} ${styles.mobileOnly}`} aria-label="Resumen de tu pedido">
              <h2 className={styles.summaryTitle}>Tu pedido ({count})</h2>
              <OrderLinesSummary lines={lines} />
              <dl className={styles.totals}>
                <div>
                  <dt>Entrega</dt>
                  <dd>Gratis</dd>
                </div>
              </dl>
            </section>

            <section className={styles.card} aria-labelledby="datos-titulo">
              <h2 id="datos-titulo" className={styles.cardTitle}>
                1. Tus datos
              </h2>
              <div className={styles.formGrid}>
                <TextField label="Nombre completo" autoComplete="name" error={errors.nombre?.message} {...register('nombre')} />
                <TextField
                  label="Correo electrónico"
                  type="email"
                  autoComplete="email"
                  error={errors.email?.message}
                  {...register('email')}
                />
                <TextField
                  label="Teléfono / WhatsApp"
                  type="tel"
                  inputMode="tel"
                  autoComplete="tel"
                  placeholder="10 dígitos"
                  error={errors.telefono?.message}
                  {...register('telefono')}
                />
              </div>
              {!user && (
                <p className={styles.hint}>
                  ¿Ya tienes cuenta? <Link to="/login?next=/tienda/checkout">Inicia sesión</Link> para llenar tus datos y
                  ver tus pedidos.
                </p>
              )}
            </section>

            <section className={styles.card} aria-labelledby="entrega-titulo">
              <h2 id="entrega-titulo" className={styles.cardTitle}>
                2. Entrega
              </h2>
              <div className={styles.pickup}>
                <div className={styles.pickupHead}>
                  <span>Recoger en el club</span>
                  <span>Gratis</span>
                </div>
                <p>
                  {club.address} · {club.hours}
                </p>
              </div>
              <p className={styles.hint}>Te avisamos por WhatsApp cuando tu pedido esté listo. No hay envíos a domicilio.</p>
            </section>

            <section className={styles.card} aria-labelledby="pago-titulo">
              <h2 id="pago-titulo" className={styles.cardTitle}>
                3. Pago
              </h2>
              {error && <FormAlert>{error}</FormAlert>}
              <PaymentMethods intro="Pagas el 100% de tu pedido en Mercado Pago, con el método que prefieras:" />
              {phase === 'redirecting' && <PaymentRedirect />}
            </section>
          </form>

          <aside className={`${styles.summary} ${styles.desktopOnly}`} aria-label="Resumen de tu pedido">
            <h2 className={styles.summaryTitle}>Tu pedido ({count})</h2>
            <OrderLinesSummary lines={lines} />
            <dl className={styles.totals}>
              <div>
                <dt>Subtotal</dt>
                <dd>{formatMXN(subtotal)}</dd>
              </div>
              <div>
                <dt>Entrega</dt>
                <dd>Gratis</dd>
              </div>
              <div className={styles.grandTotal}>
                <dt>Total</dt>
                <dd>{formatMXN(subtotal)}</dd>
              </div>
            </dl>
            <Button type="submit" form="checkout" size="lg" fullWidth loading={busy}>
              {payLabel}
            </Button>
          </aside>
        </div>
      </div>

      <MobileActionBar label="Total" amount={subtotal}>
        <Button type="submit" form="checkout" size="lg" fullWidth loading={busy}>
          {payLabel}
        </Button>
      </MobileActionBar>
    </>
  );
}
