import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Chip,
  ChipGroup,
  ChoiceCard,
  EmptyState,
  FormAlert,
  MobileActionBar,
  PageHero,
  QuantityStepper,
  SelectField,
  TextField,
} from '@/components/ui';
import { IconCup, IconPlus } from '@/components/icons/icons';
import { useAuth } from '@/features/auth/AuthProvider';
import {
  FOOD_CATEGORIES,
  FOOD_PAYMENT_MODE,
  createFoodOrder,
  getMenu,
  type FoodCategory,
  type FoodItem,
  type FoodOrder,
} from '@/features/alimentos/api';
import { StatusTracker } from '@/features/alimentos/StatusTracker';
import { TOTAL_LINES } from '@/features/reservas/config';
import { useAsync } from '@/features/reservas/useAsync';
import { formatMXN } from '@/lib/format';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './AlimentosPage.module.css';

const KEY = 'pcp.alimentos.pedido';

function loadQty(): Record<string, number> {
  try {
    return JSON.parse(sessionStorage.getItem(KEY) ?? '{}') as Record<string, number>;
  } catch {
    return {};
  }
}

/**
 * Carta digital de alimentos y bebidas (sin diseño propio: sigue el sistema de la tienda).
 * Pedido desde el sitio para consumir en tu línea o recoger en barra (CLAUDE.md §6).
 */
