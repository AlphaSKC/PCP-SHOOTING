# Roadmap compartido · PCP Shooting

Última actualización: **23 de noviembre de 2026**.

Este documento reúne el estado actual y el orden de trabajo del equipo. Las guías de cada módulo explican los contratos de la API. Una pantalla con datos de demostración no significa que su integración esté terminada.

**R04.2 implementado y migración aplicada en Supabase: reserva de mostrador y consulta administrativa. Sus tres pruebas HTTP ya pasan. R05 está en curso en backend; contrato y migración aplicada descritos en `docs/MERCADO-PAGO.md`. R03.3 mantiene su verificación parcial.**

> **R05 backend (23 nov):** checkout, webhook y conciliación manual implementados; ver [MERCADO-PAGO.md](MERCADO-PAGO.md). Checkout devuelve `checkoutUrl` para redirección o QR. Migración aplicada en Supabase el 22 nov. Pendientes: credenciales de prueba, endpoint HTTPS, integración frontend, pruebas sandbox y conciliación programada.

**En paralelo, frontend puede conectar configuración operativa, paquetes, blog, inventario, historial de clientes y reportes a sus endpoints existentes.** Coordinar cada conexión antes de cambiar el contrato.

## Cómo mantenerlo

- Backend: actualizar el estado de la API, migraciones, pruebas y documentación al cerrar cada bloque.
- Frontend: actualizar su columna cuando sustituya la simulación por llamadas reales y compruebe el flujo.
- Marcar un bloque como **integrado** solo cuando backend y frontend se hayan probado juntos.
- Si cambia una regla con el cliente, registrar aquí la decisión y su fecha, y actualizar la guía afectada.
- No marcar como resuelto algo que todavía dependa de credenciales, contenido o una respuesta del cliente.
- No fijar fechas de entrega sin acordarlas entre el equipo.

Estados: **Terminado**, **En curso**, **Parcial**, **Pendiente**, **Por confirmar** y **Pospuesto**. El alcance de cada estado se indica en la tabla; no implica que todo el sitio esté listo para producción.

## 1. Base disponible

| Módulo | Backend | Frontend / integración |
| --- | --- | --- |
| Supabase Auth, login, registro, recuperación, perfiles y roles | Terminado; validación JWT, roles Cliente/Recepción/Administrador y administrador inicial | El flujo de autenticación fue comprobado por el equipo. |
| Catálogo de alimentos, playeras y decorativos no funcionales | Terminado para gestión básica, categoría, imagen y notas Markdown | Catálogo de tienda conectado cuando `VITE_DATA_SOURCE=api`. **Probado (21 nov):** `/admin/productos` conectado para playeras y artículos (categorías 2 y 0): listar, crear, editar, foto, visible/oculto y entradas de inventario; probado con sesión de Administrador y reflejado en `/tienda`. Sin tallas en la API. **En curso (21 nov):** catálogo de armas conectado a la categoría 3 (decisión del backend): panel Productos → Catálogo de armas y página pública `/catalogo`; no vendible, precio vacío = «Por cotizar», datos del rifle en `especificacionesTecnicas` (`tipo`, `calibres`, `modoDisparo`, `destaca`, `resumen`, `ficha`). Pendiente prueba con Administrador. |
| Imágenes de productos | Terminado el endpoint de carga y la relación de múltiples imágenes | Pendiente primera carga completa contra Storage con una sesión real y conexión del editor/galería de producto. |
| Pedidos de clientes y pagos manuales | Terminado el flujo básico: crear, consultar, registrar/confirmar pago, cancelar pendientes y recoger | **Probado (21 nov):** checkout crea el pedido real (`POST /api/pedidos`, requiere sesión, idOperacion estable en reintentos); página del pedido, «Mis pedidos» en perfil y cancelación por el cliente. `/admin/pedidos` conectado: lista, detalle, clientes (`GET /api/perfiles`, solo Administrador), pago manual (registrar + confirmar), recogida y cancelación. Sin Mercado Pago (R05): el pedido queda «Pendiente de pago» y el club registra el pago. Estados del diseño «Preparando»/«Listo para recoger» no existen en la API. Probado de punta a punta con cliente y Administrador. |
| Inventario | Terminado: movimientos, existencias, mínimos, alertas, búsqueda y paginación; categoría interna Consumible (4), no vendible y oculta en tienda | **Probado (21 nov):** `/admin/inventario` conectado: existencias de tienda, alimentos y consumibles con alertas (bajo mínimo/agotado/inactivo), filtros y búsqueda, entradas/salidas/ajustes con UUID estable, mínimo por artículo, activar/desactivar y crear consumible; historial por artículo. Probado con Administrador. |
| Membresías CARTAQ | Parcial: registro de vigencias y renovación con pago completo confirmado | Falta cerrar planes, precios, beneficios y renovación definitiva. Cobro recurrente no implementado. **Frontend (21 nov):** el perfil del cliente lee `GET /api/membresias/vigente`; en `/admin/clientes` se registra/renueva una vigencia eligiendo un pago confirmado del cliente (ver F12). |
| Historial del cliente | Terminado para perfil, pedidos, pagos y membresías | **Probado (21 nov):** `/admin/clientes` conectado: lista de clientes (`GET /api/perfiles?rol=Cliente`), ficha con historial (pedidos, pagos, membresías), activar/desactivar cuenta y WhatsApp. Sin cartas responsivas, correo ni última visita (no existen en la API). Probado con Administrador. |
| Reportes CSV/JSON | Terminado para pedidos, pagos, membresías e inventario | **Probado (21 nov):** `/admin/reportes` conectado con datos reales: ventas de tienda (pedidos), ingresos (pagos confirmados), membresías (con socios vigentes hoy) e inventario; rango de fechas en días del club, KPIs, gráfica diaria, tabla con nombre del cliente y CSV generado por la API. Reservas, alimentos y eventos se muestran como «pendiente» (sin datos de ejemplo). Probado con Administrador. |
| Blog | Terminado: borradores, programación, publicación, archivo, filtros y auditoría | **Probado (21 nov):** conectado. Panel `/admin/contenido` → pestaña Blog: crear/editar con `version`, slug, resumen, categoría, portada por URL, destacado, Markdown con barra de formato y vista previa, publicar ahora, programar (hora del club), despublicar y archivar. Público `/blog` y `/blog/:slug` leen la API y renderizan Markdown de forma segura (`react-markdown`, sin HTML crudo). Galería, textos y avisos muestran «pendiente». Probado con Administrador. |
| Configuración operativa, líneas y bloqueos | Terminado el punto R01; migración aplicada en Supabase | **Probado (21 nov):** `/admin/configuracion` conectado: horario semanal con primer/último turno, reglas (actividad, margen, apartado, anticipación, llegada/tolerancia) con `version`, 15 líneas (nombre/activa) y bloqueos (crear/quitar, hora del club). Staff real: lista Administrador/Recepción, dar acceso a un cliente registrado y quitarlo (`PATCH /api/perfiles/{id}/acceso`). Plantillas de WhatsApp/correo de avisos siguen locales (sin API). Probado con Administrador. |
| Paquetes y consumibles | Terminado R02: gestión por persona, precios general/CARTAQ opcional, composición, versiones, permisos y catálogo público | **Probado (21 nov):** `/admin/paquetes` conectado: lista con estado (visible al reservar / activo oculto / inactivo), crear y editar con `version`, código, incluye/requisitos (Markdown), precio general y CARTAQ (vacío = pendiente), activar/desactivar y composición de consumibles por persona; crear consumible interno (categoría 4) desde el editor. Recepción solo lectura. Sin «renta de rifle» (no existe en la API). Falta cargar el catálogo real del cliente y conectar `/reservar` (siguiente bloque). Probado con Administrador. |
| Consulta de disponibilidad | Terminado R03.1 y ampliado en R03.2: turnos, mapa, bloqueos, anticipación y apartados vigentes | **Probado (21 nov):** `/reservar` consulta los próximos 8 días (`GET /api/disponibilidad` con `personas`) y muestra días, turnos y mapa de líneas con el motivo real de cada turno/línea (No disponible, Bloqueada, Inactiva, Ocupada). Probado con cliente. |
| Apartados temporales | Terminado R03.2: selección atómica de líneas, plazo configurable, idempotencia, privacidad y expiración lógica | **Probado (21 nov):** `/reservar` aparta N líneas (una por persona) con UUID estable en reintentos y contador de vencimiento; si vence o hay 409, recarga disponibilidad. Probado. |
| Galería del sitio | Pospuesto; no implementada en backend | Existe demostración. Retomar después por acuerdo del equipo. |

