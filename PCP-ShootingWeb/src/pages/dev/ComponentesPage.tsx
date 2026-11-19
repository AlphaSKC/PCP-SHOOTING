import { useState, type ReactNode } from 'react';
import { Link } from 'react-router';
import {
  Button,
  ButtonLink,
  Card,
  CardHeader,
  CartDrawer,
  Checkbox,
  Chip,
  ChipGroup,
  DataTable,
  EmptyState,
  KpiCard,
  KpiGrid,
  LineMap,
  PageHero,
  ProductCard,
  ProductGrid,
  QuantityStepper,
  SelectField,
  StatusBadge,
  StepBar,
  Switch,
  TextareaField,
  TextField,
  type CartLine,
  type Column,
  type ProductCardData,
  type ShootingLine,
  type StatusKey,
} from '@/components/ui';
import { IconArrowRight, IconPlus, IconSearch } from '@/components/icons/icons';
import styles from './ComponentesPage.module.css';

/*
 * Catálogo de componentes (solo en desarrollo: /dev/componentes).
 * Todos los datos son DE EJEMPLO; no son precios ni nombres reales.
 */

const PRODUCTS: (ProductCardData & { id: string; sizes?: string[] })[] = [
  { id: 'p1', name: 'Playera PCP Negra', category: 'Playera', description: 'Logo dorado · Tallas CH a XG', price: 450, sizes: ['CH', 'M', 'G', 'XG'] },
  { id: 'p2', name: 'Playera PCP Roja', category: 'Playera', description: 'Logo blanco · Tallas CH a XG', price: 450, sizes: ['CH', 'M', 'G', 'XG'] },
  { id: 'p3', name: 'Gorra PCP Negra', category: 'Gorra', description: 'Logo dorado · Ajustable', price: 380 },
  { id: 'p4', name: 'Taza PCP', category: 'Taza', description: 'Disponible en negro, rojo y blanco', price: 220 },
];

const OCCUPIED = [3, 7, 8, 12];
const LINES: ShootingLine[] = Array.from({ length: 15 }, (_, i) => ({
  number: i + 1,
  status: OCCUPIED.includes(i + 1) ? 'occupied' : 'available',
}));

type Agenda = { id: string; hora: string; cliente: string; detalle: string; linea: string; estado: StatusKey };
const AGENDA: Agenda[] = [
  { id: 'a1', hora: '9:00', cliente: 'Cliente de ejemplo 1', detalle: 'Recreativo · 4 pers.', linea: 'Líneas 1–2', estado: 'llego' },
  { id: 'a2', hora: '10:00', cliente: 'Cliente de ejemplo 2', detalle: 'Bench Rest · 1 pers.', linea: 'Línea 5', estado: 'llego' },
  { id: 'a3', hora: '11:00', cliente: 'Cliente de ejemplo 3', detalle: 'Plinking · 8 pers.', linea: 'Líneas 9–12', estado: 'por_llegar' },
  { id: 'a4', hora: '12:00', cliente: 'Cliente de ejemplo 4', detalle: 'IPSC · 2 pers.', linea: 'Línea 14', estado: 'pendiente_pago' },
];

const AGENDA_COLUMNS: Column<Agenda>[] = [
  { key: 'hora', header: 'Hora', strong: true, render: (r) => r.hora },
  {
    key: 'cliente',
    header: 'Cliente',
    primary: true,
    render: (r) => (
      <span className={styles.stack}>
        <strong>{r.cliente}</strong>
        <span className={styles.muted}>{r.detalle}</span>
      </span>
    ),
  },
  { key: 'linea', header: 'Línea', render: (r) => r.linea },
  { key: 'estado', header: 'Estado', render: (r) => <StatusBadge status={r.estado} /> },
];

const STEPS = ['Actividad', 'Fecha y línea', 'Tus datos', 'Pago'];
const CATEGORIES = ['Todo', 'Playeras', 'Gorras', 'Tazas y stickers'];

function Section({ id, title, children }: { id: string; title: string; children: ReactNode }) {
  return (
    <section id={id} className={styles.section} aria-labelledby={`${id}-t`}>
      <h2 id={`${id}-t`} className={styles.sectionTitle}>
        {title}
      </h2>
      {children}
    </section>
  );
}

