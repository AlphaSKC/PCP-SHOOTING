import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { Button, ButtonLink, EmptyState, Photo, ProductCard, ProductGrid, QuantityStepper } from '@/components/ui';
import { IconArrowLeft, IconBadge, IconLock, IconPin } from '@/components/icons/icons';
import { Markdown } from '@/features/blog/Markdown';
import { useAsync } from '@/features/reservas/useAsync';
import {
  ONLINE_PAYMENT,
  addToCart,
  availableFor,
  cachedProductByCode,
  canBuy,
  cardData,
  cachedProducts,
  getProductByCode,
  productPath,
} from '@/features/tienda/api';
import { useCart } from '@/features/tienda/CartProvider';
import type { Product } from '@/features/tienda/types';
import { cx, formatMXN } from '@/lib/format';
import { toast } from '@/lib/toast';
import { LoadingScreen } from '@/pages/LoadingScreen';
import styles from './ProductoPage.module.css';

const MAX_QTY = 10;

/**
 * Vista previa de un producto de la tienda (/tienda/producto/:sku), estilo ficha de tienda deportiva:
 * galería grande a la izquierda y compra a la derecha (en celular, fotos deslizables arriba).
 * Precio y existencias salen del catálogo público; la API vuelve a validar al crear el pedido.
 */
export default function ProductoPage() {
  const { sku = '' } = useParams();
  const code = decodeURIComponent(sku);
  const [attempt, setAttempt] = useState(0);
  const { data, loading, error } = useAsync(() => getProductByCode(code), [code, attempt]);
  // Mientras llega la versión fresca se muestra la que ya estaba en caché (navegación instantánea desde la tienda).
  const product = data === undefined || (loading && !data) ? (cachedProductByCode(code) ?? null) : data;

  if (!product) {
    if (loading) return <LoadingScreen />;
    return (
      <div className={`container ${styles.page}`}>
        <title>Producto no disponible · PCP Shooting</title>
        {error ? (
          <EmptyState
            title="No pudimos cargar el producto"
            description={error.message}
            action={<Button onClick={() => setAttempt((n) => n + 1)}>Reintentar</Button>}
          />
        ) : (
          <EmptyState
            title="Producto no disponible"
            description="Este artículo ya no está a la venta o el enlace cambió."
            action={<ButtonLink to="/tienda">Ver la tienda</ButtonLink>}
          />
        )}
      </div>
    );
  }
  return <ProductView key={product.id} product={product} />;
}