Última verificación del backend al cerrar R03.2: **291 pruebas aprobadas**, incluidas las pruebas PostgreSQL en esquemas temporales. Las migraciones de blog, configuración operativa, paquetes/consumibles y apartados están aplicadas en Supabase. Esta cifra es evidencia de ese cierre, no una sustitución de las pruebas de cambios posteriores.

## 2. Orden de trabajo de reservaciones

Los IDs R01–R08 son estables para usarlos en mensajes y tareas. No son los números del plan anterior de mejoras, cuyo punto 5 era blog/galería.

| ID | Bloque | Estado backend | Dependencias y criterio de cierre |
| --- | --- | --- | --- |
| R01 | Configuración operativa, horarios, líneas y bloqueos | **Terminado** | Endpoints con permisos, auditoría, versiones, valores iniciales y migración aplicada. Integración del panel pendiente. |
| R02 | Paquetes y consumibles | **Terminado — backend** | Gestión implementada, composición por persona, tarifa general/CARTAQ opcional, consumibles internos, permisos, auditoría y concurrencia. Migración aplicada. Falta integrar frontend y recibir/cargar el catálogo definitivo; no se inventaron paquetes reales. |
| R03 | Disponibilidad y apartados temporales | **Terminado - backend; concurrencia omitida en el cierre R03.3** | Consulta y apartados implementados. R05 agrega la confirmación de reservas y pagos; ver [MERCADO-PAGO.md](MERCADO-PAGO.md).
| R04 | Reservaciones web y de mostrador | **Parcial - R04.1 y R04.2 terminados en backend; integracion de `/admin/reservas` pendiente** | R04.2 agrega mostrador y consultas administrativas; migracion aplicada en Supabase. Confirmacion y ocupacion pagada dependen de R05. El wizard `/reservar` esta conectado a R04.1; `/admin/reservas` sigue en demostracion y falta conectarlo a la API. |
| R05 | Mercado Pago en entorno de pruebas | **En curso — checkout, webhook y conciliación manual implementados** | 17 pruebas HTTP simuladas y una prueba PostgreSQL de conciliación. Migración aplicada en Supabase el 22 nov; este cambio no requiere otra. Pendientes credenciales, HTTPS, frontend, sandbox y conciliación programada. Ver [MERCADO-PAGO.md](MERCADO-PAGO.md). |
| R06 | Responsivas, llegadas y ausencias | **Pendiente** | R04/R05 y texto/mecanismo de responsiva. QR de llegada, búsqueda manual, tutor, llegada, consumo único de inventario y estado No asistió conservando el cobro. |
| R07 | Avisos y solicitud de factura | **Pendiente** | R05/R06, correo correcto, proveedor de correo/WhatsApp y datos fiscales acordados. QR en perfil y avisos. Captura fiscal para facturación del club, sin emitir CFDI automáticamente. |
| R08 | Historial y reportes de reservas | **Pendiente** | R04–R06. Agregar reservas, llegadas y ausencias a historial/reportes existentes y probar con frontend. |

