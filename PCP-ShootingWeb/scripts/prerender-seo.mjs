/*
 * SEO al compilar (se ejecuta después de `vite build`).
 *
 * Genera, a partir de dist/index.html y src/app/seo.json:
 *   - Un HTML por página pública (dist/nosotros.html, dist/blog.html…) con su <title>, descripción, canonical,
 *     Open Graph/Twitter, datos estructurados y un contenido base legible sin JavaScript (título, texto y enlaces).
 *     React lo reemplaza al cargar. Vercel los sirve con `cleanUrls` (/nosotros → nosotros.html).
 *   - dist/app.html: el "shell" neutro para el resto de rutas (rewrite de Vercel).
 *   - dist/sitemap.xml y dist/robots.txt.
 *   - Artículos del blog (dist/blog/<slug>.html + sitemap) si se define SEO_API_URL y la API responde.
 *
 * Variables (archivo .env* o entorno): VITE_SITE_URL (por defecto seo.json → defaultUrl), SEO_API_URL (opcional).
 */
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEnv } from 'vite';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const dist = join(root, 'dist');
const env = { ...loadEnv('production', root, ''), ...process.env };
const seo = JSON.parse(await readFile(join(root, 'src/app/seo.json'), 'utf8'));
const SITE = (env.VITE_SITE_URL || seo.defaultUrl).replace(/\/$/, '');
const API = (env.SEO_API_URL || '').replace(/\/$/, '');
const abs = (p) => (/^https?:\/\//.test(p) ? p : `${SITE}${p.startsWith('/') ? p : `/${p}`}`);

const esc = (s) => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const jsonLd = (data) => `<script type="application/ld+json" data-prerender>${JSON.stringify(data).replace(/</g, '\\u003c')}</script>`;

// Si el script ya corrió, index.html es la portada prerenderizada: el shell limpio está en app.html.
let shell = await readFile(join(dist, 'index.html'), 'utf8');
if (!shell.includes('<div id="root"></div>')) shell = await readFile(join(dist, 'app.html'), 'utf8').catch(() => '');
if (!shell.includes('<div id="root"></div>')) throw new Error('No encontré el shell de Vite con <div id="root"></div>: ejecuta primero `vite build`.');

const business = () => {
  const b = seo.business;
  return {
    '@context': 'https://schema.org',
    '@type': 'SportsActivityLocation',
    name: b.name,
    description: b.description,
    url: SITE,
    image: abs(seo.image),
    logo: abs('/favicon.png'),
    telephone: b.telephone,
    email: b.email,
    address: { '@type': 'PostalAddress', streetAddress: b.streetAddress, addressRegion: b.addressRegion, addressCountry: b.addressCountry },
    openingHoursSpecification: [{ '@type': 'OpeningHoursSpecification', dayOfWeek: b.openingDays, opens: b.opens, closes: b.closes }],
    ...(b.sameAs.length ? { sameAs: b.sameAs } : {}),
  };
};

/** Enlaces del contenido base (para que los buscadores descubran todas las secciones). */
const nav = seo.routes.map((r) => `<li><a href="${r.path}">${esc(r.h1)}</a></li>`).join('');

function page({ path, title, description, h1, body = '', type = 'website', image = seo.image, extraHead = '' }) {
  const head = [
    `<title data-prerender>${esc(title)}</title>`,
    `<meta name="description" content="${esc(description)}" data-prerender />`,
    `<link rel="canonical" href="${abs(path)}" data-prerender />`,
    `<meta property="og:type" content="${type}" data-prerender />`,
    `<meta property="og:site_name" content="${esc(seo.siteName)}" data-prerender />`,
    `<meta property="og:locale" content="${seo.locale}" data-prerender />`,
    `<meta property="og:url" content="${abs(path)}" data-prerender />`,
    `<meta property="og:title" content="${esc(title)}" data-prerender />`,
    `<meta property="og:description" content="${esc(description)}" data-prerender />`,
    `<meta property="og:image" content="${esc(abs(image))}" data-prerender />`,
    `<meta property="og:image:alt" content="${esc(seo.imageAlt)}" data-prerender />`,
    `<meta name="twitter:card" content="summary_large_image" data-prerender />`,
    extraHead,
  ].join('\n    ');

  const content = `<div id="root"><main style="max-width:960px;margin:0 auto;padding:48px 16px;font-family:Montserrat,system-ui,sans-serif"><h1>${esc(h1)}</h1><p>${esc(description)}</p>${body}<nav aria-label="Secciones"><ul>${nav}</ul></nav></main></div>`;

  return shell
    .replace(/<title[^>]*>[\s\S]*?<\/title>/, '')
    .replace(/<meta\s+name="description"[\s\S]*?\/>/, '')
    .replace('</head>', `    ${head}\n  </head>`)
    .replace('<div id="root"></div>', content);
}

async function write(path, html) {
  const file = path === '/' ? join(dist, 'index.html') : join(dist, `${path.slice(1)}.html`);
  await mkdir(dirname(file), { recursive: true });
  await writeFile(file, html, 'utf8');
}

/* 1. Shell neutro para las rutas sin HTML propio (carrito, perfil, panel…). */
await writeFile(join(dist, 'app.html'), shell, 'utf8');

/* 2. Páginas públicas. */
const today = new Date().toISOString().slice(0, 10);
const sitemap = [];
for (const r of seo.routes) {
  await write(r.path, page({ ...r, extraHead: r.path === '/' ? jsonLd(business()) : '' }));
  sitemap.push({ loc: abs(r.path), lastmod: today, changefreq: r.changefreq, priority: r.priority });
}

/* 3. Artículos del blog (opcional: requiere la API accesible al compilar). */
let posts = 0;
if (API) {
  try {
    const list = [];
    for (let pagina = 1; pagina <= 20; pagina++) {
      const res = await fetch(`${API}/api/blog?pagina=${pagina}&tamanoPagina=100`, { signal: AbortSignal.timeout(10000) });
      if (!res.ok) throw new Error(`GET /api/blog → ${res.status}`);
      const pageData = await res.json();
      list.push(...pageData.elementos);
      if (list.length >= pageData.total || pageData.elementos.length === 0) break;
    }
    for (const a of list) {
      const res = await fetch(`${API}/api/blog/${encodeURIComponent(a.slug)}`, { signal: AbortSignal.timeout(10000) });
      if (!res.ok) continue;
      const d = await res.json();
      // Texto plano del Markdown (sin HTML): párrafos para el contenido base.
      const text = String(d.contenidoMarkdown ?? '')
        .split(/\n{2,}/)
        .map((p) => p.replace(/[#>*_`[\]()!-]/g, ' ').replace(/\s+/g, ' ').trim())
        .filter(Boolean)
        .slice(0, 12)
        .map((p) => `<p>${esc(p)}</p>`)
        .join('');
      const path = `/blog/${a.slug}`;
      const date = d.publicarEnUtc ?? today;
      await write(
        path,
        page({
          path,
          title: `${d.titulo} · Blog PCP Shooting`,
          description: d.resumen,
          h1: d.titulo,
          body: text,
          type: 'article',
          image: d.imagenUrl || seo.image,
          extraHead: jsonLd({
            '@context': 'https://schema.org',
            '@type': 'BlogPosting',
            headline: d.titulo,
            description: d.resumen,
            datePublished: date,
            image: [d.imagenUrl || abs(seo.image)],
            mainEntityOfPage: abs(path),
            publisher: { '@type': 'Organization', name: seo.siteName, logo: { '@type': 'ImageObject', url: abs('/favicon.png') } },
          }),
        }),
      );
      sitemap.push({ loc: abs(path), lastmod: String(date).slice(0, 10), changefreq: 'monthly', priority: 0.6 });
      posts++;
    }
  } catch (e) {
    console.warn(`[seo] No se incluyeron artículos del blog: ${e.message}`);
  }
}

/* 4. sitemap.xml y robots.txt */
const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${sitemap
  .map((u) => `  <url><loc>${esc(u.loc)}</loc><lastmod>${u.lastmod}</lastmod><changefreq>${u.changefreq}</changefreq><priority>${Number(u.priority).toFixed(1)}</priority></url>`)
  .join('\n')}
</urlset>
`;
await writeFile(join(dist, 'sitemap.xml'), xml, 'utf8');

const robots = `User-agent: *
Allow: /
${seo.noindex.map((p) => `Disallow: ${p}`).join('\n')}

Sitemap: ${SITE}/sitemap.xml
`;
await writeFile(join(dist, 'robots.txt'), robots, 'utf8');

console.log(`[seo] ${seo.routes.length} páginas, ${posts} artículos, sitemap y robots generados para ${SITE}`);
