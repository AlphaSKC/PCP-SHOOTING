import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useClub } from '@/features/contenido/ContenidoProvider';
import { Button, ButtonLink, EmptyState, FormAlert, MobileActionBar, TextField } from '@/components/ui';
import { IconArrowLeft, IconCart, IconLock } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import { PaymentMethods, PaymentRedirect } from '@/features/pagos/PaymentMethods';
import { AuthError } from '@/features/auth/types';
import { ONLINE_PAYMENT, OrderError, createOrder, getProducts, simulatePayment } from '@/features/tienda/api';
import { ApiError } from '@/lib/api';
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
 * Con la API: requiere sesión (el pedido pertenece al cliente) y, mientras no exista Mercado Pago en la API,
 * el pedido queda "Pendiente de pago" y el club registra el pago desde el panel.
 */
export default function CheckoutPage() {
  const club = useClub();
  const { user, status, updateProfile } = useAuth();
  const { lines, count, subtotal, items, clear } = useCart();
  const navigate = useNavigate();
  const [phase, setPhase] = useState<'idle' | 'creating' | 'redirecting'>('idle');
  const [error, setError] = useState<string | null>(null);
  // Existencias al día al entrar al checkout (el carrito puede venir de hace horas).
  useEffect(() => {
    if (!ONLINE_PAYMENT) void getProducts().catch(() => undefined);
  }, []);
  const shortLines = lines.filter((l) => l.short);

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
    if (shortLines.length) {
      setError('Algunos productos ya no tienen suficientes piezas. Ajusta tu carrito antes de continuar.');
      return;
    }
    setPhase('creating');
    try {
      if (!ONLINE_PAYMENT) {
        // El club avisa por WhatsApp: el nombre y teléfono del pedido son los del perfil.
        if (user && (contact.nombre !== user.name || contact.telefono !== user.phone.replace(/[\s()-]/g, ''))) {
          await updateProfile({ nombre: contact.nombre, email: user.email, telefono: contact.telefono });
        }
        const order = await createOrder(items, contact);
        clear();
        navigate(`/tienda/pedido/${order.id}`, { replace: true });
        return;
      }
      const order = await createOrder(items, contact);
      // TODO(API R05): redirigir a `init_point` de la preferencia de Mercado Pago.
      setPhase('redirecting');
      const paid = await simulatePayment(order.id);
      clear();
      navigate(`/tienda/pedido/${paid.id}`, { replace: true });
    } catch (e) {
      setPhase('idle');
      // 409: otra persona compró las últimas piezas. Se recargan existencias para mostrarlas en el carrito.
      if (e instanceof ApiError && e.status === 409) {
        void getProducts().catch(() => undefined);
        setError(`${e.message} Actualizamos las existencias: revisa tu carrito.`);
        return;
      }
      setError(
        e instanceof OrderError || e instanceof ApiError || e instanceof AuthError
          ? e.message
          : 'No pudimos registrar tu pedido. Intenta de nuevo.',
      );
    }
  });

  if (!ONLINE_PAYMENT && status !== 'loading' && !user && lines.length > 0) {
    return (
      <div className={`container ${styles.page}`}>
        <title>Finalizar compra · PCP Shooting</title>
        <div className={styles.card}>
          <EmptyState
            icon={IconLock}
            title="Inicia sesión para comprar"
            description="Tus pedidos quedan guardados en tu cuenta para que puedas consultarlos y recogerlos en el club."
            action={
              <>
                <ButtonLink to="/login?next=/tienda/checkout">Iniciar sesión</ButtonLink>
                <ButtonLink to="/registro" variant="secondary">
                  Crear cuenta
                </ButtonLink>
              </>
            }
          />
        </div>
      </div>
    );
  }

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
  const payLabel = ONLINE_PAYMENT ? `Pagar ${formatMXN(subtotal)}` : `Confirmar pedido · ${formatMXN(subtotal)}`;

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
                  readOnly={!ONLINE_PAYMENT}
                  hint={!ONLINE_PAYMENT ? 'El de tu cuenta. Puedes cambiarlo en tu perfil.' : undefined}
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
              {!ONLINE_PAYMENT && <p className={styles.hint}>Si cambias tu nombre o teléfono, también se actualizan en tu perfil.</p>}
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
              {ONLINE_PAYMENT ? (
                <>
                  <PaymentMethods intro="Pagas el 100% de tu pedido en Mercado Pago, con el método que prefieras:" />
                  {phase === 'redirecting' && <PaymentRedirect />}
                </>
              ) : (
                <div className={styles.pickup}>
                  <div className={styles.pickupHead}>
                    <span>Pago pendiente</span>
                    <span>{formatMXN(subtotal)}</span>
                  </div>
                  <p>
                    El pago en línea con Mercado Pago estará disponible pronto. Al confirmar, tu pedido queda apartado como
                    «Pendiente de pago» y el club te contacta por WhatsApp para completar el pago antes de recogerlo.
                  </p>
                </div>
              )}
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