R03.2 ya crea apartados reales y libera su capacidad lógica al vencer, sin esperar a borrar registros. **Todavía no existen reservas confirmadas ni integración de pagos en ese flujo**. Eso corresponde a R04/R05.

### Entregas de R03

| ID | Entrega | Estado |
| --- | --- | --- |
| R03.1 | Consulta pública de turnos y mapa de líneas usando configuración, paquetes y bloqueos. | **Terminado — backend**. [Contrato y límites](DISPONIBILIDAD.md). |
| R03.2 | Crear apartados temporales con el plazo configurado y evitar empalmes entre clientes. | **Terminado — backend**. [Contrato](APARTADOS.md). Revalidación, grupos atómicos e idempotencia probados con solicitudes simultáneas. |
| R03.3 | Cierre de vencimientos, conservación/consolidación de registros y pruebas ampliadas de límites. | **Terminado — backend**. Historial paginado propio, filtros y corte UTC coherente; conservación sin borrado. Por indicación del usuario se omiten las pruebas de concurrencia de este cierre. |

La anticipación máxima se interpreta provisionalmente como una ventana móvil de 48 horas. La distinción entre 48 horas y dos fechas locales sigue pendiente del cliente. La consulta ya incluye apartados vigentes, pero todavía no comprueba stock ni incorpora reservas confirmadas.

### Entregas de R04

| ID | Entrega | Estado |
| --- | --- | --- |
| R04.1 | Reserva web desde apartado propio, participantes, copias de datos e historial propio. | **Terminado — backend**. [Contrato](RESERVAS-WEB.md). Tarifa general; beneficio CARTAQ por participante pendiente de definición. Sin confirmación de pago. |
| R04.2 | Reserva de mostrador para cliente registrado, actor empleado separado y horario inmediato. | **Terminado — backend; migración aplicada y tres pruebas HTTP aprobadas.** `POST /api/reservas/mostrador`; listado/detalle administrativo; permisos de Recepción/Administrador. Checkout QR queda en R05. |

### Entregable esperado de cada bloque

- Contrato documentado y ejemplos HTTP para frontend.
- Permisos y validaciones de negocio implementados.
- Migración, cuando corresponda, verificada antes de aplicarse.
- Pruebas relevantes aprobadas, incluidas concurrencia y PostgreSQL si afectan capacidad, pagos o inventario.
- Registro de qué quedó conectado en frontend y qué sigue pendiente.

## 3. Trabajo que frontend ya puede tomar

| Prioridad | Trabajo | Referencia |
| --- | --- | --- |
| 1 | Conectar configuración, horarios, líneas y bloqueos; respetar diferencia entre Administrador y Recepción. | [OPERACION.md](OPERACION.md) |
| 2 | Conectar gestión y catálogo de paquetes, composición y categoría interna de consumibles. | [PAQUETES.md](PAQUETES.md) |
| 3 | Conectar editor y consulta pública del blog; renderizar Markdown de forma segura. | [BLOG.md](BLOG.md) |
| 4 | Conectar inventario, historial de clientes, pedidos y gestión de productos a los contratos actuales. | Swagger y [ejemplos HTTP](../PCP-Shooting/PCP-Shooting/PCP-Shooting.http) |
| 5 | Conectar reportes existentes, sin presentar reservas/eventos de demostración como resultados reales. | [REPORTES.md](REPORTES.md) |
| 6 | Completar carga de imágenes con JWT real y verificar el resultado en el catálogo. | [IMAGENES-API.md](IMAGENES-API.md) |
| 7 | Conectar horarios/mapa y apartados propios con contador y UUID estable en reintentos. Mantener deshabilitada la confirmación/pago de reservas hasta R04/R05. | [DISPONIBILIDAD.md](DISPONIBILIDAD.md), [APARTADOS.md](APARTADOS.md) |

El equipo acuerda qué toma cada uno antes de modificar archivos compartidos. El panel guarda actualmente buena parte de sus acciones en un almacén de demostración local; debe reemplazarse módulo por módulo.

## 4. Reglas confirmadas para los próximos módulos

Estas reglas se recopilaron del cuestionario del cliente y las aclaraciones del equipo del **22 de noviembre de 2026**. Algunas son configuración ya persistida; otras son requisitos para módulos aún pendientes.