function ProductView({ product }: { product: Product }) {
  const cart = useCart();
  const images = product.images?.length ? product.images : product.imageUrl ? [product.imageUrl] : [];
  const [size, setSize] = useState<string | undefined>(undefined);
  const [sizeError, setSizeError] = useState(false);
  const [qty, setQty] = useState(1);
  const soldOut = !canBuy(product);
  const left = product.sizes ? (size ? availableFor(product, size) : undefined) : availableFor(product);
  const maxQty = left === undefined || !Number.isFinite(left) ? MAX_QTY : Math.max(1, Math.min(MAX_QTY, left));

  // Si cambia la talla y hay menos piezas, se ajusta la cantidad.
  useEffect(() => setQty((q) => Math.min(q, maxQty)), [maxQty]);

  function onAdd() {
    if (product.sizes && !size) {
      setSizeError(true);
      document.getElementById('tallas')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }
    cart.add(product, size, qty);
    toast.success(`Agregado al carrito: ${product.name}${size ? ` · talla ${size}` : ''}${qty > 1 ? ` · ${qty} piezas` : ''}.`);
    if (window.matchMedia('(min-width: 768px)').matches) cart.openDrawer();
  }

  const description = product.description.trim();
  const shareImage = images[0];

  return (
    <div className={`container ${styles.page}`}>
      <title>{`${product.name} · Tienda PCP Shooting`}</title>
      {description && <meta name="description" content={description.replace(/\s+/g, ' ').slice(0, 155)} />}
      <meta property="og:title" content={`${product.name} · PCP Shooting`} />
      <meta property="og:type" content="product" />
      {shareImage && <meta property="og:image" content={shareImage} />}

      <nav className={styles.crumbs} aria-label="Ruta">
        <Link to="/tienda" className={styles.back}>
          <IconArrowLeft size={16} /> Tienda
        </Link>
        <span aria-hidden="true">/</span>
        {product.storeCategory ? (
          <>
            <Link to={`/tienda/${product.storeCategory.slug}`}>{product.storeCategory.name}</Link>
            {product.brand && (
              <>
                <span aria-hidden="true">/</span>
                <Link to={`/tienda/${product.storeCategory.slug}/${product.brand.slug}`}>{product.brand.name}</Link>
              </>
            )}
          </>
        ) : (
          <Link to={`/tienda?categoria=${product.category}`}>{product.categoryLabel}</Link>
        )}
      </nav>

      <div className={styles.layout}>
        <Gallery images={images} name={product.name} soldOut={soldOut} />

        <section className={styles.info} aria-labelledby="producto-nombre">
          <span className={styles.category}>{product.brand?.name ?? product.storeCategory?.name ?? product.categoryLabel}</span>
          <h1 id="producto-nombre" className={styles.name}>{product.name}</h1>
          <p className={styles.price}>{formatMXN(product.price)}</p>
          {soldOut ? (
            <p className={cx(styles.stock, styles.stockOut)}>Agotado</p>
          ) : left !== undefined && Number.isFinite(left) && left > 0 && left <= 3 ? (
            <p className={styles.stock}>¡Quedan {left}!</p>
          ) : null}

          {product.sizes && product.sizes.length > 0 && (
            <fieldset id="tallas" className={styles.sizes} aria-describedby={sizeError ? 'talla-error' : undefined}>
              <legend className={styles.legend}>Elige tu talla</legend>
              <div className={styles.sizeGrid}>
                {product.sizes.map((s) => {
                  const out = availableFor(product, s) <= 0;
                  return (
                    <button
                      key={s}
                      type="button"
                      className={cx(styles.size, size === s && styles.sizeOn, out && styles.sizeOut)}
                      aria-pressed={size === s}
                      disabled={out}
                      title={out ? `Talla ${s} agotada` : `Talla ${s}`}
                      onClick={() => {
                        setSize(s);
                        setSizeError(false);
                      }}
                    >
                      {s}
                    </button>
                  );
                })}
              </div>
              {sizeError && (
                <p id="talla-error" className={styles.sizeError} role="alert">
                  Elige una talla para agregar al carrito.
                </p>
              )}
            </fieldset>
          )}

          {!soldOut && (
            <div className={styles.qtyRow}>
              <span className={styles.legend}>Cantidad</span>
              <QuantityStepper value={qty} onChange={setQty} label={product.name} min={1} max={maxQty} />
            </div>
          )}

          <div className={styles.actions}>
            <Button size="lg" fullWidth disabled={soldOut} onClick={onAdd}>
              {soldOut ? 'Agotado' : 'Agregar al carrito'}
            </Button>
            {cart.has(product.id) && (
              <ButtonLink to="/tienda/carrito" variant="secondary" size="lg" fullWidth className={styles.mobileOnly}>
                Ver carrito ({cart.count})
              </ButtonLink>
            )}
          </div>

          <ul className={styles.perks}>
            <li>
              <IconPin size={18} />
              <span>
                <strong>Recoge en el club</strong> Km 23 Ruta de los Cenotes. No hay envíos a domicilio.
              </span>
            </li>
            <li>
              <IconLock size={18} />
              <span>
                {ONLINE_PAYMENT ? (
                  <><strong>Pago seguro en línea</strong> Tarjeta de crédito, débito y efectivo.</>
                ) : (
                  <><strong>Aparta en línea</strong> Pagas al recoger tu pedido en el club.</>
                )}
              </span>
            </li>
            <li>
              <IconBadge size={18} />
              <span>
                <strong>Producto oficial</strong> Diseño y logo de PCP Shooting.
              </span>
            </li>
          </ul>

          {(description || product.notes) && (
            <div className={styles.details}>
              <h2 className={styles.detailsTitle}>Descripción</h2>
              <ExpandableText>
                {description && (
                  <div className={styles.description}>
                    {description.split(/\n{2,}/).map((p, i) => (
                      <p key={i}>{p}</p>
                    ))}
                  </div>
                )}
                {product.notes && <Markdown className={styles.notes}>{product.notes}</Markdown>}
              </ExpandableText>
            </div>
          )}
        </section>
      </div>

      <Related current={product} />
    </div>
  );
}