export default function AlimentosPage() {
  const { user } = useAuth();
  const { data: menu, loading } = useAsync(getMenu, []);
  const [category, setCategory] = useState<FoodCategory | 'todo'>('todo');
  const [qty, setQty] = useState<Record<string, number>>(loadQty);
  const [delivery, setDelivery] = useState<'linea' | 'barra'>('linea');
  const [line, setLine] = useState('');
  const [nombre, setNombre] = useState(user?.name ?? '');
  const [telefono, setTelefono] = useState(user?.phone ?? '');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [sending, setSending] = useState(false);
  const [order, setOrder] = useState<FoodOrder | null>(null);

  useEffect(() => {
    try {
      sessionStorage.setItem(KEY, JSON.stringify(qty));
    } catch {
      /* sin almacenamiento */
    }
  }, [qty]);

  const items = useMemo(
    () =>
      (menu ?? []).flatMap((item) => (qty[item.id] ? [{ item, quantity: qty[item.id]! }] : [])),
    [menu, qty],
  );
  const count = items.reduce((s, i) => s + i.quantity, 0);
  const total = items.reduce((s, i) => s + i.item.price * i.quantity, 0);
  const visible = (menu ?? []).filter((m) => category === 'todo' || m.category === category);

  const setItemQty = (id: string, n: number) =>
    setQty((prev) => {
      const next = { ...prev };
      if (n <= 0) delete next[id];
      else next[id] = n;
      return next;
    });

  const submit = async () => {
    const e: Record<string, string> = {};
    if (count === 0) e.items = 'Agrega al menos un producto.';
    if (delivery === 'linea' && !line) e.line = 'Elige tu línea de tiro.';
    if (nombre.trim().length < 2) e.nombre = 'Escribe tu nombre.';
    if (!/^(\+?52)?\d{10}$/.test(telefono.replace(/[\s()-]/g, ''))) e.telefono = 'Escribe un teléfono de 10 dígitos.';
    setErrors(e);
    if (Object.keys(e).length) return;
    setSending(true);
    try {
      const o = await createFoodOrder(
        items,
        delivery === 'linea' ? { type: 'linea', line: Number(line) } : { type: 'barra' },
        { nombre, telefono },
      );
      setOrder(o);
      setQty({});
      document.getElementById('tu-pedido')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch {
      setErrors({ form: 'No pudimos enviar tu pedido. Intenta de nuevo.' });
    } finally {
      setSending(false);
    }
  };

  const submitLabel =
    FOOD_PAYMENT_MODE === 'en_linea' ? `Pagar ${formatMXN(total)}` : `Enviar pedido · ${formatMXN(total)}`;

  return (
    <>
      <title>Alimentos y bebidas · PCP Shooting</title>
      <meta name="description" content="Pide bebidas, snacks y comida durante tu visita a PCP Shooting: te lo llevamos a tu línea o recoges en barra." />

      <PageHero
        eyebrow="Alimentos y bebidas"
        title="Pide sin dejar tu línea"
        lead="Bebidas, snacks y comida para tu visita. Pide desde aquí: te lo llevamos a tu línea de tiro o lo recoges en barra."
      />

      <div className={`container ${styles.layout}`}>
        <section className={styles.menu} aria-labelledby="carta">
          <div className={styles.menuHead}>
            <h2 id="carta" className={styles.menuTitle}>
              Carta
            </h2>
            <ChipGroup label="Categorías de la carta">
              {FOOD_CATEGORIES.map((c) => (
                <Chip key={c.id} selected={category === c.id} onClick={() => setCategory(c.id)}>
                  {c.label}
                </Chip>
              ))}
            </ChipGroup>
          </div>

          {loading && !menu ? (
            <LoadingScreen />
          ) : (
            <ul className={styles.items}>
              {visible.map((item: FoodItem) => {
                const n = qty[item.id] ?? 0;
                return (
                  <li key={item.id} className={styles.item}>
                    <div className={styles.itemText}>
                      <h3 className={styles.itemName}>{item.name}</h3>
                      <p className={styles.itemDesc}>{item.description}</p>
                      <strong className={styles.itemPrice}>{formatMXN(item.price)}</strong>
                    </div>
                    {!item.available ? (
                      <span className={styles.soldOut}>Agotado</span>
                    ) : n > 0 ? (
                      <QuantityStepper label={item.name} value={n} min={0} onChange={(v) => setItemQty(item.id, v)} />
                    ) : (
                      <Button
                        variant="secondary"
                        iconStart={<IconPlus size={16} />}
                        onClick={() => setItemQty(item.id, 1)}
                        aria-label={`Agregar ${item.name}`}
                      >
                        Agregar
                      </Button>
                    )}
                  </li>
                );
              })}
            </ul>
          )}
        </section>

        <aside id="tu-pedido" className={styles.panel} aria-labelledby="pedido-titulo">
          <h2 id="pedido-titulo" className={styles.panelTitle}>
            {order ? `Pedido ${order.folio}` : `Tu pedido${count ? ` (${count})` : ''}`}
          </h2>

          {order ? (
            <div className={styles.confirm} role="status">
              <StatusTracker status={order.status} />
              <p>
                ¡Recibimos tu pedido! Te avisaremos por WhatsApp cuando esté listo.{' '}
                {order.delivery.type === 'linea'
                  ? `Lo llevamos a tu línea ${order.delivery.line}.`
                  : 'Recógelo en la barra con tu folio.'}
              </p>
              <dl className={styles.totals}>
                {order.items.map((i) => (
                  <div key={i.id}>
                    <dt>
                      {i.name} × {i.quantity}
                    </dt>
                    <dd>{formatMXN(i.price * i.quantity)}</dd>
                  </div>
                ))}
                <div className={styles.grand}>
                  <dt>{order.paid ? 'Pagado' : 'A pagar en el club'}</dt>
                  <dd>{formatMXN(order.total)}</dd>
                </div>
              </dl>
              <Button variant="secondary" size="lg" fullWidth onClick={() => setOrder(null)}>
                Hacer otro pedido
              </Button>
            </div>
          ) : count === 0 ? (
            <EmptyState
              icon={IconCup}
              title="Tu pedido está vacío"
              description="Agrega bebidas, snacks o comida de la carta."
              className={styles.empty}
            />
          ) : (
            <div className={styles.form}>
              {errors.form && <FormAlert>{errors.form}</FormAlert>}
              <ul className={styles.lines}>
                {items.map(({ item, quantity }) => (
                  <li key={item.id}>
                    <span className={styles.lineName}>{item.name}</span>
                    <QuantityStepper label={item.name} value={quantity} min={0} onChange={(v) => setItemQty(item.id, v)} />
                    <strong>{formatMXN(item.price * quantity)}</strong>
                  </li>
                ))}
              </ul>

              <div role="radiogroup" aria-label="Entrega" className={styles.delivery}>
                <ChoiceCard
                  compact
                  selected={delivery === 'linea'}
                  onSelect={() => setDelivery('linea')}
                  title="A mi línea de tiro"
                  description="Te lo llevamos mientras disparas"
                />
                <ChoiceCard
                  compact
                  selected={delivery === 'barra'}
                  onSelect={() => setDelivery('barra')}
                  title="Recoger en barra"
                  description="Te avisamos cuando esté listo"
                />
              </div>

              {delivery === 'linea' && (
                <SelectField
                  label="Línea de tiro"
                  value={line}
                  placeholder="Elige tu línea"
                  onChange={(e) => setLine(e.target.value)}
                  options={Array.from({ length: TOTAL_LINES }, (_, i) => ({ value: String(i + 1), label: `Línea ${i + 1}` }))}
                  error={errors.line}
                />
              )}
              <TextField label="Nombre" autoComplete="name" value={nombre} onChange={(e) => setNombre(e.target.value)} error={errors.nombre} />
              <TextField
                label="Teléfono / WhatsApp"
                type="tel"
                inputMode="tel"
                autoComplete="tel"
                placeholder="10 dígitos"
                value={telefono}
                onChange={(e) => setTelefono(e.target.value)}
                error={errors.telefono}
              />

              <dl className={styles.totals}>
                <div className={styles.grand}>
                  <dt>Total</dt>
                  <dd>{formatMXN(total)}</dd>
                </div>
              </dl>
              <p className={styles.payNote}>
                {FOOD_PAYMENT_MODE === 'en_linea'
                  ? 'Pagas en Mercado Pago antes de que preparemos tu pedido.'
                  : 'Pagas al recibir tu pedido en el club (efectivo o tarjeta).'}
              </p>
              <Button size="lg" fullWidth loading={sending} onClick={submit} className={styles.desktopSubmit}>
                {submitLabel}
              </Button>
              <p className={styles.payNote}>Servicio durante tu visita · Lunes a sábado, 9:00 a 17:00.</p>
            </div>
          )}
        </aside>
      </div>

      {!order && count > 0 && (
        <MobileActionBar label={`Tu pedido · ${count} producto${count === 1 ? '' : 's'}`} amount={total}>
          <Button
            size="lg"
            fullWidth
            onClick={() => document.getElementById('tu-pedido')?.scrollIntoView({ behavior: 'smooth', block: 'start' })}
          >
            Ver mi pedido
          </Button>
        </MobileActionBar>
      )}
    </>
  );
}