- Hora local del club: Cancún. Atención martes a domingo, 08:00–18:00; lunes cerrado.
- Primer inicio reservable 11:00, último 15:30. Actividad de 10 minutos más margen de 5: inicios cada 15 minutos. El paquete se vende por actividad/tirada, no como tiempo de renta.
- 15 líneas equivalentes, una persona por línea. El flujo de participantes y asignación de varias líneas se concretará en R03/R04.
- Apartado de pago de 10 minutos por defecto, modificable por Administrador. Cada apartado conservará el vencimiento que recibió al crearse.
- Reservas web con cuenta: mínimo una hora antes, máximo dos días antes. El corte exacto de los dos días —48 horas o límite por fecha local— debe acordarse antes de implementarlo.
- Precio por persona. Admiten precios temporales en cero durante desarrollo; falta el catálogo y no debe asumirse que cero habilita cobros reales o acceso gratuito en producción.
- Desde nueve años; menores con tutor. Responsiva por participante, con mecanismo y texto pendientes.
- Equipo propio únicamente para miembros CARTAQ; falta cerrar cómo verificar el beneficio y si cambia el precio.
- Sin cancelación/reprogramación ni devolución por decisión del cliente en el flujo ordinario. Los errores de cobro, pagos tardíos, duplicados y contracargos requieren tratamiento propio; no se consideran resueltos por esa regla.
- Llegada desde tres minutos antes y tolerancia de tres minutos. Al vencer sin llegada, marcar **No asistió**, conservar el pago y liberar la línea. No descontar consumibles si no se realizó la actividad.
- Al registrar llegada, marcar la reserva utilizada y descontar consumibles una sola vez. Validar pago y responsivas antes de admitir la llegada.
- Recepción y Administrador pueden generar reservas de mostrador para ese momento si hay disponibilidad; excepción al mínimo de una hora.
- Cobro de reservas de mostrador mediante Mercado Pago: QR con enlace al checkout como opción principal; abrir el mismo checkout en el dispositivo del club como alternativa. Sin registro de efectivo ni terminal externa en ese flujo.
- No almacenar datos bancarios ni de tarjeta. QR de pago y QR de llegada son distintos; escanear un QR de pago no confirma la reserva.
- Tarjeta de débito/crédito y saldo de Mercado Pago. Solo pagos inmediatos para reservas, sin OXXO.
- Tienda con recogida local, sin envíos. La reserva se confirma con pago completo verificado por backend, nunca solo por la pantalla de retorno.

## 5. Definiciones y accesos pendientes

| Tema | Qué falta | Trabajo afectado |
| --- | --- | --- |
| Paquetes | Nombres, precios, tarifas CARTAQ, tiros, blancos y demás consumibles/cantidades. | R02, R03, R06 |
| Paquetes/tiros adicionales | Confirmar si se compran en línea, en el club o ambos, y cómo afectan la ocupación de la línea. | R02–R04 |
| Anticipación máxima | Precisar si dos días son 48 horas o dos fechas del calendario local. | R03/R04 |
| Pago fuera de plazo | Regla acordada (22 nov): los 10 minutos limitan el inicio del checkout. Si se inicia antes de vencer, el backend mantiene la línea ocupada mientras Mercado Pago procesa; aprobación tardía confirma la reserva. Rechazo/cancelación libera la protección. | R05 |
| Cuenta de Mercado Pago | El cliente indicó que debe abrirla. Identificar al responsable que dará acceso a la aplicación/credenciales (pregunta 4.2). | R05 y producción |
| Pruebas de Mercado Pago | Crear/configurar la aplicación, entorno de pruebas, tarjetas/cuentas necesarias y URL HTTPS de notificaciones. | R05 |
| Reserva de mostrador | Resuelto: cliente registrado obligatoriamente, por sí mismo o con ayuda de Recepción; se selecciona su perfil activo, sin usar la cuenta del empleado. | R04 |
| Responsivas | Texto, firma, conservación y representación del menor por el tutor. | R06 |
| Correo de avisos | El recibido tiene dos arrobas: confirmar dirección correcta y configurar el envío. | R07 |
| WhatsApp | Proveedor, acceso, plantillas y automatización acordada; el número de contacto no basta para enviar mensajes automáticamente. | R07 |
| Facturación | Datos fiscales que capturará el sitio y procedimiento de entrega al club. Sin CFDI automático. | R07 |
| Alimentos | Confirmar pago en línea o en el club y completar el flujo específico de preparación/entrega. | Pedidos de alimentos |
| CARTAQ | Planes, precios, beneficios, vigencia y condiciones de renovación/cancelación. | Membresías y tarifas R02 |
| Contenido real | Imágenes, textos, precios y datos del negocio definitivos. | Catálogo, blog, galería y publicación |

Las preguntas 3.8 y 4.2 ya fueron explicadas al cliente: **3.8 está resuelta** con las aclaraciones de mostrador y pago; **4.2 sigue pendiente**. No volver a tratarlas a ambas como pendientes.

## 6. Después del flujo de reservas

| Trabajo | Estado / alcance siguiente |
| --- | --- |
| Compras de tienda con Mercado Pago | Pendiente conectar pedidos existentes a checkout y notificaciones, revisar stock durante pago, entregas y conciliación. Reutilizar lo preparado en R05. |
| Suscripciones CARTAQ automáticas | Investigación realizada; **sin implementación**. El usuario pidió explorar cobro periódico hasta cancelar, pero falta confirmar con el cliente los planes y condiciones antes de sustituir la renovación manual original. |
| Galería del sitio | **Pospuesto por indicación del usuario**. Retomar su backend y después la integración visual. |
| Eventos e inscripciones | Backend pendiente; existen pantallas/datos de demostración. Definir operación y contrato antes de integrar. |
| Alimentos y preparación | Gestión básica de productos/pedidos disponible; flujo específico y método de pago pendientes. |
| Otros contenidos | Textos institucionales, avisos, contacto y demás formularios: revisar alcance y sustituir simulaciones según prioridad. No quedan cubiertos por el blog. |
| Producción | Configuración por ambiente, secretos, HTTPS, correo, monitoreo, copias/restauración, pruebas conjuntas y despliegue. No dar por listo el sitio solo porque pasen las pruebas del backend. |

