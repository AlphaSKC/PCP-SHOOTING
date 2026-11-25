import { useEffect, type ComponentType } from 'react';
import { createBrowserRouter, Outlet, ScrollRestoration, useLocation, type RouteObject } from 'react-router';
import PublicLayout from '@/layouts/PublicLayout';
import { RequireAuth, RequireRole } from '@/app/auth';
import { ErrorPage } from '@/pages/ErrorPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

/**
 * Carga diferida por página: cada pantalla es su propio archivo JS,
 * así el panel admin no pesa en el sitio público.
 */
function page(load: () => Promise<{ default: ComponentType }>) {
  return async () => ({ Component: (await load()).default });
}

/** Enlaces con ancla (/nosotros#modos): baja hasta la sección cuando la página ya cargó. */
function ScrollToHash() {
  const { hash, pathname } = useLocation();
  useEffect(() => {
    if (!hash) return;
    const id = decodeURIComponent(hash.slice(1));
    // Espera un cuadro para que la página diferida termine de pintar.
    const frame = requestAnimationFrame(() => document.getElementById(id)?.scrollIntoView());
    return () => cancelAnimationFrame(frame);
  }, [hash, pathname]);
  return null;
}

function Root() {
  return (
    <>
      <ScrollRestoration />
      <ScrollToHash />
      <Outlet />
    </>
  );
}

const publicRoutes: RouteObject[] = [
  { index: true, lazy: page(() => import('@/pages/public/inicio/InicioPage')) },
  { path: 'nosotros', lazy: page(() => import('@/pages/public/nosotros/NosotrosPage')) },
  { path: 'catalogo', lazy: page(() => import('@/pages/public/catalogo/CatalogoPage')) },
  {
    path: 'reservar',
    // En celular el wizard usa su propia barra fija (Continuar), en lugar de las pestañas.
    handle: { hideTabBar: true },
    lazy: page(() => import('@/pages/public/reservar/ReservarPage')),
  },
  {
    path: 'reservar/confirmacion/:id',
    lazy: page(() => import('@/pages/public/reservar/ConfirmacionPage')),
  },
  { path: 'tienda', lazy: page(() => import('@/pages/public/tienda/TiendaPage')) },
  {
    // En celular estas pantallas usan su barra fija de pago en lugar de las pestañas.
    handle: { hideTabBar: true },
    children: [
      { path: 'tienda/carrito', lazy: page(() => import('@/pages/public/tienda/CarritoPage')) },
      { path: 'tienda/checkout', lazy: page(() => import('@/pages/public/tienda/CheckoutPage')) },
    ],
  },
  { path: 'tienda/pedido/:id', lazy: page(() => import('@/pages/public/tienda/PedidoPage')) },
  {
    path: 'alimentos',
    // En celular usa su barra fija "Ver mi pedido" en lugar de las pestañas.
    handle: { hideTabBar: true },
    lazy: page(() => import('@/pages/public/alimentos/AlimentosPage')),
  },
  { path: 'eventos', lazy: page(() => import('@/pages/public/eventos/EventosPage')) },
  { path: 'eventos/:slug', lazy: page(() => import('@/pages/public/eventos/EventoPage')) },
  { path: 'membresias', lazy: page(() => import('@/pages/public/membresias/MembresiasPage')) },
  { path: 'galeria', lazy: page(() => import('@/pages/public/galeria/GaleriaPage')) },
  { path: 'blog', lazy: page(() => import('@/pages/public/blog/BlogPage')) },
  { path: 'blog/:slug', lazy: page(() => import('@/pages/public/blog/BlogPostPage')) },
  { path: 'contacto', lazy: page(() => import('@/pages/public/contacto/ContactoPage')) },
  {
    handle: { compactFooter: true },
    children: [
      { path: 'login', lazy: page(() => import('@/pages/public/auth/LoginPage')) },
      { path: 'registro', lazy: page(() => import('@/pages/public/auth/RegistroPage')) },
      { path: 'recuperar', lazy: page(() => import('@/pages/public/auth/RecuperarPage')) },
      { path: 'restablecer', lazy: page(() => import('@/pages/public/auth/RestablecerPage')) },
    ],
  },
  {
    element: <RequireAuth />,
    children: [{ path: 'perfil', lazy: page(() => import('@/pages/public/perfil/PerfilPage')) }],
  },
  // Catálogo de componentes: solo existe en desarrollo (no se publica).
  ...(import.meta.env.DEV
    ? [{ path: 'dev/componentes', lazy: page(() => import('@/pages/dev/ComponentesPage')) }]
    : []),
  { path: '*', element: <NotFoundPage /> },
];

const adminRoutes: RouteObject[] = [
  { index: true, lazy: page(() => import('@/pages/admin/AdminInicioPage')) },
  { path: 'reservas', lazy: page(() => import('@/pages/admin/AdminReservasPage')) },
  { path: 'pedidos', lazy: page(() => import('@/pages/admin/AdminPedidosPage')) },
  { path: 'alimentos', lazy: page(() => import('@/pages/admin/AdminAlimentosPage')) },
  {
    // Solo administradores; recepción ve inicio, reservas, pedidos y alimentos.
    element: <RequireRole roles={['admin']} />,
    children: [
      { path: 'productos', lazy: page(() => import('@/pages/admin/AdminProductosPage')) },
      { path: 'paquetes', lazy: page(() => import('@/pages/admin/AdminPaquetesPage')) },
      { path: 'inventario', lazy: page(() => import('@/pages/admin/AdminInventarioPage')) },
      { path: 'eventos', lazy: page(() => import('@/pages/admin/AdminEventosPage')) },
      { path: 'clientes', lazy: page(() => import('@/pages/admin/AdminClientesPage')) },
      { path: 'reportes', lazy: page(() => import('@/pages/admin/AdminReportesPage')) },
      { path: 'contenido', lazy: page(() => import('@/pages/admin/AdminContenidoPage')) },
      { path: 'portadas', lazy: page(() => import('@/pages/admin/AdminPortadasPage')) },
      { path: 'contacto', lazy: page(() => import('@/pages/admin/AdminContactoPage')) },
      { path: 'configuracion', lazy: page(() => import('@/pages/admin/AdminConfigPage')) },
    ],
  },
  { path: '*', element: <NotFoundPage admin /> },
];

export const router = createBrowserRouter([
  {
    element: <Root />,
    errorElement: <ErrorPage />,
    children: [
      { path: '/', element: <PublicLayout />, children: publicRoutes },
      {
        path: '/admin',
        element: <RequireRole roles={['admin', 'recepcion']} />,
        children: [
          {
            lazy: page(() => import('@/layouts/AdminLayout')),
            children: adminRoutes,
          },
        ],
      },
    ],
  },
]);