/**
 * Texto largo recortado a unas líneas con «Leer más» (como las fichas de Nike, Amazon o Liverpool).
 * Solo muestra el botón si el texto realmente no cabe.
 */
function ExpandableText({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [overflows, setOverflows] = useState(false);
  const box = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = box.current;
    if (!el) return;
    const check = () => setOverflows(el.scrollHeight > el.clientHeight + 2);
    check();
    const ro = new ResizeObserver(check);
    ro.observe(el);
    return () => ro.disconnect();
  }, [children]);
  return (
    <>
      <div ref={box} id="producto-descripcion" className={cx(styles.expandable, !open && styles.collapsed, !open && overflows && styles.faded)}>
        {children}
      </div>
      {(overflows || open) && (
        <button type="button" className={styles.more} aria-expanded={open} aria-controls="producto-descripcion" onClick={() => setOpen((o) => !o)}>
          {open ? 'Ver menos' : 'Leer más'}
        </button>
      )}
    </>
  );
}

/** Galería: miniaturas + foto grande en tableta/escritorio; fotos deslizables con contador en celular. */
function Gallery({ images, name, soldOut }: { images: string[]; name: string; soldOut: boolean }) {
  const [active, setActive] = useState(0);
  const track = useRef<HTMLDivElement>(null);

  // En celular, el contador sigue a la foto visible al deslizar.
  function onScroll() {
    const el = track.current;
    if (!el) return;
    const i = Math.round(el.scrollLeft / Math.max(1, el.clientWidth));
    if (i !== active) setActive(i);
  }
  function show(i: number) {
    setActive(i);
    track.current?.scrollTo({ left: i * track.current.clientWidth, behavior: 'smooth' });
  }

  if (images.length === 0) {
    return (
      <div className={styles.gallery}>
        <div className={styles.main}>
          <Photo alt={`Foto: ${name}`} />
          {soldOut && <span className={styles.badge}>Agotado</span>}
        </div>
      </div>
    );
  }

  return (
    <div className={cx(styles.gallery, images.length > 1 && styles.galleryMulti)}>
      {images.length > 1 && (
        <ul className={styles.thumbs} aria-label="Fotos del producto">
          {images.map((src, i) => (
            <li key={src}>
              <button
                type="button"
                className={cx(styles.thumb, i === active && styles.thumbOn)}
                aria-label={`Ver foto ${i + 1} de ${images.length}`}
                aria-current={i === active}
                onMouseEnter={() => show(i)}
                onFocus={() => show(i)}
                onClick={() => show(i)}
              >
                <img src={src} alt="" loading="lazy" />
              </button>
            </li>
          ))}
        </ul>
      )}
      <div className={styles.main}>
        <div ref={track} className={styles.track} onScroll={onScroll}>
          {images.map((src, i) => (
            <img
              key={src}
              src={src}
              alt={i === 0 ? `Foto: ${name}` : `Foto ${i + 1} de ${name}`}
              className={styles.slide}
              loading={i === 0 ? 'eager' : 'lazy'}
              decoding="async"
            />
          ))}
        </div>
        {soldOut && <span className={styles.badge}>Agotado</span>}
        {images.length > 1 && (
          <span className={styles.counter} aria-hidden="true">
            {active + 1} / {images.length}
          </span>
        )}
      </div>
    </div>
  );
}

/** «También te puede gustar»: primero de la misma categoría, máximo 4. */
function Related({ current }: { current: Product }) {
  const cart = useCart();
  // El catálogo ya se cargó al abrir el producto: no se vuelve a pedir.
  const list = useMemo(() => {
    const others = cachedProducts().filter((p) => p.id !== current.id);
    return [...others.filter((p) => p.category === current.category), ...others.filter((p) => p.category !== current.category)].slice(0, 4);
  }, [current]);
  if (list.length === 0) return null;
  return (
    <section className={styles.related} aria-labelledby="relacionados">
      <h2 id="relacionados" className={styles.relatedTitle}>También te puede gustar</h2>
      <ProductGrid>
        {list.map((p) => (
          <ProductCard key={p.id} product={cardData(p)} href={productPath(p)} added={cart.has(p.id)} onAdd={(size) => addToCart(cart, p, size)} />
        ))}
      </ProductGrid>
    </section>
  );
}