## 7. Historial breve

| Fecha | Avance o decisión |
| --- | --- |
| 20 nov 2026 | Backend del blog terminado, migración aplicada y guía publicada. |
| 20 nov 2026 | Revisadas respuestas del cliente; acordados turnos de 15 minutos, apartado configurable, ausencias y reserva de mostrador con pago Mercado Pago. |
| 20 nov 2026 | R01 terminado: configuración operativa, siete horarios, 15 líneas y bloqueos con permisos/auditoría/concurrencia. Migración aplicada y 232 pruebas aprobadas. |
| 20 nov 2026 | Creado este roadmap. Próximo bloque: R02. Galería continúa pospuesta. |
| 20 nov 2026 | R02 terminado en backend: paquetes por persona, tarifa CARTAQ opcional, consumibles internos, composición y control de concurrencia. Migración aplicada, 257 pruebas aprobadas; siguiente R03. |
| 20 nov 2026 | R03.1 terminado: GET público de disponibilidad con horarios, líneas, bloqueos, margen y anticipación. Lectura consistente PostgreSQL, 277 pruebas aprobadas, sin migración. Siguiente R03.2. Corte de 48 horas provisional. |
| 21 nov 2026 | Frontend: panel de tienda conectado y probado; catálogo de armas usa la categoría 3 por indicación del backend. |
| 21 nov 2026 | Frontend: configuración operativa (R01) y staff conectados en el panel y probados. |
| 21 nov 2026 | Frontend: panel de paquetes y consumibles (R02) conectado y probado. |
| 21 nov 2026 | Frontend: reservaciones web conectadas (disponibilidad, apartados, reserva con participantes, confirmación y perfil). Solicitudes al backend en §8. |
| 21 nov 2026 | Frontend: inventario y clientes (historial, cuenta, membresía) conectados y probados. Nuevas solicitudes F12–F14. |
| 21 nov 2026 | Frontend: reportes conectados y probados (pedidos, pagos, membresías, inventario). Solicitud F15. |
| 22 nov 2026 | Frontend: SEO técnico (prerender de páginas públicas y artículos, sitemap, robots, canonical, Open Graph, datos estructurados, noindex en cuenta/panel). Guía en [SEO.md](SEO.md). Redirecciones del WordPress pendientes. |
| 21 nov 2026 | Frontend: panel de marcas (Contenido → Marcas) construido contra el contrato F17; carrusel en bucle continuo. Pendiente API de Kevin. |
| 21 nov 2026 | Frontend: portada `/` con paquetes, productos y blog reales + carrusel «Marcas que nos acompañan» (logos pendientes del cliente). |
| 21 nov 2026 | Frontend: todas las pantallas sin API muestran «pendiente» (sin datos simulados); inicio del panel con datos reales. |
| 21 nov 2026 | Frontend: blog conectado y probado (panel y público, Markdown seguro). Solicitud F16. |
| 21 nov 2026 | Acordado: en modo API no se muestran datos simulados; lista «Módulos SIN API» agregada en §8. |
| 21 nov 2026 | Frontend: pedidos de tienda conectados y probados (checkout, perfil y panel con pago manual/recogida/cancelación). Falta Mercado Pago (R05, backend). |
| 20 nov 2026 | R03.2 terminado: apartados privados con TTL configurable, selección atómica, reintentos sin renovación, vencimiento lógico y protección concurrente. Migración aplicada, 291 pruebas aprobadas. Siguiente R03.3. |

## 8. Solicitudes de frontend al backend · para validar y probar (Kevin / Codex)

Lista viva. Frontend ya integró todo lo que la API permite; estas piezas faltan o mejorarían la integración. Al cerrar una, marcarla aquí con fecha y avisar el contrato. **Codex: valida cada punto contra el código actual, prioriza con Kevin y agrega pruebas.**

