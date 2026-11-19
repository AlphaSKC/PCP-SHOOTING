import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  return {
    plugins: [react()],
    resolve: {
      alias: { '@': '/src' },
    },
    build: {
      rollupOptions: {
        output: {
          // Librerías grandes en archivos aparte: cambian poco y el navegador las guarda en caché.
          manualChunks: {
            react: ['react', 'react-dom', 'react-router'],
            supabase: ['@supabase/supabase-js'],
          },
        },
      },
    },
    server: {
      // La API C# corre en local en este puerto (launchSettings.json del backend).
      // El proxy evita depender de CORS durante el desarrollo. Se puede cambiar con API_PROXY_TARGET.
      proxy: {
        '/api': { target: env.API_PROXY_TARGET || 'http://localhost:5026', changeOrigin: true, secure: false },
      },
    },
  };
});
