# PCP Shooting — Contexto del proyecto (frontend)

Este archivo resume todo lo decidido hasta ahora para que cualquier sesión de Claude (o cualquier desarrollador) arranque con el contexto completo. **Léelo antes de escribir código.**

## 1. El proyecto

- **Cliente:** PCP Shooting, "el primer club de tiro exclusivo para armas de aire comprimido en México". Km 23 Ruta de los Cenotes, Quintana Roo. Lunes a sábado 9:00–17:00. Tel. 998 159 6699 · contacto@pcpshooting.com. Sitio actual en WordPress: pcpshooting.com (se reemplaza).
- **Qué se construye:** plataforma web integral con sitio público responsivo, cuentas de usuario, reservación de líneas de tiro con pago, tienda de ropa con carrito, eventos, membresías **CARTAQ**, alimentos y bebidas, inventario y panel administrativo con reportes.
- **Cotización aceptada:** `docs/pcp-handoff/docs/Cotizacion_PCP_Shooting.pdf` (folio COT-PCP-2026-001). Es la fuente de verdad del **alcance**: cada módulo, qué incluye y qué NO incluye.
- **Meta de fecha:** primera versión funcional antes de enero de 2027.

## 2. Equipo y reparto

| Parte | Responsable | Tecnología |
|---|---|---|
| Frontend: sitio público + panel admin | Nosotros (este repo) | **React + TypeScript + React Router** |
| Backend / API REST | Otro desarrollador | **ASP.NET Core (C#)**, Entity Framework Core / Dapper |
| Base de datos, auth y archivos | Compartido | **Supabase** (PostgreSQL, Supabase Auth, Supabase Storage) |
| Pagos | Backend integra, frontend redirige | **Mercado Pago** (Checkout Pro / Bricks) |
| Hosting | — | Frontend en **Vercel**, API en **Azure App Service** |

> La cotización menciona Next.js; el equipo decidió **React con React Router** (por ejemplo, Vite + React Router). La cotización permite ajustar tecnologías.

**Regla de oro:** el frontend **no** habla directo con la base de datos para reglas de negocio. Todo pasa por la API C#. Supabase Auth da el JWT y el frontend lo manda en `Authorization: Bearer <token>` a la API.

## 3. Diseños de referencia (carpeta `docs/pcp-handoff/design/`)

> Todas las rutas de esta tabla son relativas a `docs/pcp-handoff/`.

| Carpeta | Qué hay | Cómo usarlo |
|---|---|---|
| `design/prototipos/` | 3 HTML navegables: `sitio-web.html`, `panel-admin.html`, `version-celular.html` | Ábrelos en el navegador para ver el flujo real (clics, pasos, carrito). |
| `design/capturas/escritorio/` | PNG de cada pantalla pública (1440 px) | Referencia visual para maquetar. |
| `design/capturas/admin/` | PNG de las 8 pantallas del panel | Ídem. |
| `design/capturas/movil/` | PNG de cada pantalla en celular (390 px), sitio y admin | Ídem: versión responsiva. |
| `design/fuente-dc/` | Código fuente de cada pantalla (`*.dc.html`) | Tiene el **HTML y los estilos exactos** (colores, tamaños, espaciados). Usa `{{variable}}`, `<sc-for>` (repetir) y `<sc-if>` (condición); cada archivo trae al final una clase `Component` con los datos de ejemplo. Tradúcelo a componentes React. |
| `design/pdf/` | Los mismos diseños en PDF con explicación por pantalla | Para presentar o consultar rápido. |
| `assets/` | Logo oficial en PNG transparente: color y blanco | Usa el blanco sobre negro (header, footer), el de color sobre blanco. |
| `docs/Manual_identidad_PCP.pdf` | Manual de marca | Colores y tipografía oficiales. |

Archivos fuente por pantalla: `Main` = tienda de ropa, `Catalogo`, `Blog`, `Contacto`, `Login`, `Reservas`, `Eventos`, `Membresia`, `Perfil`, `Nosotros`, `Galeria`, `Menu` (menú lateral). Admin: `AdminInicio`, `AdminReservas`, `AdminPedidos`, `AdminProductos`, `AdminEventos`, `AdminClientes`, `AdminContenido`, `AdminConfig`. Celular: `MReserva1`–`MReserva5`, `MTienda`, `MCarrito`, `MCheckout`, `MPedidoOk`, `MCatalogo`, `MEventos`, `MMembresia`, `MLogin`, `MRegistro`, `MPerfil`, `MNosotros`, `MGaleria`, `MBlog`, `MContacto`, `MAdmin*`.

**Ojo:** los diseños de celular son la misma app adaptada. En React se construye **un solo componente responsivo por pantalla**, no dos versiones.

## 4. Sistema de diseño

**Tipografía:** Montserrat (Google Fonts), pesos 400, 500, 600, 700, 800. Títulos en MAYÚSCULAS, peso 700.

**Colores**

| Token | Hex | Uso |
|---|---|---|
| `black` | `#000000` | Header, footer, hero, sidebar admin, textos |
| `bg` | `#F7F7F7` | Fondo de páginas públicas (`#F2F2F2` en admin) |
| `surface` | `#FFFFFF` | Tarjetas |
| `border` | `#E3E3E3` | Bordes de tarjetas; inputs `#B9B9BA` |
| `muted` | `#424242` | Texto secundario sobre claro |
| `muted-dark` | `#B9B9BA` | Texto secundario sobre negro |
| `primary` | `#D30000` | Botones principales (texto blanco), selección activa |
| `accent` | `#FF0000` | Detalles sobre fondo negro (eyebrows, íconos activos) |
| `link` | `#940000` | Enlaces y eyebrows sobre fondo claro |
| `primary-soft` | `#FFE5E5` / `#FFF5F5` | Fondos de selección, íconos |
| `gold` | `#C4992F` / `#F9F293` | Solo membresía "Socio Oro" |

**Estados (chips):** Pagado `#E8F0FE/#1E3A8A` · Preparando `#FFF4E5/#7A4100` · Listo para recoger, Llegó, Firmada, Activo `#E7F5EC/#14532D` · Entregado, Borrador `#EFEFEF/#333` · Pendiente, Vencida `#FFE5E5/#940000`.

**Medidas:** radios 10–16 px (tarjetas 16, botones 10–12, chips 999). Botones de 44 a 52 px de alto. Áreas táctiles de 44 px como mínimo. Contenedor máximo de 1280 px con padding de 32 px (16 px en celular).

**Componentes base a crear:** `Header` (logo, nav, cuenta, carrito, botón "Reservar línea"), `MobileTopBar`, `BottomTabBar` (Reservar, Eventos, Tienda, Socios, Perfil), `SideMenu` (drawer), `Footer`, `Button` (primary, secondary, dark, ghost, gold), `Chip/StatusBadge`, `Card`, `Input/Select/Textarea` con label, `StepBar`, `LineMap` (mapa de 15 líneas), `ProductCard`, `CartDrawer`, `KpiCard`, `AdminLayout` (sidebar + topbar), `DataTable`, `EmptyState`.

## 5. Rutas propuestas (React Router)

**Públicas**

| Ruta | Pantalla (archivo de diseño) |
|---|---|
| `/` | Inicio (no hay diseño propio: usar Nosotros/hero + accesos a Reservar, Tienda, Eventos) |
| `/nosotros` | `Nosotros` (incluye modos y actividades) |
| `/catalogo` | `Catalogo` (armas, informativo, botón cotizar) |
| `/reservar` | `Reservas` (wizard 4 pasos + confirmación); en celular `MReserva1–5` |
| `/reservar/confirmacion/:id` | Paso 5 (código QR) |
| `/tienda` | `Main` (tienda de ropa) |
| `/tienda/carrito` | Carrito (drawer en escritorio, página en celular) |
| `/tienda/checkout` | Finalizar compra |
| `/tienda/pedido/:id` | Pedido confirmado |
| `/alimentos` | **Sin diseño todavía**: carta de alimentos y bebidas (ver sección 7) |
| `/eventos` | `Eventos` |
| `/membresias` | `Membresia` (planes CARTAQ + inscripción) |
| `/galeria` | `Galeria` |
| `/blog` y `/blog/:slug` | `Blog` |
| `/contacto` | `Contacto` |
| `/login`, `/registro`, `/recuperar` | `Login` (pestañas) |
| `/perfil` | `Perfil` (protegida) |

**Panel administrativo** (protegido por rol; layout `AdminLayout`)

| Ruta | Pantalla |
|---|---|
| `/admin` | `AdminInicio` (dashboard) |
| `/admin/reservas` | `AdminReservas` (calendario 15 líneas × horario, validar QR, registrar llegada) |
| `/admin/pedidos` | `AdminPedidos` (estados + WhatsApp) |
| `/admin/productos` | `AdminProductos` (tienda; pestaña catálogo de armas) |
| `/admin/paquetes` | **Sin diseño**: paquetes/actividades de tiro (precio, condiciones, consumibles) |
| `/admin/alimentos` | **Sin diseño**: carta, pedidos y ventas de alimentos y bebidas |
| `/admin/inventario` | **Sin diseño**: existencias, entradas/salidas, mínimos, historial |
| `/admin/eventos` | `AdminEventos` |
| `/admin/clientes` | `AdminClientes` (socios CARTAQ, cartas responsivas) |
| `/admin/reportes` | **Sin diseño**: 6 categorías de reportes con filtros y CSV |
| `/admin/contenido` | `AdminContenido` (blog, galería) |
| `/admin/configuracion` | `AdminConfig` (horarios, líneas, staff y roles, plantillas WhatsApp) |

Roles: `cliente`, `recepcion` (reservas, llegadas, pedidos, alimentos), `admin` (todo).

## 6. Reglas de negocio acordadas en la cotización (respetarlas aunque el diseño diga otra cosa)

- **Paquetes por actividad o tirada, NO por duración.** El diseño muestra "1 hora" y "Duración de cada turno": quitar la duración y mostrar lo que incluye el paquete.
- El cliente elige **paquete → fecha → horario → una de las 15 líneas**. La API valida disponibilidad y evita duplicados. No hay asignación automática de líneas.
- **Cobro anticipado del 100%** del paquete. La reserva solo se confirma cuando la API valida el pago (webhook de Mercado Pago). Estados: `pendiente_pago`, `confirmada`, `utilizada`, `cancelada`.
- Cada reservación genera un **código QR**. El staff lo valida y registra la llegada; al marcarla como utilizada se descuentan los consumibles del inventario.
- **Tienda: solo recolección en el club. NO hay envío a domicilio.** El diseño de checkout trae la opción de envío y el costo de $150: **quitarlos**.
- **Membresías CARTAQ:** internas, pago único por periodo y **renovación manual** (nuevo pago). Sin cobros recurrentes. En el diseño los planes se llaman Tirador, Socio y Socio Oro: los nombres reales están pendientes.
- **Alimentos y bebidas:** carta digital, pedido desde el sitio, estados `recibido → en_preparacion → listo → entregado`, consumo o recolección en el club. **El método de pago está pendiente** (en línea o en el club).
- **Inventario:** productos, alimentos, bebidas y consumibles; entradas, salidas, ajustes, mínimos, alertas, historial; consumo configurable por paquete.
- **Pagos:** la plataforma **no guarda datos de tarjeta**. Sin pagos parciales ni recurrentes.
- **Carta responsiva:** el diseño trae firma en pantalla. El mecanismo legal está pendiente (sección 8).
- **Fuera de alcance:** apps nativas, CFDI automático, envíos, terminales físicas, firma electrónica certificada, multialmacén y reportes extra.

## 7. Pantallas que faltan por diseñar

Siguen el mismo sistema de diseño de la sección 4:

1. `/alimentos`: carta de alimentos y bebidas con categorías y carrito propio o pedido rápido.
2. `/admin/alimentos`: pedidos entrantes por estado y registro de venta en mostrador.
3. `/admin/inventario`: tabla de existencias con alertas, movimientos y ajuste manual.
4. `/admin/paquetes`: CRUD de paquetes con consumibles asociados.
5. `/admin/reportes`: filtros por fecha, KPIs, tablas, gráficas básicas y exportar a CSV.
6. `/` Inicio.

## 8. Definiciones pendientes con el cliente (no inventar; dejar configurable)

- Intervalos entre horarios y cuándo se libera una línea.
- Políticas de cancelación, reprogramación y ausencia.
- Consumibles exactos por paquete.
- Alimentos y bebidas: si se pagan en línea o en el club.
- Carta responsiva: firma, validación, almacenamiento y requisitos legales.
- Membresías CARTAQ: planes, vigencias, beneficios y renovación.
- Precios reales, textos y fotos (el cliente los entrega). Los prototipos usan precios y nombres **de ejemplo**: no son datos reales.

## 9. Contrato con la API (coordinarlo con el desarrollador del backend)

Propuesta de recursos REST; confirmar nombres y formas con él antes de consumir:

```
Auth: Supabase Auth (JWT) → header Authorization en cada llamada a la API
GET    /api/paquetes                        · catálogo de paquetes/actividades
GET    /api/disponibilidad?paqueteId&fecha  · horarios y estado de las 15 líneas
POST   /api/reservas                        · crea reserva pendiente_pago → devuelve preferencia de pago
GET    /api/reservas/mias | /api/reservas/{id}
POST   /api/reservas/{id}/checkin           · (staff) valida QR y marca utilizada
GET    /api/productos?categoria             · tienda de ropa
POST   /api/pedidos                         · crea pedido desde el carrito → preferencia de pago
GET    /api/pedidos/mios | PATCH /api/pedidos/{id}/estado (staff)
GET    /api/alimentos  · POST /api/alimentos/pedidos · PATCH estado
GET    /api/eventos    · POST /api/eventos/{id}/inscripciones
GET    /api/membresias/planes · POST /api/membresias (contratar/renovar)
POST   /api/pagos/webhook/mercadopago       · (backend) confirma pagos
GET    /api/inventario · POST /api/inventario/movimientos
GET    /api/reportes/{categoria}?desde&hasta[&formato=csv]
CRUD   /api/admin/... (productos, paquetes, eventos, contenido, usuarios, configuración)
```

Recomendación: pedirle al backend su **Swagger/OpenAPI** (la cotización lo incluye) y generar los tipos de TypeScript desde ahí (`openapi-typescript`). Mientras la API no exista, usar datos simulados (MSW o JSON locales) con la misma forma.

## 10. Estructura sugerida del frontend

```
src/
  app/            router.tsx, providers (auth, query client)
  layouts/        PublicLayout, AdminLayout
  pages/          una carpeta por ruta (public/*, admin/*)
  components/     ui/ (Button, Card, Chip, Input…), layout/ (Header, Footer, TabBar, SideMenu)
  features/       reservas/, tienda/, carrito/, eventos/, membresias/, alimentos/, inventario/, reportes/
  lib/            api.ts (fetch con JWT), supabase.ts, format.ts (moneda MXN, fechas es-MX)
  styles/         tokens.css (variables de la sección 4)
```

**Decisiones tomadas (paso 1):**
- El frontend vive en `PCP-ShootingWeb/` (Vite 7 + React 19 + React Router 7). Comandos: `npm run dev`, `npm run build`.
- **No tocar `PCP-Shooting/` (backend C#)**: lo mantiene otro desarrollador. Lo que el frontend necesite de la API se le pide a él.
- Estilos con **CSS Modules + variables CSS** (`src/styles/tokens.css`). Nada de colores ni medidas sueltas: usar los tokens.
- Montserrat desde Google Fonts (400–800) en `index.html`. Alias `@/` → `src/`.
- Cortes responsivos (cada pantalla debe verse bien en los 3): celular `< 768px` (barra superior compacta + pestañas inferiores), tableta `768–1023px` (header sin navegación central, menú lateral), escritorio `≥ 1024px`.
- Navegación centralizada en `src/app/navigation.ts`; rutas en `src/app/router.tsx` con carga diferida por página.
- `/api` se redirige por proxy a `http://localhost:5026` en desarrollo (sin CORS).
- **Auth con Supabase (conectado).** Credenciales en `PCP-ShootingWeb/.env.local` (`VITE_SUPABASE_URL`, `VITE_SUPABASE_PUBLISHABLE_KEY`; plantilla en `.env.example`). `src/features/auth/authService.ts` elige: Supabase (`authSupabase.ts`) si hay credenciales válidas, si no el modo demo (`authDemo.ts`). Cliente en `src/lib/supabase.ts` (flujo PKCE, "mantener sesión" = localStorage vs sessionStorage, bloquea claves secretas). Reglas de seguridad:
  - Supabase solo da identidad. El **rol sale de la API** (`GET /api/perfiles/yo`; 0 = Cliente, 1 = Administrador), nunca de `user_metadata`. Si la API no responde, el usuario queda como cliente (mínimo privilegio). El primer acceso crea el perfil con `PUT /api/perfiles/yo`.
  - `src/lib/api.ts` envía `Authorization: Bearer <token>` en cada llamada; la API revalida el rol en cada petición.
  - Mensajes genéricos en registro/recuperación (no revelan qué correos existen). Contraseña: 8–72, letras y números.
  - Flujos: login, registro con confirmación de correo (`/login?confirmado=1`), reenviar confirmación, recuperar (`/recuperar`) → `/restablecer`, cambio de correo con confirmación, cierre de sesión local, sincronización entre pestañas (`onAuthStateChange`). Google oculto hasta `VITE_AUTH_GOOGLE=true`.
  - Cabeceras de seguridad (CSP, HSTS, X-Frame-Options, Permissions-Policy) en `vercel.json`.
  - Base de datos: `supabase/seguridad/01_rls_bloquear_acceso_publico.sql` (RLS + revocar anon/authenticated) y lista de verificación del panel en `supabase/README.md`.
  - `RequireAuth`/`RequireRole` (`src/app/auth.tsx`) protegen `/perfil` y `/admin/*`. Pendiente backend: rol **Recepción**.

**Componentes base (paso 2)** en `src/components/ui/` (importar desde `@/components/ui`): `Button`/`ButtonLink`/`ButtonAnchor`, `Card`/`CardHeader`, `Chip`/`ChipGroup`, `StatusBadge` (mapa `STATUS` con los estados de la API), `TextField`/`SelectField`/`TextareaField`/`Checkbox`/`Switch`, `StepBar`, `LineMap`, `Photo` (marcador rayado si no hay foto), `ProductCard`/`ProductGrid`, `QuantityStepper`, `CartDrawer`, `KpiCard`/`KpiGrid`, `DataTable` (en celular las filas se vuelven tarjetas), `EmptyState`, `PageHero`. Layout en `src/components/layout/`. Catálogo visual en `/dev/componentes` (solo en `npm run dev`). Formato de moneda: `formatMXN` en `src/lib/format.ts`.

**Reservas (paso 5):** wizard en `/reservar` (actividad → fecha/hora/línea → datos + carta con firma → pago) y confirmación con QR en `/reservar/confirmacion/:id`. Todo en `src/features/reservas/`: `api.ts` simula `GET /api/paquetes`, `GET /api/disponibilidad`, `POST /api/reservas` y el webhook de Mercado Pago; `config.ts` concentra lo pendiente de definir (horarios, personas por línea, firma). Reglas aplicadas: sin duración en paquetes; el pago NO captura tarjeta (redirige a Mercado Pago); la reserva nace `pendiente_pago` y se confirma con el pago. El borrador se guarda en sessionStorage.

**Tienda (paso 6):** `/tienda` (filtros, búsqueda y orden en la URL), carrito global `src/features/tienda/CartProvider.tsx` (localStorage, se sincroniza entre pestañas), panel lateral en tableta/escritorio y página `/tienda/carrito` en celular, `/tienda/checkout` y `/tienda/pedido/:id`. `src/features/tienda/api.ts` simula `GET /api/productos`, `POST /api/pedidos` y el pago. Reglas: solo recolección en el club (se quitó envío y $150) y pago en Mercado Pago (sin campos de tarjeta; componente compartido `src/features/pagos/PaymentMethods.tsx`).

**Eventos, membresías y alimentos (paso 7):**
- `/eventos`: filtros por tipo en la URL, cupo restante y diálogo de inscripción con pago (requiere cuenta). `src/features/eventos/api.ts` simula `GET /api/eventos` y `POST /api/eventos/{id}/inscripciones`.
- `/membresias`: planes CARTAQ con periodo Mensual/Anual mostrado como **pago único por periodo, renovación manual, sin cargos automáticos**; inscripción con identificación oficial (requiere cuenta) y aviso de renovación si ya es socio. Planes en `src/features/membresias/plans.ts` (nombres/precios de EJEMPLO, pendientes). En demo, `activateMembership` de `authService` activa la credencial.
- `/alimentos` (sin diseño): carta, pedido a la línea de tiro o en barra, seguimiento recibido → en preparación → listo → entregado. `FOOD_PAYMENT_MODE` en `src/features/alimentos/api.ts` (por defecto `en_club`) porque el método de pago está pendiente.

**Panel administrativo (paso 8):** las 12 pantallas de `/admin` funcionan con un almacén de DEMO compartido (`src/features/admin/store.ts` + `seed.ts`, guardado en localStorage; se restaura desde Configuración). Las acciones se reflejan entre pantallas: el **check-in** (código o QR con la cámara vía BarcodeDetector) marca la reserva `utilizada` y descuenta del inventario los consumibles del paquete por persona. Incluye: calendario 15 líneas × horario (lista + minimapa en celular), nueva reserva en mostrador (sin pago queda `pendiente_pago`), bloqueo de líneas; pedidos con cambio de estado y WhatsApp; productos con existencias por talla y catálogo de armas; eventos con cupo e inscritos; clientes con renovación manual de membresía; paquetes con consumibles (sin duración); inventario con movimientos, mínimos y alertas; reportes de 6 categorías con fechas, gráfica, tabla y CSV; contenido (blog, galería, textos, avisos) y configuración (horario, intervalo, líneas activas, staff, plantillas). Piezas compartidas en `src/features/admin/ui.tsx`. Al integrar la API, cada `update(...)` se sustituye por su endpoint `/api/admin/...`.

**Integración con la API (paso 9, en curso):**
- Cliente HTTP en `src/lib/api.ts` (`apiGet`, `apiGetAll` que recorre páginas de 100, errores ProblemDetails → `ApiError` en español). Respuestas camelCase; enums numéricos; paginación `{ elementos, total, pagina, tamanoPagina }`.
- Interruptor `VITE_DATA_SOURCE=api|demo` (ver `.env.example`; por defecto `demo`). En desarrollo `/api` va por proxy a `API_PROXY_TARGET` (por defecto `http://localhost:5026`).
- **Conectado:** catálogo de la tienda → `GET /api/productos?esVendible=true` (público). Se excluyen categoría 1 (Alimento) y 3 (ReplicaDecorativa); 2 = Playera, 0 = otros artículos. El carrito guarda en caché los productos de la API (`pcp.catalogo.api`).
- **Pendiente (requiere login Supabase):** pedidos, membresías, perfiles, inventario y administración ya existen en la API pero exigen JWT; no se conectan hasta el paso 4b. Aún no hay endpoints de reservas, paquetes, disponibilidad, eventos ni alimentos (pedidos).

**Páginas informativas (paso 3):** Nosotros, Catálogo, Galería (con visor), Blog (filtros y búsqueda en la URL, `/blog/:slug`) y Contacto. Los contenidos de ejemplo viven en `src/data/` (sustituir por API/datos del cliente). Formularios con React Hook Form + Zod (`src/features/contacto/`). Envíos simulados hasta que el backend publique `POST /api/contacto` y `POST /api/suscripciones` (propuesta pendiente de confirmar con él). Precios del catálogo de armas: `null` = "Por cotizar" hasta que el cliente los entregue.

Librerías sugeridas: React Router, TanStack Query (datos de la API), React Hook Form + Zod (formularios), Supabase JS (solo auth y subida de archivos), Tailwind o CSS Modules con los tokens. El carrito vive en un estado global (Zustand o Context) persistido en localStorage.

## 11. Orden de trabajo sugerido

1. Configurar el proyecto, los tokens de diseño, Montserrat, el layout público y el router con todas las rutas en blanco.
2. Componentes base de la sección 4.
3. Páginas informativas: nosotros, catálogo, galería, blog, contacto.
4. Auth con Supabase: login, registro, perfil y rutas protegidas.
5. Wizard de reservas con mapa de líneas (con datos simulados y luego la API).
6. Tienda, carrito y checkout (solo recolección).
7. Eventos y membresías; alimentos y bebidas.
8. Panel admin: layout, reservas con check-in QR, pedidos y productos; después inventario, paquetes, reportes, contenido y configuración.
9. Integración real con la API y Mercado Pago, pruebas en celular, publicación en Vercel.