| ID | Prioridad | Qué se necesita | Por qué / dónde lo usa el frontend |
| --- | --- | --- | --- |
| F01 | Alta - integración frontend | Contrato R05 de backend disponible; migración aplicada en Supabase. | Conectar `ONLINE_PAYMENT` y `/reservar` al checkout y usar `checkoutUrl` para redirección/QR. Credenciales y pruebas reales con Mercado Pago siguen pendientes. Ver [MERCADO-PAGO.md](MERCADO-PAGO.md). |
| F02 | **Alta - integracion frontend** | Endpoints backend disponibles: listado por fecha/estado y detalle para Administrador y Recepcion, con cliente, participantes, lineas y total. | Conectar `/admin/reservas` (calendario de 15 lineas por horario) a la API. |
| F03 | Alta | R04.2 de mostrador disponible en backend. R06: llegada con QR, validar pago/responsivas, marcar utilizada, descontar consumibles una vez y No asistio. Definir contenido del QR. | Boton Nueva reserva y escaner QR de `/admin/reservas`; QR en confirmacion del cliente. |
| F04 | **Terminado — backend, 23 nov** | `lineasDetalle` en apartados sin alterar `lineas` UUID; `numeroLinea`/`nombreLinea` por participante y `cantidadParticipantes` en resumen propio. [Contrato](FRONTEND-CONTRATOS.md). | Frontend pendiente de usar estos campos para mostrar líneas tras recargar. |
| F05 | **Terminado — backend, 23 nov** | `GET /api/disponibilidad/ventana` público: límites UTC, fechas del club y anticipación configurada. [Contrato](FRONTEND-CONTRATOS.md). | Sustituir ocho consultas fijas por las fechas dentro de la ventana; consultar disponibilidad real por paquete/grupo. |
| F06 | Media | **Cancelar un apartado o reserva pendiente** por el cliente (liberar líneas antes del vencimiento). | Botón «Cancelar» en el paso de pago y en la confirmación. |
| F07 | **Terminado — backend, 23 nov** | `nombreCliente` y `telefonoCliente` en pedidos administrativos, detalle e historial/reporte JSON. Permisos existentes; directorio de perfiles sigue restringido. [Contrato](FRONTEND-CONTRATOS.md). | Frontend pendiente de mostrar los campos en `/admin/pedidos` para Recepción. |
| F08 | Media | Pedidos: **validar existencias al crear** (no solo al confirmar el pago) y, si el cliente lo pide, estados **Preparando / Listo para recoger**. | El cliente puede pedir algo agotado; el error aparece hasta que el staff confirma el pago. |
| F09 | Baja | **Tallas / variantes** de productos con existencias por talla. | Tienda y panel de productos; hoy cada talla tendría que ser un producto distinto. |
| F10 | Baja | **Marcas aliadas (portada)** | F17: tabla, endpoints públicos/administrativos y subida de logo a Storage. | Panel → Contenido → Marcas listo (muestra «pendiente» hasta que exista la API); la portada usa la lista fija de `src/data/brands.ts` mientras `/api/marcas` responda 404, y el carrusel ya gira en bucle. |
| **Plantillas de avisos** (WhatsApp/correo) y correo remitente en configuración. | Se guardan solo en el navegador del administrador. |
| F11 | Baja | Exponer **existencias o «agotado»** en el catálogo público (`GET /api/productos`). | La tienda no puede marcar productos agotados. |
| F12 | Media | **Cobro de membresías CARTAQ**: hoy una vigencia solo se registra con un pago confirmado de un *pedido*, pero no existe un producto/plan de membresía para generarlo. Definir planes (con el cliente) y el flujo: plan → pedido/checkout → pago → vigencia automática. | `/membresias` (inscripción del cliente) y `/admin/clientes` (renovación manual). |
| F13 | Baja | **Historial global de movimientos de inventario** (paginado, con usuario que registró) y usuario en `MovimientoConsulta`. | `/admin/inventario` solo muestra el historial del artículo seleccionado y sin autor. |
| F17 | **Media · pedido por el cliente** | **Marcas aliadas en base de datos** (carrusel «Marcas que nos acompañan» de la portada). Contrato abajo en «Contrato F17». | Panel `/admin/contenido` → pestaña Marcas (ya construida, muestra «pendiente» mientras la API responda 404) y portada `/`. |
| F16 | Baja | **Subida de portadas del blog** (Storage, como imágenes de productos). | `/admin/contenido`: hoy la portada se pega como URL https. |
| F15 | Baja | **CSV de reportes legibles**: agregar nombre del cliente, folio corto del pedido y nombres de producto (hoy el CSV trae solo UUID). Agregar reportes de **reservas, alimentos y eventos** cuando existan. | Botón «Exportar CSV» de `/admin/reportes`. |
| F14 | Baja | En la ficha del cliente: **correo** (vive en Supabase Auth), **reservas** del cliente y **última visita** (con R06). | `/admin/clientes` no puede mostrarlos. |

### Módulos SIN API · el frontend muestra «pendiente» (sin datos simulados)

Regla acordada (21 nov 2026): en modo API **no se muestran datos de ejemplo**. Donde falta backend, la pantalla dice «pendiente» y explica qué falta. **Codex: cada fila es trabajo de backend por hacer; al terminar una, avisar el contrato para conectarla.**

| Módulo / pantalla | Qué falta en la API | Estado en el frontend |
| --- | --- | --- |
| **Reservas en el panel** (`/admin/reservas`) | Integrar listado/detalle y alta de mostrador ya disponibles en backend (F02/F03); llegada con QR de R06. | `/admin/reservas` sigue pendiente de conexion; el cliente si reserva en `/reservar`. |
| **Pago de reservas y pedidos** | R05 Mercado Pago (F01). | Pedido/reserva quedan «Pendiente de pago»; pedidos se cobran manualmente en el panel. |
| **Alimentos y bebidas** (`/alimentos`, `/admin/alimentos`) | Carta pública de alimentos con categorías, pedido de alimentos del cliente, estados recibido → en preparación → listo → entregado, y método de pago (definición del cliente). Hoy solo existe el producto categoría 1 y el inventario. | `/alimentos` y `/admin/alimentos` muestran «pendiente». |
| **Eventos e inscripciones** (`/eventos`, `/admin/eventos`) | Todo el módulo: eventos con cupo y precio, inscripciones, pago e inscritos. | `/eventos`, `/admin/eventos` y «Mis eventos» del perfil muestran «pendiente». |
| **Reportes de reservas, alimentos y eventos** | Endpoints en `/api/reportes/...` cuando existan esos módulos (F15). | Chips «pendiente» en `/admin/reportes`, sin números. |
| **Cartas responsivas** | Texto oficial, firma, almacenamiento y validación por participante (R06). | En `/reservar` solo se acepta el reglamento; perfil y clientes muestran «pendiente». |
| **Galería** | Backend pospuesto por acuerdo. | `/galeria` y la pestaña Galería del panel muestran «pendiente». |
| **Contenido institucional y avisos** (textos, avisos del sitio) | Endpoints de contenido distintos al blog. | Pestañas Textos y Avisos de `/admin/contenido` muestran «pendiente» (el blog es real). |
| **Contacto y suscripciones** | `POST /api/contacto` y `POST /api/suscripciones` (propuestos). | `/contacto` muestra los canales directos (WhatsApp, correo, teléfono) en lugar del formulario; el bloque de suscripción se oculta. |
| **Plantillas de avisos** (WhatsApp/correo) | Configuración persistida (F10) y envío (R07). | Se guardan solo en el navegador. |
| **Membresías: planes y cobro** | Planes CARTAQ y flujo de pago (F12; definición del cliente). | `/membresias` muestra «pendiente» (planes de ejemplo ocultos); renovación manual en `/admin/clientes`; el perfil muestra la vigencia real. |

