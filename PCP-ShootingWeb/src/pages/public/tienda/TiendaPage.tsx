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
import { ONLINE_PAYMENT, addToCart, cardData, getProducts, productPath } from '@/features/tienda/api';
import { getStoreCategories } from '@/features/tienda/categoriasApi';
import { CategoryTiles } from '@/features/tienda/components/StoreNav';
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
  ONLINE_PAYMENT
    ? { icon: IconLock, title: 'Pago seguro en línea', text: 'Tarjeta de crédito, débito y efectivo' }
    : { icon: IconLock, title: 'Aparta en línea', text: 'Pagas al recoger en el club' },
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
  // F24: con categorías la tienda entra por botones (categoría → marca → productos); sin ellas, lista única como antes.
  const { data: categories } = useAsync(getStoreCategories, [attempt]);
  const byCategory = Boolean(categories && categories.length > 0);
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
      placeholder="Rifle, munición, playera…"
      defaultValue={query}
      onChange={(e) => update('q', e.target.value)}
    />
  );

  return (
    <>
      <title>Tienda oficial · PCP Shooting</title>
      <meta
        name="description"
        content="Rifles, municiones, accesorios y ropa oficial de PCP Shooting. Compra en línea y recoge en el club."
      />

      <PageHero
        eyebrow="Tienda oficial"
        title="Todo para tu tiro"
        lead="Rifles, municiones, accesorios y ropa oficial PCP. Compra en línea y recoge en el club."
        hideAsideOnMobile
        hideChildrenOnMobile
        aside={
          <div className={styles.heroPhoto}>
            <Photo alt="Foto: playeras PCP negra, roja y blanca" className={styles.darkPlaceholder} />
          </div>
        }
      >
        <div className={styles.heroActions}>
          <ButtonLink to={byCategory ? '/tienda#categorias' : '/tienda#productos'} size="lg">
            {byCategory ? 'Ver categorías' : 'Ver productos'}
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
        {byCategory && (
          <section id="categorias" className={styles.products} aria-labelledby="categorias-titulo">
            <div className={styles.count}>
              <h2 id="categorias-titulo">Compra por categoría</h2>
              <p>Elige una categoría y después la marca.</p>
            </div>
            <CategoryTiles categories={categories!} />
          </section>
        )}

        <section id="productos" className={styles.products} aria-labelledby="productos-titulo">
          <div className={styles.toolbar}>
            <div className={styles.count}>
              <h2 id="productos-titulo">{byCategory ? 'Todos los productos' : 'Productos'}</h2>
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

          {!byCategory && (
            <ChipGroup label="Categorías">
              {/* Solo las categorías que tienen productos (la API puede no tener todas). */}
              {CATEGORIES.filter((c) => c.id === 'todo' || c.id === category || (products ?? []).some((p) => p.category === c.id)).map((c) => (
                <Chip key={c.id} selected={category === c.id} onClick={() => update('categoria', c.id === 'todo' ? null : c.id)}>
                  {c.label}
                </Chip>
              ))}
            </ChipGroup>
          )}

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
                  product={cardData(p)}
                  href={productPath(p)}
                  added={cart.has(p.id)}
                  onAdd={(size) => addToCart(cart, p, size)}
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
              <span>Martes a domingo · 8:00 a 18:00</span>
            </div>
          }
        />
      </div>
    </>
  );
}
