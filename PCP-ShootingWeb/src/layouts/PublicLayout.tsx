import { useState } from 'react';
import { Outlet, useMatches, useNavigate } from 'react-router';
import { tabBar } from '@/app/navigation';
import { RouteSeo } from '@/app/seo';
import { SiteHeader } from '@/components/layout/SiteHeader';
import { SideMenu } from '@/components/layout/SideMenu';
import { Footer } from '@/components/layout/Footer';
import { TabBar } from '@/components/layout/TabBar';
import { CartDrawer } from '@/components/ui';
import { NoticeBanner } from '@/features/avisos/NoticeBanner';
import { stockView, useCart } from '@/features/tienda/CartProvider';
import { productPath } from '@/features/tienda/api';
import styles from './PublicLayout.module.css';

export default function PublicLayout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const cart = useCart();
  const navigate = useNavigate();

  // Las rutas pueden pedir el footer mínimo (`compactFooter`) u ocultar las pestañas de celular (`hideTabBar`).
  const handles = useMatches().map((m) => (m.handle ?? {}) as { compactFooter?: boolean; hideTabBar?: boolean });
  const compactFooter = handles.some((h) => h.compactFooter);
  const hideTabBar = handles.some((h) => h.hideTabBar);

  return (
    <div className={styles.shell}>
      <RouteSeo />
      <a href="#contenido" className="skip-link">
        Saltar al contenido
      </a>
      <NoticeBanner />
      <SiteHeader
        cartCount={cart.count}
        menuOpen={menuOpen}
        onOpenMenu={() => setMenuOpen(true)}
        onOpenCart={cart.openDrawer}
      />
      <main id="contenido" className={styles.main}>
        <Outlet />
      </main>
      <Footer compact={compactFooter} />
      {!hideTabBar && <TabBar label="Secciones" items={tabBar} />}
      <SideMenu open={menuOpen} onClose={() => setMenuOpen(false)} />
      <CartDrawer
        open={cart.drawerOpen}
        onClose={cart.closeDrawer}
        lines={cart.lines.map((l) => ({
          id: l.key,
          name: l.product.name,
          href: productPath(l.product),
          price: l.product.price,
          quantity: l.quantity,
          imageUrl: l.product.imageUrl,
          size: l.size,
          sizes: l.product.sizes,
          ...stockView(l),
        }))}
        onQuantityChange={cart.setQuantity}
        onSizeChange={cart.setSize}
        onRemove={cart.remove}
        onCheckout={() => {
          cart.closeDrawer();
          navigate('/tienda/checkout');
        }}
        onContinue={() => {
          cart.closeDrawer();
          navigate('/tienda');
        }}
      />
    </div>
  );
}