### Contrato F17 · Marcas aliadas (propuesto por frontend, ajustable)

El frontend ya consume exactamente esto (`PCP-ShootingWeb/src/features/marcas/api.ts`). Si cambia algo, avisar para ajustarlo.

| Método y ruta | Acceso | Cuerpo / respuesta |
| --- | --- | --- |
| `GET /api/marcas` | Público | `[{ id, nombre, descripcion, url, logoUrl, orden }]` solo activas, ordenadas por `orden`. |
| `GET /api/administracion/marcas` | Administrador | Igual + `activo`, `version`; incluye inactivas. |
| `POST /api/administracion/marcas` | Administrador | `{ nombre (≤80, obligatorio), descripcion (≤60, opcional), url (https, opcional), activo, orden }` → la marca creada (201). |
| `PUT /api/administracion/marcas/{id}` | Administrador | Mismo cuerpo + `version` → la marca actualizada; 409 si la versión es vieja. |
| `POST /api/administracion/marcas/{id}/logo` | Administrador | `multipart/form-data`: `archivo` (PNG/JPG/WebP/SVG, ≤ 2 MB) + `idOperacion` (UUID, reintentos idempotentes) → `{ logoUrl }`. Guardar en un bucket público (p. ej. `pcp-marcas`), como las imágenes de productos. Si se acepta SVG, sanitizarlo o servirlo con `Content-Type: image/svg+xml` y `Content-Disposition` seguro. |
| `DELETE /api/administracion/marcas/{id}/logo` | Administrador | 204. La marca queda «solo texto» (`logoUrl = null`). |

Reglas: sin borrado físico (se oculta con `activo=false`); `orden` entero (el panel intercambia el orden de dos marcas con dos PUT); auditoría como en los demás módulos; RLS y permisos revocados para `anon`/`authenticated` como el resto de tablas. Valores iniciales sugeridos: Airguns FV, Altaros, ZAN Projectiles, Donny FL, AEA, H&V (sin logo; el cliente los sube).

### Estado de integración del frontend (21 nov 2026)

| Módulo | Estado |
| --- | --- |
| Login, registro, recuperación, perfiles y roles | Probado |
| Productos de tienda y catálogo de armas (categoría 3) | Probado |
| Pedidos de tienda (sin Mercado Pago) | Probado |
| Configuración operativa, líneas, bloqueos y staff | Probado |
| Paquetes y consumibles | Probado |
| Reservaciones web (sin Mercado Pago) | Probado |
| Inventario | Probado |
| Reportes (pedidos, pagos, membresías, inventario) | Probado |
| Clientes e historial | Probado |
| Blog (panel y público) | Probado |
| Inicio del panel (pedidos por cobrar/entregar, ingresos del día, alertas de inventario) | Probado |
| Portada `/` (paquetes, productos y blog reales; carrusel de marcas) | Probado |
| SEO técnico (prerender, sitemap, robots, vistas previas) | Hecho; validar tras publicar ([SEO.md](SEO.md)) |
| Eventos, alimentos, galeria, contacto y membresias publicas | Pendiente de los modulos correspondientes; reservas administrativas pendientes de integracion frontend (F02/F03). |

### Pruebas sugeridas de integración (frontend + API)

1. Pedido: cliente crea pedido → staff confirma pago → existencias bajan → «Marcar como recogido». Repetir confirmación con existencias insuficientes (debe dar 409 legible).
2. Reserva: elegir paquete visible, 2 personas, turno con 2 líneas libres → apartar → otra cuenta intenta las mismas líneas (409) → registrar participantes (adulto + menor con tutor) → la reserva aparece en «Mis reservas» → al vencer, `estado=Vencida` y las líneas vuelven a `Libre`.
3. Reintentos: repetir `POST /api/apartados` y `POST /api/reservas` con el mismo `idOperacion` (no duplica ni reinicia el contador).
4. Inventario: entrada de 10 → salida de 3 → ajuste −20 (debe dar 409 por existencias negativas) → mínimo 8 → aparece «Bajo mínimo».
5. Clientes: confirmar pago de un pedido del cliente → registrar membresía de 12 meses con ese pago → el cliente ve «Socio CARTAQ» en su perfil; reutilizar el mismo pago (409).
6. Configuración: cambiar actividad/margen y comprobar que `/api/disponibilidad` refleja la nueva grilla; bloqueo global de 1 hora → turnos afectados sin disponibilidad.

## Referencias

