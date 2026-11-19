import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import {
  Button,
  ButtonLink,
  Chip,
  ChipGroup,
  CtaBanner,
  EmptyState,
  PageHero,
  Photo,
  ProductCard,
  ProductGrid,
  SelectField,
  TextField,
} from '@/components/ui';
import { IconArrowRight, IconBadge, IconLock, IconPin, IconSearch } from '@/components/icons/icons';
import { getProducts } from '@/features/tienda/api';
import { useCart } from '@/features/tienda/CartProvider';
import type { ProductCategory } from '@/features/tienda/types';
import { useAsync } from '@/features/reservas/useAsync';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './TiendaPage.module.css';

const CATEGORIES: { id: ProductCategory | 'todo'; label: string }[] = [
  { id: 'todo', label: 'Todo' },
  { id: 'playeras', label: 'Playeras' },
  { id: 'gorras', label: 'Gorras' },
  { id: 'accesorios', label: 'Tazas y stickers' },
];

const SORTS = [
  { value: 'destacados', label: 'Destacados' },
  { value: 'precio-asc', label: 'Precio: menor a mayor' },
  { value: 'precio-desc', label: 'Precio: mayor a menor' },
  { value: 'nombre', label: 'Nombre' },
];

const PERKS = [
  { icon: IconLock, title: 'Pago seguro en línea', text: 'Tarjeta de crédito, débito y efectivo' },
  { icon: IconPin, title: 'Recoge en el club', text: 'Km 23 Ruta de los Cenotes' },
  { icon: IconBadge, title: 'Diseño oficial', text: 'Colores y logo de la marca' },
];

const normalize = (s: string) =>
  s
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase();

export default function TiendaPage() {
  const [attempt, setAttempt] = useState(0);
  const { data: products, loading, error } = useAsync(getProducts, [attempt]);
  const cart = useCart();
  const [params, setParams] = useSearchParams();
  const category = (params.get('categoria') as ProductCategory | null) ?? 'todo';
  const query = params.get('q') ?? '';
  const sort = params.get('orden') ?? 'destacados';

  const update = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (!value) next.delete(key);
    else next.set(key, value);
    setParams(next, { replace: true, preventScrollReset: true });
  };

  const visible = useMemo(() => {
    const q = normalize(query.trim());
    const list = (products ?? []).filter(
      (p) =>
        (category === 'todo' || p.category === category) &&
        (!q || normalize(`${p.name} ${p.description} ${p.categoryLabel}`).includes(q)),
    );
    return [...list].sort((a, b) =>
      sort === 'precio-asc'
        ? a.price - b.price
        : sort === 'precio-desc'
          ? b.price - a.price
          : sort === 'nombre'
            ? a.name.localeCompare(b.name, 'es')
            : a.featured - b.featured,
    );
  }, [products, category, query, sort]);

  const search = (
    <TextField
      label="Buscar"
      type="search"
      placeholder="Playera, gorra, taza…"
      defaultValue={query}
      onChange={(e) => update('q', e.target.value)}
    />
  );

  return (
    <>
      <title>Tienda oficial · PCP Shooting</title>
      <meta
        name="description"
        content="Playeras, gorras, tazas y stickers oficiales de PCP Shooting. Compra en línea y recoge en el club."
      />

      <PageHero
        eyebrow="Tienda oficial · Ropa y accesorios"
        title="Viste la marca PCP"
        lead="Playeras, gorras, tazas y stickers oficiales en negro, rojo y blanco. Compra en línea y recoge en el club."
        hideAsideOnMobile
        hideChildrenOnMobile
        aside={
          <div className={styles.heroPhoto}>
            <Photo alt="Foto: playeras PCP negra, roja y blanca" className={styles.darkPlaceholder} />
          </div>
        }
      >
        <div className={styles.heroActions}>
          <ButtonLink to="/tienda?categoria=playeras#productos" size="lg">
            Ver playeras
          </ButtonLink>
          <ButtonLink to="/reservar" size="lg" variant="ghost">
            Reservar línea de tiro
          </ButtonLink>
        </div>
      </PageHero>

      <ul className={styles.perks} aria-label="Ventajas">
        {PERKS.map(({ icon: Icon, title, text }) => (
          <li key={title} className={styles.perk}>
            <span className={styles.perkIcon} aria-hidden="true">
              <Icon size={20} />
            </span>
            <span>
              <strong>{title}</strong>
              <span>{text}</span>
            </span>
          </li>
        ))}
      </ul>

      <div className={`container ${styles.main}`}>
        <section id="productos" className={styles.products} aria-labelledby="productos-titulo">
          <div className={styles.toolbar}>
            <div className={styles.count}>
              <h2 id="productos-titulo">Productos</h2>
              <p>
                {visible.length} producto{visible.length === 1 ? '' : 's'}
              </p>
            </div>
            <div className={styles.filters}>
              <div className={styles.desktopSearch}>{search}</div>
              <SelectField
                label="Ordenar por"
                value={sort}
                options={SORTS}
                onChange={(e) => update('orden', e.target.value === 'destacados' ? null : e.target.value)}
                className={styles.sort}
              />
            </div>
          </div>

          <ChipGroup label="Categorías">
            {/* Solo las categorías que tienen productos (la API puede no tener todas). */}
            {CATEGORIES.filter((c) => c.id === 'todo' || c.id === category || (products ?? []).some((p) => p.category === c.id)).map((c) => (
              <Chip key={c.id} selected={category === c.id} onClick={() => update('categoria', c.id === 'todo' ? null : c.id)}>
                {c.label}
              </Chip>
            ))}
          </ChipGroup>

          <div className={styles.mobileSearch}>{search}</div>

          {loading && !products ? (
            <LoadingScreen />
          ) : error ? (
            <EmptyState
              title="No pudimos cargar la tienda"
              description={error.message}
              action={<Button onClick={() => setAttempt((n) => n + 1)}>Reintentar</Button>}
            />
          ) : visible.length === 0 ? (
            <EmptyState icon={IconSearch} title="Sin resultados" description="No encontramos productos con esa búsqueda." />
          ) : (
            <ProductGrid>
              {visible.map((p) => (
                <ProductCard
                  key={p.id}
                  product={{ name: p.name, category: p.categoryLabel, description: p.description, price: p.price, imageUrl: p.imageUrl }}
                  added={cart.has(p.id)}
                  onAdd={() => cart.add(p)}
                />
              ))}
            </ProductGrid>
          )}

          {cart.count > 0 && (
            <ButtonLink to="/tienda/carrito" variant="dark" size="lg" fullWidth className={styles.mobileCart}>
              Ver carrito ({cart.count})
            </ButtonLink>
          )}
        </section>

        <CtaBanner
          eyebrow="¿Es tu primera vez?"
          title="Renta un rifle y vive la experiencia"
          text="15 puestos de tiro y tres disciplinas: Bench Rest, Plinking e IPSC. Reserva tu horario y paga en línea."
          textMobile="15 líneas de tiro y tres disciplinas. Reserva y paga en línea."
          extra={
            <ul className={styles.pills}>
              <li>Rifle PCP incluido</li>
              <li>Diábolos incluidos</li>
              <li>Área de tiro</li>
            </ul>
          }
          actions={
            <div className={styles.ctaAction}>
              <ButtonLink to="/reservar?actividad=recreativo" size="lg" iconEnd={<IconArrowRight size={18} />}>
                Quiero vivir la experiencia
              </ButtonLink>
              <span>Lunes a sábado · 9:00 a 17:00</span>
            </div>
          }
        />
      </div>
    </>
  );
}
