import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router/dom';
import { router } from '@/app/router';
import { AuthProvider } from '@/features/auth/AuthProvider';
import { CartProvider } from '@/features/tienda/CartProvider';
import { ContenidoProvider } from '@/features/contenido/ContenidoProvider';
import '@/styles/tokens.css';
import '@/styles/base.css';

const root = document.getElementById('root');
if (!root) throw new Error('Missing React root element.');

// El HTML prerenderizado (SEO) trae título, descripción y vistas previas fijas: React pone las suyas por página.
document.head.querySelectorAll('[data-prerender]').forEach((el) => el.remove());

createRoot(root).render(
  <StrictMode>
    <AuthProvider>
      <CartProvider>
        <ContenidoProvider><RouterProvider router={router} /></ContenidoProvider>
      </CartProvider>
    </AuthProvider>
  </StrictMode>,
);