- [Índice de documentación](README.md).
- [Configuración operativa](OPERACION.md).
- [Paquetes y consumibles](PAQUETES.md).
- [Consulta de disponibilidad](DISPONIBILIDAD.md).
- [Apartados temporales](APARTADOS.md).
- [Autenticación y roles](AUTH-BACKEND.md).
- [Blog](BLOG.md).
- [Reportes](REPORTES.md).
- [Imágenes](IMAGENES-API.md).
- [SEO](SEO.md): prerender, sitemap y pasos de Search Console y Perfil de Empresa.
- [Contexto e instrucciones del proyecto](../CLAUDE.md): contiene acuerdos y estado histórico; contrastar sus pendientes antiguos con este roadmap y las aclaraciones posteriores.

Los PDF de preguntas y respuestas fueron proporcionados al equipo y analizados, pero no se incorporan automáticamente al repositorio desde la carpeta personal de descargas.

## Cierre R03.3 · 20 nov 2026

Historial propio implementado en GET /api/apartados, con filtros de estado, paginación y consultadoEnUtc. No requiere migración ni proceso de borrado. Verificación local: 251 pruebas aprobadas; 36 PostgreSQL omitidas en esa ejecución y casos de concurrencia excluidos por petición del usuario. La ejecución completa previa encontró límite de conexiones PostgreSQL (53300); no se presenta como aprobada. Las cifras de R03.2 anteriores son históricas.

Verificación PostgreSQL adicional y acotada: 3 pruebas aprobadas de vencimiento exacto, conservación de ocupación al cambiar configuración y migración protegida. No se ejecutaron casos de concurrencia en esta comprobación. Sigue pendiente una ejecución completa de PostgreSQL sin saturación de conexiones.

Decision R04 - 20 nov 2026: cuenta del cliente obligatoria tambien en mostrador; registro autonomo o asistido mediante el flujo existente. Esta decision se implemento en R04.2.

## Cierre R04.1 · 20 nov 2026

Reservaciones web implementadas desde apartado propio, con participantes/tutor, copias de precios y consumibles, idempotencia, detalle e historial. No se cobra ni descuenta inventario; aplica tarifa general mientras se define CARTAQ. R04.2 (mostrador) se cerro el 22 nov 2026; R05 confirmara pagos y agregara ocupacion confirmada.

Verificación: 262 pruebas locales aprobadas, 37 PostgreSQL omitidas en esa ejecución; concurrencia excluida por indicación del usuario. Tres pruebas PostgreSQL específicas aprobadas por separado: reserva/snapshots/migración protegida y compatibilidad con las migraciones anteriores de apartados y operación. Sin ejecución completa de PostgreSQL. Modelo sincronizado con la migración `20261120203716_ReservasWebIniciales`, aplicada en Supabase.

## Corrección de pruebas · 23 nov 2026

Corregidos tres fallos reproducidos: el flujo de mostrador ahora omite la transacción únicamente con el proveedor no relacional de pruebas, como el flujo web existente; PostgreSQL conserva su transacción serializable. La prueba de paginación cuenta los cuatro perfiles iniciales, incluido Recepción. La prueba de acceso administrativo espera 403 para un cliente, según la política del endpoint.

Verificación actual: **265 pruebas locales aprobadas, cero fallidas y 37 PostgreSQL omitidas**. Casos de concurrencia excluidos por indicación del usuario. No se ejecutó PostgreSQL en este cierre; no requiere migración. Las tres pruebas HTTP de mostrador ya tienen resultado aprobado. R05 sigue pendiente de conciliación, credenciales, HTTPS y pruebas integradas.

## Avance R05 · 23 nov 2026

Conciliación manual por pedido/reserva para Administrador, búsqueda paginada y consulta de detalle en Mercado Pago, reintentos idempotentes y validaciones compartidas con webhook. Se corrigió la asociación de intentos sucesivos: una aprobación posterior guarda el identificador del cobro correcto sin sobrescribir una confirmación previa. No requiere migración ni credenciales para ejecutar las pruebas simuladas.

Verificación: **282 pruebas locales aprobadas, cero fallidas y 38 PostgreSQL omitidas**; concurrencia excluida por indicación del usuario. Incluye 17 casos nuevos de R05. Una prueba PostgreSQL de conciliación aprobada por separado en esquema temporal, con HTTP simulado. No se presenta como ejecución completa de PostgreSQL ni compra sandbox real. Siguiente: credenciales de Mercado Pago, webhook HTTPS, conexión frontend y pruebas integradas; la conciliación programada queda pendiente.

## Avance F04, F05 y F07 · 23 nov 2026

Backend terminado para los tres contratos: datos de líneas y conteo de participantes; ventana pública de anticipación; nombre/teléfono en pedidos administrativos. Integración frontend pendiente. Sin migración: solo se vincula la navegación de línea a su FK existente y se amplían las respuestas. Se ajustó la prueba de reconstrucción de reservas para respetar la FK de pagos y las migraciones posteriores de mostrador/R05 dentro del esquema temporal.

Verificación: 285 pruebas locales aprobadas; 39 PostgreSQL omitidas en esa ejecución y concurrencia excluida por indicación del usuario. Dos pruebas PostgreSQL específicas aprobadas por separado: reservas/líneas y pedidos/reportes. Modelo sin cambios de esquema pendientes. Ver [contratos](FRONTEND-CONTRATOS.md). Continúan pendientes las otras solicitudes F; próximos candidatos de backend: F08/F11 (existencias de tienda), F13 (historial global), F15 (CSV legible) y F17 (marcas). F06 debe coordinarse con pagos iniciados y las reglas de cancelación; F09/F12 requieren cerrar variantes/planes.