export default function ComponentesPage() {
  const [category, setCategory] = useState('Todo');
  const [step, setStep] = useState(1);
  const [line, setLine] = useState<number | null>(5);
  const [people, setPeople] = useState(2);
  const [accept, setAccept] = useState(false);
  const [published, setPublished] = useState(true);
  const [cart, setCart] = useState<CartLine[]>([
    { id: 'p1', name: 'Playera PCP Negra', price: 450, quantity: 1, size: 'M', sizes: ['CH', 'M', 'G', 'XG'] },
  ]);
  const [cartOpen, setCartOpen] = useState(false);

  const addToCart = (p: (typeof PRODUCTS)[number]) =>
    setCart((prev) => {
      const found = prev.find((l) => l.id === p.id);
      if (found) return prev.map((l) => (l.id === p.id ? { ...l, quantity: l.quantity + 1 } : l));
      return [...prev, { id: p.id, name: p.name, price: p.price, quantity: 1, sizes: p.sizes, size: p.sizes?.[1] }];
    });

  return (
    <>
      <title>Componentes · PCP Shooting</title>
      <PageHero
        eyebrow="Solo desarrollo · Datos de ejemplo"
        title="Componentes base"
        lead="Catálogo del sistema de diseño. Cambia el ancho de la ventana para revisar escritorio, tableta y celular."
      >
        <Button size="lg" iconEnd={<IconArrowRight size={18} />}>
          Primario
        </Button>
        <Button size="lg" variant="ghost">
          Contorno sobre negro
        </Button>
      </PageHero>

      <div className={`container ${styles.page}`}>
        <nav aria-label="Secciones del catálogo" className={styles.toc}>
          {[
            ['botones', 'Botones'],
            ['chips', 'Chips y estados'],
            ['formularios', 'Formularios'],
            ['reserva', 'Pasos y líneas'],
            ['tienda', 'Productos y carrito'],
            ['panel', 'Panel'],
          ].map(([id, label]) => (
            <a key={id} href={`#${id}`}>
              {label}
            </a>
          ))}
        </nav>

        <Section id="botones" title="Botones">
          <Card className={styles.stackCard}>
            <div className={styles.row}>
              <Button>Primario</Button>
              <Button variant="secondary">Secundario</Button>
              <Button variant="dark">Oscuro</Button>
              <Button variant="gold">Elegir Socio Oro</Button>
              <Button variant="text">Eliminar</Button>
              <Button iconStart={<IconPlus size={16} />}>Nueva reserva</Button>
              <Button loading>Pagando</Button>
              <Button disabled>Deshabilitado</Button>
            </div>
            <div className={styles.row}>
              <Button size="lg">Continuar: datos</Button>
              <Button size="lg" variant="secondary">
                Atrás
              </Button>
              <ButtonLink to="/reservar" size="lg" variant="dark">
                Enlace con aspecto de botón
              </ButtonLink>
            </div>
          </Card>
          <div className={styles.darkPanel}>
            <div className={styles.row}>
              <Button size="lg">Ver playeras</Button>
              <Button size="lg" variant="ghost">
                Reservar línea de tiro
              </Button>
            </div>
          </div>
        </Section>

        <Section id="chips" title="Chips y estados">
          <Card className={styles.stackCard}>
            <ChipGroup label="Categorías">
              {CATEGORIES.map((c) => (
                <Chip key={c} selected={category === c} onClick={() => setCategory(c)}>
                  {c}
                </Chip>
              ))}
            </ChipGroup>
            <div className={styles.row}>
              {(['pagado', 'preparando', 'listo_para_recoger', 'entregado', 'pendiente_pago', 'confirmada', 'llego', 'vencida', 'activo', 'borrador'] as StatusKey[]).map(
                (s) => (
                  <StatusBadge key={s} status={s} />
                ),
              )}
              <StatusBadge tone="gold">Socio Oro</StatusBadge>
            </div>
          </Card>
        </Section>

        <Section id="formularios" title="Formularios">
          <Card>
            <form className={styles.form} onSubmit={(e) => e.preventDefault()}>
              <TextField label="Nombre completo" placeholder="Tu nombre" autoComplete="name" required />
              <TextField label="Teléfono / WhatsApp" type="tel" placeholder="10 dígitos" hint="Te avisamos por WhatsApp." />
              <TextField
                label="Correo electrónico"
                type="email"
                defaultValue="correo-invalido"
                error="Escribe un correo válido, por ejemplo tucorreo@ejemplo.com"
              />
              <SelectField
                label="Motivo"
                defaultValue=""
                placeholder="Elige una opción"
                options={[
                  { value: 'reserva', label: 'Reservas' },
                  { value: 'eventos', label: 'Eventos' },
                  { value: 'membresia', label: 'Membresía CARTAQ' },
                ]}
              />
              <TextField label="Buscar" hideLabel type="search" placeholder="Playera, gorra, taza…" />
              <div className={styles.inline}>
                <span className={styles.fieldLabel}>Personas</span>
                <QuantityStepper label="personas" value={people} onChange={setPeople} max={8} />
              </div>
              <TextareaField className={styles.full} label="Mensaje" placeholder="Cuéntanos qué necesitas" />
              <Checkbox
                className={styles.full}
                checked={accept}
                onChange={(e) => setAccept(e.target.checked)}
                label="Acepto el reglamento del campo de tiro y el uso del equipo de seguridad."
              />
              <div className={styles.full}>
                <Switch label="Publicado en el sitio" showLabel checked={published} onChange={setPublished} />
              </div>
            </form>
          </Card>
        </Section>

        <Section id="reserva" title="Pasos y mapa de líneas">
          <div className={styles.darkPanel}>
            <StepBar steps={STEPS} current={step} />
            <div className={styles.row}>
              <Button variant="ghost" onClick={() => setStep((s) => Math.max(0, s - 1))}>
                Paso anterior
              </Button>
              <Button onClick={() => setStep((s) => Math.min(STEPS.length - 1, s + 1))}>Paso siguiente</Button>
            </div>
          </div>
          <Card>
            <LineMap lines={LINES} selected={line} onSelect={setLine} />
            <p className={styles.muted}>Seleccionada: {line ? `Línea ${line}` : 'ninguna'}</p>
          </Card>
        </Section>

        <Section id="tienda" title="Productos y carrito">
          <ProductGrid>
            {PRODUCTS.map((p) => (
              <ProductCard key={p.id} product={p} added={cart.some((l) => l.id === p.id)} onAdd={() => addToCart(p)} />
            ))}
          </ProductGrid>
          <div className={styles.row}>
            <Button variant="dark" size="lg" onClick={() => setCartOpen(true)}>
              Ver carrito ({cart.reduce((n, l) => n + l.quantity, 0)})
            </Button>
            <Button variant="secondary" size="lg" onClick={() => setCart([])}>
              Vaciar carrito
            </Button>
          </div>
          <CartDrawer
            open={cartOpen}
            onClose={() => setCartOpen(false)}
            lines={cart}
            onQuantityChange={(id, q) => setCart((prev) => prev.map((l) => (l.id === id ? { ...l, quantity: q } : l)))}
            onSizeChange={(id, size) => setCart((prev) => prev.map((l) => (l.id === id ? { ...l, size } : l)))}
            onRemove={(id) => setCart((prev) => prev.filter((l) => l.id !== id))}
            onCheckout={() => setCartOpen(false)}
          />
        </Section>

        <Section id="panel" title="Panel: indicadores, tabla y estado vacío">
          <KpiGrid>
            <KpiCard label="Reservas hoy" value="18" hint="42 personas · 6 por llegar" highlight />
            <KpiCard label="Ventas del día" value="$12,450" hint="Reservas y tienda" />
            <KpiCard label="Pedidos por preparar" value="4" hint="2 listos para recoger" />
            <KpiCard label="Ocupación de la semana" value="62%" hint="Sábado al 91%" />
          </KpiGrid>
          <Card>
            <CardHeader title="Agenda de hoy" action={<Link to="/admin/reservas">Ver calendario</Link>} />
            <DataTable caption="Agenda de hoy" columns={AGENDA_COLUMNS} rows={AGENDA} rowKey={(r) => r.id} />
          </Card>
          <Card>
            <EmptyState
              icon={IconSearch}
              title="Sin resultados"
              description="No encontramos productos con esa búsqueda. Prueba con otra palabra."
              action={<Button variant="secondary">Limpiar búsqueda</Button>}
            />
          </Card>
        </Section>
      </div>
    </>
  );
}
