/*
 * Portadas de página (foto de fondo del encabezado negro) · contrato propuesto F21 (docs/PORTADAS.md).
 *   GET  /api/portadas                                    (público) páginas ya configuradas: [{ pagina, activa, imagenUrl, posicion, velo }]
 *   GET  /api/administracion/portadas                     (Administrador) una fila por página, con version
 *   PUT  /api/administracion/portadas/{pagina}            { activa, imagenUrl, posicion, velo, version }
 *   POST /api/administracion/portadas/{pagina}/imagen     multipart { archivo, idOperacion } → { imagenUrl, version }
 * Mientras el backend no publique F21 (404/405): el sitio usa las portadas fijas del código (solo Inicio)
 * y el panel funciona como vista previa sin guardar.
 */
import { useEffect, useState } from 'react';
import { ApiError, DATA_SOURCE, apiGet, apiPut, apiUpload } from '@/lib/api';

export type Veil = 'suave' | 'normal' | 'fuerte';
export type PortadaPage = { key: string; label: string; path: string; fallback?: { image: string; position: string } };

/** Páginas públicas con encabezado negro (PageHero oscuro). Blog y Galería usan encabezado claro: no aplican. */
export const PORTADA_PAGES: PortadaPage[] = [
  { key: 'inicio', label: 'Inicio', path: '/', fallback: { image: '/img/hero-tirador.webp', position: '50% 30%' } },
  { key: 'nosotros', label: 'Nosotros', path: '/nosotros' },
  { key: 'reservar', label: 'Reservar línea', path: '/reservar' },
  { key: 'catalogo', label: 'Catálogo de armas', path: '/catalogo' },
  { key: 'tienda', label: 'Tienda', path: '/tienda' },
  { key: 'eventos', label: 'Eventos', path: '/eventos' },
  { key: 'alimentos', label: 'Alimentos y bebidas', path: '/alimentos' },
  { key: 'membresias', label: 'Membresías', path: '/membresias' },
  { key: 'contacto', label: 'Contacto', path: '/contacto' },
];

export type PublicPortada = { page: string; active: boolean; imageUrl: string | null; position: string; veil: Veil };

export type PortadaDraft = {
  page: string;
  active: boolean;
  imageUrl: string | null;
  position: string;
  veil: Veil;
  version: string | null;
  updatedUtc: string | null;
};

type ApiPublic = { pagina: string; activa: boolean; imagenUrl: string | null; posicion: string | null; velo: Veil | null };
type ApiAdmin = { pagina: string; activa: boolean; imagenUrl: string | null; posicion: string | null; velo: Veil | null; version: string; actualizadoEnUtc: string | null };

export const IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';
export const IMAGE_MAX_BYTES = 8 * 1024 * 1024;
export const DEFAULT_POSITION = '50% 50%';

export const isMissingEndpoint = (e: unknown) => e instanceof ApiError && (e.status === 404 || e.status === 405);

/* ---------- Sitio público ---------- */

let cache: Promise<Map<string, PublicPortada> | null> | null = null;

/** null = el backend aún no tiene portadas (se usan las fijas). */
function loadPublic(): Promise<Map<string, PublicPortada> | null> {
  if (DATA_SOURCE !== 'api') return Promise.resolve(null);
  cache ??= apiGet<ApiPublic[]>('/api/portadas')
    .then(
      (list) =>
        new Map(list.map((p) => [p.pagina, { page: p.pagina, active: p.activa, imageUrl: p.imagenUrl, position: p.posicion || DEFAULT_POSITION, veil: p.velo ?? 'normal' }])),
    )
    .catch(() => null);
  return cache;
}

/** Tras guardar en el panel, la siguiente navegación del sitio vuelve a consultar. */
export const refreshPublicPortadas = () => {
  cache = null;
};

const pageForPath = (path: string) => {
  const clean = path !== '/' ? path.replace(/\/$/, '') : '/';
  return PORTADA_PAGES.find((p) => p.path === clean) ?? null;
};

/**
 * Portada de la página actual.
 * - Con F21 publicado: la que configuró el Administrador (no configurada → la fija del código; apagada → ninguna).
 * - Sin F21 o en demo: la fija del código (hoy solo Inicio).
 */
export function usePortada(pathname: string): { image: string; position: string; veil: Veil } | null {
  const page = pageForPath(pathname);
  const fallback = page?.fallback ? { image: page.fallback.image, position: page.fallback.position, veil: 'normal' as Veil } : null;
  const [state, setState] = useState<{ key: string | null; value: { image: string; position: string; veil: Veil } | null; known: boolean }>({ key: null, value: null, known: false });

  useEffect(() => {
    if (!page) return;
    let alive = true;
    void loadPublic().then((map) => {
      if (!alive) return;
      if (!map) return setState({ key: page.key, value: fallback, known: true });
      const p = map.get(page.key);
      // Sin configurar → la fija del código. Configurada: activa con imagen → esa; apagada → sin foto.
      const value = !p ? fallback : p.active && p.imageUrl ? { image: p.imageUrl, position: p.position, veil: p.veil } : null;
      setState({ key: page.key, value, known: true });
    });
    return () => {
      alive = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page?.key]);

  if (!page) return null;
  // Mientras carga, la fija (evita parpadeo en Inicio); ya cargada, lo que diga la API.
  return state.key === page.key && state.known ? state.value : fallback;
}

/* ---------- Panel ---------- */

const toDraft = (a: ApiAdmin): PortadaDraft => ({
  page: a.pagina,
  active: a.activa,
  imageUrl: a.imagenUrl,
  position: a.posicion || DEFAULT_POSITION,
  veil: a.velo ?? 'normal',
  version: a.version,
  updatedUtc: a.actualizadoEnUtc,
});

/** Borradores locales (sin guardar) para la vista previa cuando el backend aún no tiene F21. */
export const localDrafts = (): PortadaDraft[] =>
  PORTADA_PAGES.map((p) => ({
    page: p.key,
    active: Boolean(p.fallback),
    imageUrl: p.fallback?.image ?? null,
    position: p.fallback?.position ?? DEFAULT_POSITION,
    veil: 'normal',
    version: null,
    updatedUtc: null,
  }));

export async function listAdminPortadas(): Promise<PortadaDraft[]> {
  const list = (await apiGet<ApiAdmin[]>('/api/administracion/portadas')).map(toDraft);
  const byKey = new Map(list.map((d) => [d.page, d]));
  // Página aún no configurada en el backend: se muestra con la portada fija del sitio (si tiene) y sin versión.
  return PORTADA_PAGES.map((p) => byKey.get(p.key) ?? localDrafts().find((d) => d.page === p.key)!);
}

export async function savePortada(d: PortadaDraft): Promise<PortadaDraft> {
  return toDraft(
    await apiPut<ApiAdmin>(`/api/administracion/portadas/${d.page}`, {
      activa: d.active,
      imagenUrl: d.imageUrl,
      posicion: d.position,
      velo: d.veil,
      version: d.version,
    }),
  );
}

export async function uploadPortada(page: string, file: File, operationId: string): Promise<{ imageUrl: string; version: string }> {
  const form = new FormData();
  form.append('archivo', file);
  form.append('idOperacion', operationId);
  const r = await apiUpload<{ imagenUrl: string; version: string }>(`/api/administracion/portadas/${page}/imagen`, form);
  return { imageUrl: r.imagenUrl, version: r.version };
}
