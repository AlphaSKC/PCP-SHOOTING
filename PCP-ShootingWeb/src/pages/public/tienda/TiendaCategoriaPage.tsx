import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button, ButtonLink, EmptyState, ProductCard, ProductGrid, SelectField, TextField } from '@/components/ui';
import { IconArrowLeft, IconSearch } from '@/components/icons/icons';
import { useAsync } from '@/features/reservas/useAsync';
import { addToCart, cardData, getProducts, productPath } from '@/features/tienda/api';
import { useCart } from '@/features/tienda/CartProvider';
import { getStoreCategories } from '@/features/tienda/categoriasApi';
import { BrandTiles, type BrandTile } from '@/features/tienda/components/StoreNav';
import type { Product } from '@/features/tienda/types';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './TiendaCategoriaPage.module.css';

const SORTS = [
  { value: 'destacados', label: 'Destacados' },
  { value: 'precio-asc', label: 'Precio: menor a mayor' },
  { value: 'precio-desc', label: 'Precio: mayor a menor' },
  { value: 'nombre', label: 'Nombre' },
];

const normalize = (s: string) => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

function sortProducts(list: Product[], sort: string) {
  return [...list].sort((a, b) =>
    sort === 'precio-asc' ? a.price - b.price : sort === 'precio-desc' ? b.price - a.price : sort === 'nombre' ? a.name.localeCompare(b.name, 'es') : a.featured - b.featured,
  );
}

/**
 * Tienda por categoría (F24): /tienda/:categoria muestra las marcas de la categoría y todos sus productos;
 * /tienda/:categoria/:marca solo los de esa marca. Las marcas salen de los productos a la venta.
 */
export default function TiendaCategoriaPage() {
  const { categoria = '', marca } = useParams();
  const [attempt, setAttempt] = useState(0);
  const cats = useAsync(getStoreCategories, [attempt]);
  const prods = useAsync(getProducts, [attempt]);
  const cart = useCart();
  const [q, setQ] = useState('');
  const [sort, setSort] = useState('destacados');

  const category = cats.data?.find((c) => c.slug === categoria) ?? null;
  const inCategory = useMemo(() => (prods.data ?? []).filter((p) => p.storeCategory?.slug === categoria), [prods.data, categoria]);

  const brands = useMemo<BrandTile[]>(() => {
    const map = new Map<string, BrandTile>();
    for (const p of inCategory) {
      if (!p.brand) continue;
      const b = map.get(p.brand.slug);
      if (b) b.count += 1;
      else map.set(p.brand.slug, { ...p.brand, count: 1 });
    }
    return [...map.values()].sort((a, b) => a.order - b.order || a.name.localeCompare(b.name, 'es'));
  }, [inCategory]);
  const brand = marca ? brands.find((b) => b.slug === marca) ?? null : null;

  const visible = useMemo(() => {
    const t = normalize(q.trim());
    const list = inCategory.filter((p) => (!marca || p.brand?.slug === marca) && (!t || normalize(`${p.name} ${p.description} ${p.brand?.name ?? ''}`).includes(t)));
    return sortProducts(list, sort);
  }, [inCategory, marca, q, sort]);

  if ((cats.loading && !cats.data) || (prods.loading && !prods.data)) return <LoadingScreen />;

  if (cats.error || prods.error) {
    return (
      <div className={`container ${styles.page}`}>
        <EmptyState
          title="No pudimos cargar la tienda"
          description={(cats.error ?? prods.error)!.message}
          action={<Button onClick={() => setAttempt((n) => n + 1)}>Reintentar</Button>}
        />
      </div>
    );
  }

  // Sin F24 en el backend (o categoría inexistente/inactiva).
  if (!category) {
    return (
      <div className={`container ${styles.page}`}>
        <title>Categoría no disponible · PCP Shooting</title>
        <EmptyState
          title={cats.data === null ? 'Las categorías de la tienda estarán disponibles pronto' : 'Categoría no disponible'}
          description={cats.data === null ? 'Mientras tanto puedes ver todos los productos en la tienda.' : 'Esta categoría ya no existe o el enlace cambió.'}
          action={<ButtonLink to="/tienda">Ver la tienda</ButtonLink>}
        />
      </div>
    );
  }

  const title = brand ? `${brand.name} · ${category.name}` : category.name;

  return (
    <div className={`container ${styles.page}`}>
      <title>{`${title} · Tienda PCP Shooting`}</title>
      <meta name="description" content={category.description || `${category.name} en la tienda oficial de PCP Shooting. Compra en línea y recoge en el club.`} />

      <nav className={styles.crumbs} aria-label="Ruta">
        <Link to="/tienda" className={styles.back}>
          <IconArrowLeft size={16} /> Tienda
        </Link>
        <span aria-hidden="true">/</span>
        {brand ? <Link to={`/tienda/${category.slug}`}>{category.name}</Link> : <span aria-current="page">{category.name}</span>}
        {brand && (
          <>
            <span aria-hidden="true">/</span>
            <span aria-current="page">{brand.name}</span>
          </>
        )}
      </nav>

      <header className={styles.header}>
        {brand?.logoUrl && <img src={brand.logoUrl} alt="" className={styles.headerLogo} />}
        <div>
          <h1 className={styles.title}>{brand ? brand.name : category.name}</h1>
          <p className={styles.lead}>{brand ? `${category.name} de ${brand.name}` : category.description || 'Elige una marca o mira todos los productos.'}</p>
        </div>
      </header>

      {brands.length > 0 && (
        <section className={styles.section} aria-labelledby="marcas-titulo">
          <h2 id="marcas-titulo" className={styles.sectionTitle}>Marcas</h2>
          <BrandTiles categorySlug={category.slug} brands={brands} active={brand?.slug} total={inCategory.length} />
        </section>
      )}

      <section className={styles.section} aria-labelledby="lista-titulo">
        <div className={styles.toolbar}>
          <div>
            <h2 id="lista-titulo" className={styles.sectionTitle}>{brand ? `Productos ${brand.name}` : `Todos los productos de ${category.name.toLowerCase()}`}</h2>
            <p className={styles.count}>{visible.length} producto{visible.length === 1 ? '' : 's'}</p>
          </div>
          <div className={styles.filters}>
            <TextField label="Buscar" type="search" placeholder={`Buscar en ${category.name.toLowerCase()}…`} value={q} onChange={(e) => setQ(e.target.value)} />
            <SelectField label="Ordenar por" value={sort} options={SORTS} onChange={(e) => setSort(e.target.value)} />
          </div>
        </div>

        {marca && !brand ? (
          <EmptyState title="Marca no disponible" description={`No hay productos de esta marca en ${category.name.toLowerCase()}.`} action={<ButtonLink to={`/tienda/${category.slug}`}>Ver todas las marcas</ButtonLink>} />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={IconSearch}
            title={q ? 'Sin resultados' : 'Pronto habrá productos aquí'}
            description={q ? 'No encontramos productos con esa búsqueda.' : 'Estamos cargando el catálogo de esta categoría. Mientras tanto, mira el resto de la tienda.'}
            action={!q ? <ButtonLink to="/tienda">Ver la tienda</ButtonLink> : undefined}
          />
        ) : (
          <ProductGrid>
            {visible.map((p) => (
              <ProductCard key={p.id} product={cardData(p)} href={productPath(p)} added={cart.has(p.id)} onAdd={(size) => addToCart(cart, p, size)} />
            ))}
          </ProductGrid>
        )}
      </section>
    </div>
  );
}
