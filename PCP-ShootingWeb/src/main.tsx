import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router/dom';
import { router } from '@/app/router';
import { AuthProvider } from '@/features/auth/AuthProvider';
import { CartProvider } from '@/features/tienda/CartProvider';
import '@/styles/tokens.css';
import '@/styles/base.css';

const root = document.getElementById('root');
if (!root) throw new Error('Missing React root element.');

createRoot(root).render(
  <StrictMode>
    <AuthProvider>
      <CartProvider>
        <RouterProvider router={router} />
      </CartProvider>
    </AuthProvider>
  </StrictMode>,
);
