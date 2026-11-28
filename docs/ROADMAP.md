# Roadmap compartido · PCP Shooting

Última actualización: **27 de noviembre de 2026**.

**A01 · Base de órdenes de alimentos implementada, 27 nov:** preparación sin pago previo, registro manual del cobro local y entrega con descuento único de inventario. Migración aplicada. **Escrituras desactivadas para producción** (`Alimentos:OrdenesHabilitadas=false`) hasta resolver protección de stock durante preparación, faltantes/no recogidos y permisos de barra; no cierra OQ-05 ni integra Mercado Pago. [Contrato](ORDENES-ALIMENTOS.md). Frontend a cargo de Thiago, sin cambios en este bloque.

Este documento reúne el estado actual y el orden de trabajo del equipo. Las guías de cada módulo explican los contratos de la API. Una pantalla con datos de demostración no significa que su integración esté terminada.

> **Respuestas del cliente recibidas · 27 nov:** Kevin compartió respuestas del cuestionario Parte 2 y confirmó que ya tiene acceso a la cuenta de Mercado Pago. Se registran reglas y dudas restantes en [DEFINICIONES-CLIENTE-2026-11-27.md](DEFINICIONES-CLIENTE-2026-11-27.md). OQ-02–05 están parcialmente aclaradas; no se cierran todavía ni implican módulos implementados. Pendientes destacados: precios CARTAQ/beneficios, texto oficial y plazo de conservación de responsivas, parámetros de eventos y detalles operativos de alimentos. Acceso a la cuenta no equivale a credenciales API/webhook configurados o sandbox aprobado. F24 mantiene su prioridad expresa.

**Backend actualizado a .NET 10 (24 nov):** API y pruebas migradas; despliegue a Azure confirmado por Kevin tras corregir el workflow. Ver [preparación para Azure](DOTNET10.md). F06 queda pendiente de resolver OQ-01; reservas de F14 y reporte de reservas de F15 disponibles en backend, frontend por conectar.

**R04.2 implementado y migración aplicada en Supabase: reserva de mostrador y consulta administrativa. Sus tres pruebas HTTP ya pasan. R05 está en curso en backend; contrato y migración aplicada descritos en `docs/MERCADO-PAGO.md`. R03.3 mantiene su verificación parcial.**

> **R05 backend (24 nov):** checkout, webhook y conciliación manual/programada implementados; ver [MERCADO-PAGO.md](MERCADO-PAGO.md). Checkout devuelve `checkoutUrl` para redirección o QR. Migraciones aplicadas en Supabase; la tarea programada está deshabilitada por defecto. Pendientes: credenciales de prueba, endpoint HTTPS, integración frontend y pruebas sandbox.

> **⚠️ PENDIENTE DE KEVIN · resumen al 27 nov 2026 (frontend ya listo y validado en todo lo demás)**
>
> | Qué | Estado en Azure hoy | Qué hace el frontend mientras tanto |
> | --- | --- | --- |
> | **F24 · Tienda por categorías y marcas** ([contrato](#f24--tienda-por-categorías-y-marcas-contrato-para-backend--27-nov-2026)) | **Backend terminado y migración aplicada, 27 nov; pendiente publicar API en Azure.** | Frontend ya preparado por Thiago; comprobar creación, asignaciones, imágenes y filtros con sesión real después del despliegue. |
> | **Publicar F18** (buscar clientes para mostrador) | Terminado en backend, **no publicado** (404) | Administrador usa `/api/perfiles`; Recepción ve «pendiente». Conectado: al publicarse funciona solo. |
> | **F19** · disponibilidad para personal (turnos dentro de la anticipación web) | **Terminado en backend, 27 nov; pendiente publicar API.** | Thiago puede conectar GET `/api/reservas/mostrador/disponibilidad`; muestra estados reales dentro del mínimo web y conserva el manejo de 409 al crear. |
> | **F20** · Recepción puede leer plantillas de WhatsApp | **Terminado en backend, 27 nov; pendiente publicar API.** | Conectar GET `/api/avisos/whatsapp` para los dos textos. GET/PUT administrativos siguen exclusivos de Administrador. |
> | **F21** · Portadas de página ([contrato](#f21--portadas-de-página-contrato-para-backend--25-nov-2026)) | **Backend terminado y migración aplicada, 27 nov; pendiente publicar API.** | Panel preparado por Thiago; recargar después de publicar y comprobar imagen, enfoque, velo, desactivación y reutilización. |
> | **F22** · `DELETE /api/productos/{id}` sin historial | **Backend terminado, 27 nov; pendiente publicar API.** | Botón listo; bloqueos 409, auditoría y limpieza reintentable de Storage. [Contrato](ELIMINAR-PRODUCTOS.md). |
> | **F23** · varias fotos por producto (listar, agregar, ordenar, quitar) | **Backend terminado, 27 nov; pendiente publicar API.** | Hasta ocho fotos, principal y secundarias, `agregar=true`, orden completo y retirada. [Contrato](PRODUCTOS-GALERIA.md). Thiago debe conectar el editor. |
> | **R05 · Mercado Pago** (credenciales, HTTPS, webhook en Azure) | **Webhook publicado, comprobado 27 nov:** GET → 405 y POST sin firma/data.id → 401. Pendiente configurar/verificar credenciales y pago sandbox; no prueba procesamiento de pagos válidos. | Reservas y pedidos quedan «Pendiente de pago»; pedidos se cobran a mano en el panel. |
> | **R06** · llegada con QR, cartas responsivas, última visita (F14) | Pendiente | «Pendiente» en panel y perfil. |
> | **R07** · envío de avisos por correo/WhatsApp | Pendiente | Plantillas guardadas (F10), sin envío. |
>
> Dependen de **decisión del cliente** (no de Kevin): OQ-02 membresías CARTAQ (F12), OQ-04 inscripción a eventos, OQ-05 pedido y cobro de alimentos, OQ-01 cancelaciones (F06).
>
> **Frontend validado por Thiago (26 nov):** pasos 0–12, subidas reales a Storage (paso A), F18 con Administrador (paso B), textos y contacto (paso C), rol Recepción, portadas en vista previa, aviso de eliminar, alertas del sitio público y celular/tableta (paso D).

> **Storage en Azure · actualización de Kevin, 25 nov 2026:** `Supabase__PublishableKey` y `Supabase__Url` ya están configuradas en App Service. El bloqueo anterior por falta de configuración se retira como tarea pendiente. **Falta verificar una subida real con sesión autorizada** en galería, imágenes de productos, logos de marcas y portadas del blog; configurar las claves no da esas pruebas por aprobadas.
>
> **Blog · portada (25 nov):** frontend **conectado** (paso 10): Contenido → Blog → editor → «Portada» sube el archivo a `POST /api/administracion/blog/{id}/portada` (artículo guardado; reintento con el mismo `idOperacion`; 409 = recargar). Claves de Storage configuradas por Kevin el 25 nov; **pendiente repetir la carga real** para comprobar que se resolvió el 503 anterior. También comprobar galería, logos de marcas y foto de producto. La alternativa de URL https se conserva.
>
> **Portadas de página (F21, backend terminado 27 nov):** tabla, bucket `pcp-portadas` y cuatro rutas implementados; migración aplicada. Frontend preparado; pendiente publicar la API y comprobar con sesión real. [Contrato](#f21--portadas-de-página-contrato-para-backend--25-nov-2026).
>
> **Eliminar productos (F22, backend terminado 27 nov):** `DELETE /api/productos/{id}` solo sin historial ni imágenes compartidas; incluye auditoría y limpieza reintentable de Storage. Botón listo en el panel; pendiente publicar y comprobar con sesión real. [Contrato](ELIMINAR-PRODUCTOS.md).

**En paralelo, frontend puede conectar configuración operativa, paquetes, blog, inventario, historial de clientes y reportes a sus endpoints existentes.** Coordinar cada conexión antes de cambiar el contrato.

## Cómo mantenerlo

- Backend: actualizar el estado de la API, migraciones, pruebas y documentación al cerrar cada bloque.
- Frontend: actualizar su columna cuando sustituya la simulación por llamadas reales y compruebe el flujo. Desde el 24 nov, las nuevas integraciones frontend quedan a cargo del compañero; este trabajo se concentra en backend salvo nueva indicación.
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
| Órdenes de alimentos · A01 | **Base implementada; desactivada para producción.** Recibido → EnPreparacion → Listo → Entregado, pago separado, cobro local manual y descuento al entregar; versiones, reintentos, historial y auditoría. [Contrato](ORDENES-ALIMENTOS.md). | Sin integración nueva; Thiago debe usar las rutas específicas cuando se acuerde la activación. Pendientes OQ-05, pago online y reportes propios. No sustituye automáticamente `/api/pedidos`. |
| Membresías CARTAQ | Parcial: registro de vigencias y renovación con pago completo confirmado | Falta cerrar planes, precios, beneficios y renovación definitiva. Cobro recurrente no implementado. **Frontend (21 nov):** el perfil del cliente lee `GET /api/membresias/vigente`; en `/admin/clientes` se registra/renueva una vigencia eligiendo un pago confirmado del cliente (ver F12). |
| Historial del cliente | Terminado para perfil, pedidos, pagos y membresías | **Probado (21 nov):** `/admin/clientes` conectado: lista de clientes (`GET /api/perfiles?rol=Cliente`), ficha con historial (pedidos, pagos, membresías), activar/desactivar cuenta y WhatsApp. Sin cartas responsivas ni última visita (no existen en la API). Probado con Administrador. **24 nov (F14):** agrega correo y reservas del cliente; pendiente prueba. |
| Reportes CSV/JSON | Terminado para pedidos, pagos, membresías e inventario | **Probado (21 nov):** `/admin/reportes` conectado con datos reales: ventas de tienda (pedidos), ingresos (pagos confirmados), membresías (con socios vigentes hoy) e inventario; rango de fechas en días del club, KPIs, gráfica diaria, tabla con nombre del cliente y CSV generado por la API. Reservas, alimentos y eventos se muestran como «pendiente» (sin datos de ejemplo). Probado con Administrador. |
| Blog | Terminado: borradores, programación, publicación, archivo, filtros y auditoría | **Probado (21 nov):** conectado. Panel `/admin/contenido` → pestaña Blog: crear/editar con `version`, slug, resumen, categoría, portada por URL, destacado, Markdown con barra de formato y vista previa, publicar ahora, programar (hora del club), despublicar y archivar. Público `/blog` y `/blog/:slug` leen la API y renderizan Markdown de forma segura (`react-markdown`, sin HTML crudo). Galería, textos y avisos muestran «pendiente». Probado con Administrador. |
| Configuración operativa, líneas y bloqueos | Terminado el punto R01; migración aplicada en Supabase | **Probado (21 nov):** `/admin/configuracion` conectado: horario semanal con primer/último turno, reglas (actividad, margen, apartado, anticipación, llegada/tolerancia) con `version`, 15 líneas (nombre/activa) y bloqueos (crear/quitar, hora del club). Staff real: lista Administrador/Recepción, dar acceso a un cliente registrado y quitarlo (`PATCH /api/perfiles/{id}/acceso`). Plantillas de WhatsApp/correo de avisos siguen locales (sin API). Probado con Administrador. |
| Paquetes y consumibles | Terminado R02: gestión por persona, precios general/CARTAQ opcional, composición, versiones, permisos y catálogo público | **Probado (21 nov):** `/admin/paquetes` conectado: lista con estado (visible al reservar / activo oculto / inactivo), crear y editar con `version`, código, incluye/requisitos (Markdown), precio general y CARTAQ (vacío = pendiente), activar/desactivar y composición de consumibles por persona; crear consumible interno (categoría 4) desde el editor. Recepción solo lectura. Sin «renta de rifle» (no existe en la API). Falta cargar el catálogo real del cliente y conectar `/reservar` (siguiente bloque). Probado con Administrador. |
| Consulta de disponibilidad | Terminado R03.1 y ampliado en R03.2: turnos, mapa, bloqueos, anticipación y apartados vigentes | **Probado (21 nov):** `/reservar` consulta los próximos 8 días (`GET /api/disponibilidad` con `personas`) y muestra días, turnos y mapa de líneas con el motivo real de cada turno/línea (No disponible, Bloqueada, Inactiva, Ocupada). Probado con cliente. |
| Apartados temporales | Terminado R03.2: selección atómica de líneas, plazo configurable, idempotencia, privacidad y expiración lógica | **Probado (21 nov):** `/reservar` aparta N líneas (una por persona) con UUID estable en reintentos y contador de vencimiento; si vence o hay 409, recarga disponibilidad. Probado. |
| Galería del sitio | **Implementada en backend y conectada en frontend, 24 nov; migración aplicada** | Álbumes/fotos, carga, orden, publicación y visor. Pendiente despliegue y primera carga con Administrador real. [Contrato](GALERIA.md). |

Última verificación del backend al cerrar R03.2: **291 pruebas aprobadas**, incluidas las pruebas PostgreSQL en esquemas temporales. Las migraciones de blog, configuración operativa, paquetes/consumibles y apartados están aplicadas en Supabase. Esta cifra es evidencia de ese cierre, no una sustitución de las pruebas de cambios posteriores.

## 2. Orden de trabajo de reservaciones

Los IDs R01–R08 son estables para usarlos en mensajes y tareas. No son los números del plan anterior de mejoras, cuyo punto 5 era blog/galería.

| ID | Bloque | Estado backend | Dependencias y criterio de cierre |
| --- | --- | --- | --- |
| R01 | Configuración operativa, horarios, líneas y bloqueos | **Terminado** | Endpoints con permisos, auditoría, versiones, valores iniciales y migración aplicada. Integración del panel pendiente. |
| R02 | Paquetes y consumibles | **Terminado — backend** | Gestión implementada, composición por persona, tarifa general/CARTAQ opcional, consumibles internos, permisos, auditoría y concurrencia. Migración aplicada. Falta integrar frontend y recibir/cargar el catálogo definitivo; no se inventaron paquetes reales. |
| R03 | Disponibilidad y apartados temporales | **Terminado - backend; concurrencia omitida en el cierre R03.3** | Consulta y apartados implementados. R05 agrega la confirmación de reservas y pagos; ver [MERCADO-PAGO.md](MERCADO-PAGO.md).
| R04 | Reservaciones web y de mostrador | **Parcial - R04.1 y R04.2 terminados en backend; integracion de `/admin/reservas` pendiente** | R04.2 agrega mostrador y consultas administrativas; migracion aplicada en Supabase. Confirmacion y ocupacion pagada dependen de R05. El wizard `/reservar` esta conectado a R04.1. **Frontend (24 nov):** `/admin/reservas` conectado a R04.2 (lista, mapa, detalle y mostrador); ver F18/F19. |
| R05 | Mercado Pago en entorno de pruebas | **En curso — checkout, webhook y conciliación manual/programada implementados** | Progreso persistente, lotes y reintentos; tarea deshabilitada por defecto. Migración de conciliación aplicada en Supabase el 24 nov. Pendientes credenciales, HTTPS, frontend y sandbox. Ver [MERCADO-PAGO.md](MERCADO-PAGO.md). |
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
| Galería del sitio | **Implementada el 24 nov** según diseño aceptado, con Storage público y migración aplicada. Ver [contrato y pendientes reales](GALERIA.md). |
| Eventos e inscripciones | **Parcial — eventos informativos disponibles en backend, 24 nov.** Gestión de borrador/publicado/archivado y consulta pública. Inscripciones, cupos, pagos, asistencia y reporte pendientes de OQ-04. [Contrato](EVENTOS.md). Frontend por integrar por Thiago. |
| Alimentos y preparación | **Parcial — carta pública disponible en backend, 24 nov.** Lista/detalle de productos Alimento activos/vendibles, con precio, imagen, notas y stock. Gestión existente para Administrador/Recepción. Flujo específico y método de pago pendientes de OQ-05. [Contrato](ALIMENTOS.md). |
| Otros contenidos | Textos institucionales/contacto/redes y formulario con bandeja implementados y conectados en código el 24 nov; migraciones aplicadas. Avisos públicos terminados en backend (24 nov); **frontend conectado 25 nov** (Contenido → Avisos y franja en el sitio), pendiente prueba con Administrador. [Contenido](CONTENIDO-SITIO.md), [contacto](CONTACTO.md), [avisos públicos](AVISOS-PUBLICOS.md). |
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
| 24 nov 2026 | Frontend paso 5 (F13): `/admin/inventario` → pestaña «Historial general» (solo Administrador) con `GET /api/inventario/movimientos`: búsqueda por artículo/SKU, tipo, rango de fechas del club, paginación de 25 y columna «Registró» (`actorNombre`); el historial por artículo también muestra el usuario. Probado con Administrador. |
| 24 nov 2026 | Frontend paso 6 (F14): ficha de `/admin/clientes` con correo de Supabase Auth y reservas paginadas (`paginaReservas`), enlace a `/admin/reservas?fecha=…&reserva=…` (abre el día y el detalle). Paso 5 probado con Administrador. Paso 6 probado con Administrador. Sigue pendiente en API: última visita (R06| 24 nov 2026 | Frontend: revisión responsiva de todo el sitio y panel (360, 390, 768 y 1000 px, con fichas, pestañas, mapa y formularios abiertos). Corregido: barras de filtros del panel en celular (chips se deslizan dentro de la pantalla), Fecha + Lista/Mapa en reservas, encabezados de sección, etiquetas de estado y textos largos (correos, SKU). Sin desbordes horizontales medidos. |
| 25 nov 2026 | Frontend paso 7 (F15): `/admin/reportes` conecta **Reservas** (`/api/reportes/reservas`, fechas = día de la actividad; KPIs con `importeConfirmado`, gráfica de pagadas, tabla con folio/cliente/paquete/personas/estado) y **Alimentos y bebidas** (`/api/reportes/alimentos`, una fila por partida pagada/recogida) con CSV de la API. Pedidos/pagos/membresías usan `nombreCliente`, `folio` y `productos` del JSON (se quitó la consulta aparte de perfiles). Eventos sigue «pendiente». Probado con Administrador. |
| 25 nov 2026 | Frontend paso 8 (avisos públicos): Contenido → **Avisos** con lista (filtro por estado calculado Vigente/Programado/Vencido/Inactivo), editor (título opcional, texto plano ≤1000, orden, «mostrar desde/hasta» en hora del club → UTC, activo), vista previa y 409 = recargar sin sobrescribir. Sitio: franja arriba del header en todas las páginas públicas (`GET /api/avisos-publicos`), se vuelve a consultar cada 5 min y al volver a la pestaña, se oculta sola al vencer y se puede cerrar por sesión. Paso 7 probado con Administrador. Paso 8 probado. |
| 25 nov 2026 | Frontend paso 9 (F10): plantillas de avisos en `/admin/configuracion` conectadas a `GET/PUT /api/administracion/avisos` (versión, 409, vista previa con marcadores, «Descartar cambios»); Pedidos rellena WhatsApp con la plantilla guardada. Nueva solicitud F20 (lectura para Recepción). Paso 8 probado con Administrador. Paso 9 probado. |
| 25 nov 2026 | Frontend paso 10 (F16): subida de portada del blog conectada (arrastrar/elegir JPG/PNG/WebP ≤ 5 MB, vista previa, «Reintentar subida», mensaje claro ante 503, quitar portada, alternativa URL https). Paso 9 probado. **En espera de Kevin** (Storage en Azure) para la prueba real. |
| 25 nov 2026 | Frontend paso 11 (eventos informativos): `/admin/eventos` conectado a `/api/administracion/eventos`; sitio `/eventos` con Próximos/Pasados (`proximos=true|false`) y nueva ruta `/eventos/:slug` (detalle Markdown seguro, fecha en hora del club, 404 → página no encontrada). Sin inscripciones/cupo/precio (OQ-04). Paso 10 validado en lo posible (subida real en espera de Kevin). Paso 11 probado. |
| 25 nov 2026 | Frontend paso 12 (alimentos): sitio `/alimentos` con la carta real (`GET /api/alimentos`: foto, descripción, notas Markdown seguras, precio, agotados al final; se vuelve a consultar cada 3 min y al volver a la pestaña; sin pedido en línea: «pide y paga en barra»). Panel `/admin/alimentos` (Administrador y Recepción): productos categoría 1 con KPIs, filtros, crear/editar (SKU, precio, notas), mostrar/ocultar, foto (503 hasta Storage) y entradas de inventario. Paso 11 probado. Probado con Administrador; foto del producto pendiente de Storage (503). Propuesta de pedido web en OQ-05. OQ-05 sigue abierta (pedido, pago, cocina). |
| 25 nov 2026 | Frontend: foto de fondo en el encabezado de Inicio (`public/img/hero-tirador.webp`, velo oscuro, cifras semitransparentes) y nuevo módulo **Panel → Portadas** (`/admin/portadas`, solo Administrador): portada por página, enfoque tocando la foto, velo suave/normal/fuerte, reutilizar imagen de otra página; `PageHero` la aplica según la ruta. En vista previa hasta F21 ([§ F21 al final de este documento](#f21--portadas-de-página-contrato-para-backend--25-nov-2026)). |
| 26 nov 2026 | Frontend: **alertas emergentes en todo el proyecto** (`src/lib/toast.ts` + `Toaster`): cada acción de guardar, editar, agregar, eliminar o subir que pasa por la API muestra un aviso verde (éxito) o rojo (error con el mensaje de la API); perfil también. Los errores **502/503/504 ahora muestran el mensaje de la API con su código** (antes «El servidor no pudo completar la solicitud»), para diagnosticar Storage. Galería: aviso de que las fotos suben **ocultas**. Las alertas van arriba a la derecha (celular: arriba, a todo el ancho). Un 404 sin mensaje (ruta inexistente en Azure) o un 405 ahora dice qué acción y ruta falló. Galería: los interruptores «Álbum publicado» y «Foto publicada» no mostraban su texto (corregido). Prueba de subida real (paso A): en Galería, «Guardar» devolvió **404 de ruta inexistente**; las rutas de galería sí existen en Azure, pendiente identificar la URL con la nueva alerta. |
| 26 nov 2026 | Frontend (UX): **un solo aviso por acción**. Los mensajes verdes dentro de las pantallas se quitaron en todo el proyecto: ahora solo sale la alerta emergente, con el texto específico de la pantalla, que reemplaza al genérico de la API. Los errores de una acción también salen solo como alerta. Siguen en pantalla los avisos que deben quedarse visibles: validación del formulario, error al cargar datos, «pendiente de backend», «Revisa tu correo», contador del apartado y «foto NO guardada». Lógica central en `FormAlert` (`inline` = fijo en pantalla) y `src/lib/toast.ts`. |
| 26 nov 2026 | Prueba real de subida (paso A): **Galería validada** con Azure/Storage (subir, publicar foto y álbum, visible en `/galeria`). Siguiente: foto de producto. Productos y Catálogo de armas muestran aviso fijo «foto NO guardada» si la subida falla. |
| 26 nov 2026 | Prueba real de subida (paso A): **foto de producto validada**. Siguiente: logo de marca (Contenido → Marcas), que ahora muestra un aviso fijo «logo NO guardado» si la subida falla. |
| 26 nov 2026 | Prueba real de subida (paso A): **logo de marca validado**. Siguiente: portada del blog (F16), que ahora muestra un aviso fijo «portada NO guardada» junto a «Reintentar subida» si la subida falla. |
| 26 nov 2026 | Prueba real de subida (paso A): **portada del blog validada** (la imagen se guarda en Storage). Corregido en frontend un **409 «El artículo cambió»**: la portada se sube sola al elegirla y cambia la `version`; guardar o escribir durante la subida mandaba la versión anterior. Ahora los botones de guardar se bloquean mientras sube, las ediciones no pisan la versión nueva y, si aun así hay 409, se carga la versión actual con un mensaje claro. Backend sin cambios (comportamiento correcto). Siguiente: foto de alimento. |
| 26 nov 2026 | Paso A: **portada del blog validada** sin 409. Siguiente prueba: foto de alimento («Kit de pollo», hoy sin imagen en `/api/alimentos`). Corregido en frontend: en Alimentos, escribir mientras subía la foto podía devolver el `imageUrl` anterior y, al guardar, el PUT (que reemplaza todo) borraba la foto; ahora las ediciones no pisan la foto nueva y no se puede elegir otra foto con una acción en curso. Productos y Armas ya bloqueaban Guardar durante la subida. |
| 26 nov 2026 | **Paso A terminado** (subidas reales a Storage: galería, producto, marca, portada del blog y alimento, validadas). Paso B: **F18 conectado** en `/admin/reservas` → Reserva de mostrador (`searchCounterClients` en `reservasAdminApi.ts`); Azure aún da 404, con respaldo para Administrador y aviso «pendiente» para Recepción. `ApiError.missingRoute` detecta rutas no publicadas aunque Azure mande el título en inglés «Not Found»; esos títulos ya no se muestran al usuario. **Kevin: falta publicar F18 en Azure.** |
| 26 nov 2026 | F18 validado con Administrador (respaldo `/api/perfiles`); Recepción en espera de que se publique. Revisión en Azure: **sin publicar** F18 (404), F19 (404), F21 portadas (404), F22 DELETE productos (405) ni webhook de Mercado Pago (404). Tablas «Módulos SIN API» y «Estado de integración» actualizadas. Siguiente (paso C): validar Textos del sitio (Contenido → Textos) y Contacto (formulario + Mensajes), conectados en código pero sin prueba con Administrador. |
| 26 nov 2026 | Paso C: **Textos del sitio y Contacto validados** con Administrador. Pendientes de validar del lado frontend (paso D): rol Recepción con cuenta real, Portadas en vista previa, aviso de «Eliminar producto» (F22 pendiente), alertas del sitio público (perfil, checkout, cancelar pedido, reservar) y revisión en celular/tableta reales. |
| 26 nov 2026 | **Paso D validado** (rol Recepción con cuenta real, Portadas en vista previa, aviso de eliminar producto, alertas del sitio público, celular/tableta). Frontend sin pendientes de validación propios; lo que falta es de Kevin o del cliente (resumen al inicio del documento). Siguiente: mejoras pedidas por Thiago. |
| 27 nov 2026 | Mejora 1 · **vista previa de producto** `/tienda/producto/:sku` (estilo tienda deportiva): galería grande (miniaturas en escritorio, deslizable con contador en celular), categoría, precio, «Quedan N», selector de tallas obligatorio con tallas agotadas tachadas, cantidad con tope de existencias, «Agregar al carrito» (abre el carrito en tableta/escritorio, alerta en celular), recolección en el club, descripción y notas Markdown, y «También te puede gustar». Se abre desde la foto o nombre en la tienda, la portada y el carrito. Precio/existencias salen del catálogo (la ficha `GET /api/productos/{sku}` no los trae). **Varias fotos: no existen en la API** (solo `imagenUrl`); solicitud **F23** a Kevin. |
| 27 nov 2026 | Mejora 1 validada. Ajuste: **toda la tarjeta de producto abre la vista previa** (enlace extendido sobre la tarjeta, borde y sombra al pasar el cursor, foco visible con teclado); «Agregar» sigue agregando sin abrir la página. |
| 27 nov 2026 | Mejora 1 · ajustes: **tallas seleccionables en la tarjeta de la tienda** (botones S/M/L/XL; «Agregar» exige talla y la manda al carrito, con alerta); sin borde negro al pasar el cursor (solo sombra, y solo con mouse); en la vista previa la **descripción larga se recorta a 6 líneas con «Leer más / Ver menos»** y la foto queda fija mientras se lee. |
| 27 nov 2026 | Mejora 2 · **tienda por categorías y marcas (F24, PRIORIDAD 1 para Kevin)**. Frontend completo: `/tienda` con botones de categoría con foto («Compra por categoría») y «Todos los productos»; `/tienda/{categoria}` con logos de marcas (salen de los productos) y productos; `/tienda/{categoria}/{marca}`; vista previa con ruta Tienda › Categoría › Marca y etiqueta de marca en tarjetas. Panel: Productos → pestaña **Categorías** (crear, foto, dirección web, orden, visible, eliminar si está vacía) y listas **Categoría de la tienda / Marca** en el editor de producto («Tipo de producto» sigue para tallas). Contenido → Marcas: dirección web y «Mostrar también en el carrusel». Textos de la tienda actualizados (ya no solo ropa). Probado con datos simulados de F24 y con Azure actual (sin F24 todo queda como antes y el panel dice «pendiente (F24)»). Contrato al final del documento. |
| 25 nov 2026 | Frontend: botón «Eliminar producto» (`DeleteProductButton`, `DELETE /api/productos/{id}`) en Productos, Catálogo de armas y Alimentos; solicitud **F22** para Kevin (solo sin historial, 409 si lo tiene). Mientras no exista la ruta, el panel avisa «pendiente de backend». |
). |
| 24 nov 2026 | Frontend paso 4 (R04.2): `/admin/reservas` conectado: reservas por fecha con filtro de estado, KPIs, vista lista y mapa de 15 líneas × horario, detalle con participantes por línea y WhatsApp, y reserva de mostrador (cliente registrado, paquete, fecha/hora, líneas, participantes, UUID estable). Nuevas solicitudes F18 (búsqueda de clientes para Recepción) y F19 (disponibilidad para personal). Pendiente prueba con Administrador. |
| 24 nov 2026 | Frontend paso 3 (F04/F05): `/reservar` consulta solo las fechas de `GET /api/disponibilidad/ventana` (antes 8 días fijos); números de línea desde `lineasDetalle` y `participantes[].numeroLinea` (confirmación y perfil ya no dependen de sessionStorage); «Mis reservas» muestra personas y el estado `Pagada` (Confirmada). Pendiente prueba con cliente. |
| 24 nov 2026 | Frontend paso 2 (F07): `/admin/pedidos` e inicio del panel usan `nombreCliente`/`telefonoCliente` de cada pedido (Recepción ya ve nombre y teléfono; se quitó la consulta a `/api/perfiles`). Búsqueda por folio, nombre o teléfono. Probado con Administrador; probado con Recepción (paso D, 26 nov). |
| 24 nov 2026 | Frontend paso 1 (F08/F09/F11): tienda con existencias (`existencia`/`agotado`, «Agotado», «Quedan N»), tallas S/M/L/XL en tarjeta, carrito (tallas agotadas deshabilitadas, tope por existencia, avisos), checkout (refresca existencias, bloquea faltantes, maneja 409) y pedidos con `talla` por partida. Panel: interruptor «Maneja tallas» en playeras, existencias y entradas por talla en Productos, movimientos e historial por talla en Inventario, talla en el detalle de pedidos. Probado con Administrador y cliente (paso 1). |
| 24 nov 2026 | Validación con API de Azure: crear álbum OK; subir imagen → 503 «Falta configurar Supabase Storage» (pendiente Kevin, ver aviso al inicio). Galería: subida inmediata al elegir archivos; `UploadDrop` admite arrastrar y soltar y reelegir el mismo archivo. |
| 24 nov 2026 | Frontend (validación paso 0): corregido el alta de álbumes de galería (se enviaba `version: ""` y la API respondía 400); `guardarFoto` envía solo campos editables; los errores técnicos en inglés del binder ya no se muestran al usuario. |
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
| F06 | **Pendiente de aclaración — OQ-01** | Solicitud original: **cancelar un apartado o reserva pendiente** por el cliente (liberar líneas antes del vencimiento). Alcance sin aprobar por conflicto con la regla del cliente sin cancelaciones/reembolsos. | Frontend debe precisar el comportamiento del botón «Cancelar» en el paso de pago y en la confirmación. Ver [OQ-01](#oq-01--f06-abandonar-un-apartado-o-cancelar-una-reserva). |
| F07 | **Terminado — backend, 23 nov** | `nombreCliente` y `telefonoCliente` en pedidos administrativos, detalle e historial/reporte JSON. Permisos existentes; directorio de perfiles sigue restringido. [Contrato](FRONTEND-CONTRATOS.md). | Frontend pendiente de mostrar los campos en `/admin/pedidos` para Recepción. |
| F08 | **Stock terminado — backend, 23 nov** | Pedidos nuevos validan stock y devuelven 409 sin pedido parcial; pago revalida y descuenta. Preparando/Listo para recoger queda por definir si el cliente lo solicita. | [Contrato](FRONTEND-CONTRATOS.md). Frontend debe actualizar stock ante conflictos. |
| F09 | **Terminado — backend, 23 nov** | S/M/L/XL, `usaTallas` explícito, stock por talla, pedidos multitalle y descuento idempotente al pagar. Modelo/color sigue siendo producto independiente y precio común. Migración aplicada sin repartir stock anterior. [Contrato](TALLAS.md). | Frontend pendiente: activación en editor, stock por talla, selección y clave de carrito producto+talla. Productos anteriores siguen sin tallas hasta configurarlos. |
| F10 | **Terminado — backend, 23 nov** | GET/PUT `/api/administracion/avisos`: `notifyEmail`, `waOrderReady`, `waReminder`, versión y hora UTC. Solo Administrador; auditoría y migración aplicada. [Contrato](AVISOS.md). Envío efectivo sigue en R07; marcas es F17. | Frontend pendiente de sustituir guardado en navegador por la API. |
| F11 | **Terminado — backend, 23 nov** | `existencia`/`agotado` públicos; nulos para no vendibles. [Contrato](FRONTEND-CONTRATOS.md). | Frontend pendiente de mostrar agotados y limitar cantidades. |
| F12 | Media | **Cobro de membresías CARTAQ**: hoy una vigencia solo se registra con un pago confirmado de un *pedido*, pero no existe un producto/plan de membresía para generarlo. Definir planes (con el cliente) y el flujo: plan → pedido/checkout → pago → vigencia automática. | `/membresias` (inscripción del cliente) y `/admin/clientes` (renovación manual). |
| F13 | **Terminado — backend, 23 nov** | `GET /api/inventario/movimientos` para Administrador, paginado/filtrable. Producto y autor desde auditoría, también por producto; desconocidos nulos. [Contrato](FRONTEND-CONTRATOS.md). | **Frontend conectado y probado (24 nov, paso 5):** `/admin/inventario` → «Historial general» con filtros, paginación y autor; columna Usuario en el historial por artículo. Recepción conserva su límite a alimentos por producto. |
| F17 | **Terminado — backend, 23 nov** | Listas pública/administrativa, alta/edición con versión, ocultar, logos PNG/JPG/WebP de 2 MiB y quitar logo. Seis marcas iniciales sin imágenes. Migración/bucket/políticas aplicados. [Contrato](MARCAS-Y-PORTADAS.md). | Panel/portada existentes consumen las rutas en modo API. Selector y manejo de versión/reintento ajustados; falta prueba con JWT real. |
| F16 | **Terminado — backend, 23 nov** | `POST /api/administracion/blog/{id}/portada`: PNG/JPG/WebP de 5 MiB, idempotencia y versión actualizada sin publicar borradores. Bucket/políticas aplicados. [Contrato](MARCAS-Y-PORTADAS.md). | **Frontend conectado (25 nov, paso 10)**: selector en el editor del blog, URL y versión se actualizan sin perder cambios. Kevin confirmó claves de Storage configuradas en Azure el 25 nov; pendiente repetir subida para comprobar resolución del 503 anterior. |
| F15 | **Parcial — reservas y ventas de alimentos disponibles, 24 nov** | JSON/CSV de reservas y partidas alimenticias de pedidos pagados/recogidos; filtros e importes del conjunto completo. Pendientes eventos y datos de R06; flujo operativo de alimentos separado. [Contrato](FRONTEND-CONTRATOS.md). | **Frontend conectado (25 nov, paso 7):** `/admin/reportes` → Reservas y Alimentos y bebidas con KPIs, gráfica, tabla y CSV; nombres/folio/productos desde el JSON. Eventos sigue «pendiente». Probado con Administrador (25 nov). |
| F14 | **Parcial — reservas y correo disponibles, 24 nov** | Historial administrativo agrega reservas paginadas y correo actual consultado en Supabase Auth. Pendiente **última visita** (R06). [Contrato](FRONTEND-CONTRATOS.md). | **Frontend conectado (24 nov, paso 6):** ficha de `/admin/clientes` muestra `correo` (mailto; «Sin correo vinculado» si es nulo) y sección Reservas (paquete, fecha/hora del club, personas, Web/Mostrador, total, estado) con «Ver más» vía `paginaReservas`; cada reserva abre `/admin/reservas?fecha&reserva`. Pendiente prueba con Administrador. Última visita sigue pendiente (R06). |
| F18 | **Terminado — backend, 25 nov** | GET `/api/reservas/mostrador/clientes?busqueda=` para Administrador/Recepción activos, solo clientes activos; coincidencia parcial nombre/teléfono, búsqueda obligatoria 2–150 caracteres, paginación y DTO mínimo `id`/`nombre`/`telefono`. Sin ampliar permisos de `/api/perfiles`. [Contrato](FRONTEND-CONTRATOS.md#f18--buscar-clientes-para-reservas-de-mostrador--25-nov-2026). | Thiago puede sustituir el paso Cliente pendiente de Recepción por esta búsqueda. Sin cambios de frontend; requiere publicar API e integrar/probar. F19 sigue pendiente. **Frontend conectado (26 nov, paso B):** reserva de mostrador busca al escribir (≥ 2 caracteres, espera 350 ms), «Ver más clientes» con paginación, Administrador y Recepción. **Azure aún responde 404 (no publicado)**: mientras tanto el Administrador usa `/api/perfiles` y Recepción ve «pendiente de publicar». Al publicarse funciona sin cambios. |
| F19 | **Terminado — backend, 27 nov** | GET `/api/reservas/mostrador/disponibilidad`, solo Administrador/Recepción activos, con parámetros/DTO de disponibilidad existentes. Omite mínimo web; conserva máximo, turnos futuros, bloqueos, inactivas, apartados y protección de pagos/conciliación. Crear mostrador usa la misma variante. [Contrato](FRONTEND-CONTRATOS.md#f19--disponibilidad-para-mostrador--27-nov-2026). | Sin cambios de frontend; Thiago debe conectar el mapa/selector de mostrador y conservar 409. Consulta pública/ventana sin cambios. Pendiente publicar API y probar con sesión real. |
| F20 | **Terminado — backend, 27 nov** | GET `/api/avisos/whatsapp` para Recepción/Administrador activos, solo `waOrderReady` y `waReminder`, sin correo, versión o fechas. No amplía GET/PUT `/api/administracion/avisos` ni habilita envío. [Contrato](AVISOS.md#f20--lectura-operativa-de-whatsapp--27-nov-2026). | Thiago debe conectar la ruta de lectura al WhatsApp de Pedidos y mantener el texto alternativo si está vacío. Sin frontend modificado; pendiente publicar API e integrar. |
| F21 | **Terminado — backend, 27 nov; por publicar** | **Portadas de página** (foto de fondo del encabezado negro en Inicio, Nosotros, Reservar, Catálogo, Tienda, Eventos, Alimentos, Membresías y Contacto): `GET /api/portadas` (público), `GET /api/administracion/portadas`, `PUT /api/administracion/portadas/{pagina}` (activa, imagenUrl, posicion, velo, version) y `POST …/{pagina}/imagen` (multipart, bucket `pcp-portadas`, 8 MB). Contrato completo en [§ F21 al final de este documento](#f21--portadas-de-página-contrato-para-backend--25-nov-2026). | **Frontend preparado:** backend y bucket implementados el 27 nov; pendiente publicar la API, recargar el sitio y comprobar con Administrador real. |
| F22 | **Terminado — backend, 27 nov; por publicar** | DELETE `/api/productos/{id}` → 204 solo sin partidas de pedidos, movimientos (incluso iniciales/neto cero), consumos de reserva, composición de paquetes ni imágenes compartidas. 409 sugiere ocultar; 404 desconocido. Administrador todas las categorías, Recepción solo alimentos, también en reintentos. Auditoría, borrado explícito de cargas propias y limpieza reintentable de Storage con JWT del operador; protección contra reutilizar imágenes eliminadas. [Contrato y errores/reintentos](ELIMINAR-PRODUCTOS.md). | Botón preparado por Thiago; pendiente publicar API y probar un producto desechable con sesión real. Si Storage falla tras borrar la ficha, repetir el mismo DELETE/id (502/504; 503 si falta configuración). Ocultar sigue siendo la vía habitual. Sin cambios de frontend. |
| F23 | **Terminado — backend, 27 nov; por publicar** | `imagenes: [{ id, url, orden }]` en catálogo, ficha por SKU y listado administrativo. Hasta ocho fotos, incluyendo cargas pendientes de agregar. POST imagen con `agregar=true` agrega sin cambiar principal; valor omitido/false reemplaza principal conservando secundarias. PUT `/api/productos/{id}/imagenes/orden` recibe todos los ids actuales y elige principal. DELETE `/api/productos/{id}/imagenes/{imagenId}` retira una secundaria; principal/única → 409. Administrador y Recepción solo alimentos. [Contrato](PRODUCTOS-GALERIA.md). | La vista previa ya lee `imagenes[]`. Thiago debe conectar el editor, conservar `idOperacion`/modalidad en reintentos y recargar ante 409. Retirar conserva el archivo y el historial; F22 limpia al borrar el producto completo. Migración aplicada; pendiente publicar y probar con sesión real. Sin cambios de frontend. |
| F24 | **Terminado — backend, 27 nov; por publicar** | **Tienda por categorías y marcas**, configurable por el cliente desde el panel (Rifles, Municiones, Cargadores, Accesorios, Tanques, Compresores, Repuestos, Ropa PCP… y las que agregue después). Tabla de categorías de tienda, `slug` y `visibleEnCarrusel` en marcas, y `categoriaTiendaId`/`marcaId` en productos. [Contrato completo](#f24--tienda-por-categorías-y-marcas-contrato-para-backend--27-nov-2026). | **Frontend listo (27 nov):** `/tienda` con botones de categoría, `/tienda/{categoria}` con marcas, `/tienda/{categoria}/{marca}` con productos; panel Productos → pestaña «Categorías» y listas Categoría/Marca en el editor; Marcas con «Mostrar en el carrusel». Backend F24 implementado y migración aplicada el 27 nov; pendiente publicar la API y comprobar integración con sesión real. |

### Módulos SIN API · el frontend muestra «pendiente» (sin datos simulados)

Regla acordada (21 nov 2026): en modo API **no se muestran datos de ejemplo**. Donde falta backend, la pantalla dice «pendiente» y explica qué falta. **Codex: cada fila es trabajo de backend por hacer; al terminar una, avisar el contrato para conectarla.**

| Módulo / pantalla | Qué falta en la API | Estado en el frontend |
| --- | --- | --- |
| **Reservas en el panel** (`/admin/reservas`) | Integrar listado/detalle y alta de mostrador ya disponibles en backend (F02/F03); llegada con QR de R06. | **Conectado y probado (24–26 nov):** lista, mapa, detalle y reserva de mostrador; búsqueda F18 conectada (Azure aún 404). Llegada con QR pendiente de R06; disponibilidad para personal pendiente de F19. |
| **Pago de reservas y pedidos** | R05 Mercado Pago (F01). | Pedido/reserva quedan «Pendiente de pago»; pedidos se cobran manualmente en el panel. |
| **Alimentos y bebidas** (`/alimentos`, `/admin/alimentos`) | Carta y gestión disponibles. **A01 implementa la base de órdenes/preparación/cobro local manual/entrega**, con inventario al entregar y migración aplicada. Desactivada para producción; OQ-05 sigue abierta para stock durante preparación, faltantes/no recogidos, barra y otros parámetros. [Carta](ALIMENTOS.md), [órdenes](ORDENES-ALIMENTOS.md). | Carta/gestión conectadas y probadas por Thiago el 25 nov. Las rutas nuevas `/api/ordenes-alimentos` y `/api/administracion/ordenes-alimentos` no están integradas; no cambian los pedidos generales anteriores ni incorporan pago online. |
| **Eventos e inscripciones** (`/eventos`, `/admin/eventos`) | Eventos informativos disponibles: GET público/listado/detalle, gestión solo Administrador POST/PUT/listado/detalle, imagen por URL y fechas UTC. Cupo, inscripciones, pago, inscritos/asistencia y reporte siguen pendientes de OQ-04. [Contrato](EVENTOS.md). | **Frontend conectado (25 nov, paso 11):** `/admin/eventos` (lista con filtros próximos/pasados/estado, editor con slug, resumen, inicio/fin en hora del club, lugar, imagen URL, Markdown con vista previa, publicar/borrador/archivar, 409 = recargar) y sitio `/eventos` (próximos/pasados) + detalle `/eventos/:slug`. Sin inscripción en línea: CTA a WhatsApp/contacto hasta OQ-04. Probado con Administrador (25 nov). |
| **Reportes por módulo** | Reservas y ventas alimenticias en pedidos existentes disponibles (F15); reporte de eventos depende del módulo pendiente. | Frontend conectado (paso 7, probado) a `/api/reportes/reservas` y `/api/reportes/alimentos`; eventos mantiene «pendiente». |
| **Cartas responsivas** | Texto oficial, firma, almacenamiento y validación por participante (R06). | En `/reservar` solo se acepta el reglamento; perfil y clientes muestran «pendiente». |
| **Galería** | API y gestión implementadas, migración aplicada; carga con sesión real pendiente. | **Conectado y probado con Azure (26 nov):** subir, publicar foto y álbum, visible en `/galeria`. |
| **Contenido institucional y avisos** (textos, avisos del sitio) | Contenido institucional y avisos públicos disponibles en backend; avisos con vigencia, orden, versión y auditoría, distintos de F10. [Textos](CONTENIDO-SITIO.md), [avisos](AVISOS-PUBLICOS.md). | Avisos públicos **conectados y probados (25 nov, paso 8)**: Contenido → Avisos y franja del sitio. Textos **conectado y probado (26 nov, paso C)**. |
| **Contacto** | Contacto implementado: recepción pública, bandeja solo Administrador, estados y notas. Notificaciones por correo pendientes de R07. Newsletter fuera del alcance actual por decisión de Kevin (24 nov); no es pendiente de entrega. [Contrato](CONTACTO.md). | Formulario `/contacto` y menú Mensajes (`/admin/contacto`) **conectados y probados (26 nov, paso C)**. Newsletter fuera de alcance. |
| **Plantillas de avisos** (WhatsApp/correo) | F10 disponible en backend para los tres campos actuales; envío R07 pendiente. | **Frontend conectado (25 nov, paso 9):** `/admin/configuracion` → «Plantillas de avisos» lee GET y guarda PUT con `version` (409 = recargar sin sobrescribir), con vista previa y contadores; Pedidos usa la plantilla «pedido listo» para WhatsApp (texto estándar si está vacía o si el rol es Recepción, ver F20). Se quitó «guardado en este navegador» y la sección de datos de demostración en modo API. |
| **Membresías: planes y cobro** | Planes CARTAQ y flujo de pago (F12; definición del cliente). | `/membresias` muestra «pendiente» (planes de ejemplo ocultos); renovación manual en `/admin/clientes`; el perfil muestra la vigencia real. |

### Contrato F17 · Marcas aliadas (implementado)

El frontend ya consume exactamente esto (`PCP-ShootingWeb/src/features/marcas/api.ts`). Si cambia algo, avisar para ajustarlo.

| Método y ruta | Acceso | Cuerpo / respuesta |
| --- | --- | --- |
| `GET /api/marcas` | Público | `[{ id, nombre, descripcion, url, logoUrl, orden }]` solo activas, ordenadas por `orden`. |
| `GET /api/administracion/marcas` | Administrador | Igual + `activo`, `version`; incluye inactivas. |
| `POST /api/administracion/marcas` | Administrador | `{ nombre (≤80, obligatorio), descripcion (≤60, opcional), url (https, opcional), activo, orden }` → la marca creada (201). |
| `PUT /api/administracion/marcas/{id}` | Administrador | Mismo cuerpo + `version` → la marca actualizada; 409 si la versión es vieja. |
| `POST /api/administracion/marcas/{id}/logo` | Administrador | `multipart/form-data`: `archivo` (PNG/JPG/WebP, ≤ 2 MiB) + `idOperacion` (UUID) → `{ logoUrl, version }`. Bucket público `pcp-marcas`. SVG excluido y selector alineado al contrato; reintentos antiguos no restauran logos retirados. |
| `DELETE /api/administracion/marcas/{id}/logo` | Administrador | 204. La marca queda «solo texto» (`logoUrl = null`). |

Reglas: sin borrado físico (se oculta con `activo=false`); `orden` entero (el panel intercambia el orden de dos marcas con dos PUT); auditoría como en los demás módulos; RLS y permisos revocados para `anon`/`authenticated` como el resto de tablas. Valores iniciales sugeridos: Airguns FV, Altaros, ZAN Projectiles, Donny FL, AEA, H&V (sin logo; el cliente los sube).

### Estado de integración del frontend (actualizado 26 nov 2026)

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
| Reservas en el panel (lista, mapa, detalle, mostrador; F18 conectado, Azure pendiente) | Probado (26 nov) |
| Subidas a Storage: galería, producto, marca, portada del blog, alimento | Probado (26 nov) |
| Avisos públicos, plantillas F10, eventos informativos, carta de alimentos | Probado (25 nov) |
| Alertas emergentes (un aviso por acción) | Probado (26 nov) |
| Textos del sitio y contacto (formulario + Mensajes) | Probado (26 nov) |
| Rol Recepción con cuenta real (menú, pedidos, reservas, alimentos) | Probado (26 nov, paso D) |
| Membresías públicas, inscripción a eventos, pedido de alimentos, pagos | Pendientes de F12, OQ-04, OQ-05 y R05 |

### Pruebas sugeridas de integración (frontend + API)

1. Pedido: cliente crea pedido → staff confirma pago → existencias bajan → «Marcar como recogido». Repetir confirmación con existencias insuficientes (debe dar 409 legible).
2. Reserva: elegir paquete visible, 2 personas, turno con 2 líneas libres → apartar → otra cuenta intenta las mismas líneas (409) → registrar participantes (adulto + menor con tutor) → la reserva aparece en «Mis reservas» → al vencer, `estado=Vencida` y las líneas vuelven a `Libre`.
3. Reintentos: repetir `POST /api/apartados` y `POST /api/reservas` con el mismo `idOperacion` (no duplica ni reinicia el contador).
4. Inventario: entrada de 10 → salida de 3 → ajuste −20 (debe dar 409 por existencias negativas) → mínimo 8 → aparece «Bajo mínimo».
5. Clientes: confirmar pago de un pedido del cliente → registrar membresía de 12 meses con ese pago → el cliente ve «Socio CARTAQ» en su perfil; reutilizar el mismo pago (409).
6. Configuración: cambiar actividad/margen y comprobar que `/api/disponibilidad` refleja la nueva grilla; bloqueo global de 1 hora → turnos afectados sin disponibilidad.

## 9. OQ — Open Questions

Dudas abiertas que requieren aclaración del equipo antes de implementar. Mantener IDs estables y registrar la respuesta, su fecha y los contratos afectados al resolverlas. Una propuesta de backend no equivale a una regla aprobada.

Kevin confirmó el 24 nov que no había respuestas nuevas. **El 27 nov sí se recibieron respuestas**: ver [registro contrastado con el cuestionario](DEFINICIONES-CLIENTE-2026-11-27.md). Las secciones conservan el contexto histórico; las actualizaciones fechadas del 27 nov prevalecen sobre las listas antiguas. No volver a pedir las reglas ya contestadas; tampoco cerrar los puntos todavía ambiguos o sin respuesta.

| ID | Tema | Quién debe resolver | Trabajo afectado |
| --- | --- | --- | --- |
| OQ-01 | Abandonar selección frente a cancelar reserva | Frontend; cliente si propone excepciones | F06 |
| OQ-02 | **Parcialmente aclarada, 27 nov:** plan único anual, beneficios solo titular, renovación anticipada al terminar vigencia. Faltan importes, documento de beneficios y alcance de consumos/inscripción. | Cliente; equipo decide integración del cobro | F12 y tarifas de miembros |
| OQ-03 | **Parcialmente aclarada, 27 nov:** casilla y firma dibujada por adulto/tutor, por reserva, revisión de Recepción y consulta por actores autorizados. Faltan texto oficial y retención; «por cada visita» no responde 2.5. | Cliente; frontend coordina firma y escaneo | F03/R06, última visita F14 y asistencia F15 |
| OQ-04 | **Parcialmente aclarada, 27 nov:** cupo por persona, inscripciones múltiples pagadas, apartado temporal, QR y traslado por cancelación/reprogramación. Faltan edad/menores, minutos de apartado, límites de grupo, líneas y detalle de traslados. | Cliente y frontend | Módulo de eventos y su reporte F15 |
| OQ-05 | **Parcialmente aclarada; A01 base implementada, 27 nov:** preparación, registro manual del cobro local y entrega con inventario. Desactivada para producción: faltan protección de stock durante preparación, permisos de barra, ambigüedad 4.5 y faltantes/no recogidos. Pago online y transición desde pedidos generales por integrar. | Cliente y equipo; Thiago integra frontend | Activación de A01, ampliaciones de alimentos y F15 |

### OQ-01 · F06: abandonar un apartado o cancelar una reserva

**Estado:** abierta, 24 nov 2026. **Responsable de aclaración:** frontend. **Bloque afectado:** F06; implementación pendiente hasta definir su alcance.

**Origen del inconveniente.** En la lista de solicitudes de frontend, F06 pide «Cancelar un apartado o reserva pendiente» para liberar líneas antes del vencimiento, con un botón «Cancelar» en el paso de pago y en la confirmación. Sin embargo, la regla confirmada con el cliente establece que no habrá cancelaciones ni reembolsos; también está registrada en las reglas de este roadmap y en [RESERVAS-DISENO.md](RESERVAS-DISENO.md). La solicitud mezcla abandonar una selección temporal con cancelar una reserva, por lo que no podemos implementarla como una cancelación general.

Hay tres momentos con consecuencias distintas:

| Momento | Inconveniente / aclaración necesaria |
| --- | --- |
| Apartado o reserva pendiente sin haber iniciado checkout | Liberar las líneas permitiría abandonar la selección sin esperar al vencimiento. Confirmar si este es el único caso que frontend necesita y si incluye ambos registros. |
| Checkout iniciado, pago pendiente o conciliación incompleta | Cerrar la ventana o volver del proveedor no demuestra que no exista un cobro. Liberar líneas podría permitir otra reserva antes de recibir la aprobación del pago original. Debe conservarse la protección de capacidad mientras se verifica el resultado. |
| Reserva pagada / pantalla de confirmación | Cancelarla contradice la regla del cliente. Aclarar si el botón solo pretende cerrar la pantalla o volver al inicio, sin alterar la reserva, o si frontend está solicitando una excepción de negocio. |

**Respuesta que necesitamos de frontend:** indicar en qué pantallas y estados aparece el botón, qué acción espera el usuario y si «confirmación» significa reserva ya pagada o un resumen previo al pago. Confirmar si F06 puede limitarse a abandonar la selección antes de iniciar checkout; en la confirmación pagada, precisar si basta con navegación sin cambios en backend. Si se pretende cancelar después de iniciar el pago o una reserva pagada, señalarlo expresamente para validarlo con el cliente antes de cambiar su regla.

**Propuesta de backend, todavía no aprobada:** limitar F06 a «Liberar apartado antes de iniciar el pago» y conservar las reservas pagadas, sin cancelación ni devolución. El alcance exacto para una reserva pendiente creada desde ese apartado también requiere aclaración. No se han creado endpoints ni cambiado estados o reglas para F06.

**Criterio de cierre:** registrar aquí la respuesta de frontend y el alcance acordado; si hay una excepción a la política del cliente, obtener y documentar su definición. Después actualizar F06 y el contrato de API antes de implementar.

### OQ-02 · F12: planes y cobro de membresías CARTAQ

**Actualización del 27 nov:** un plan, inscripción única y cuota/vigencia anual; inicio al pagar anualidad y renovación anticipada desde el vencimiento actual. Beneficios solo titular; Recepción registra/verifica equipo en cada visita. Faltan importes de anualidad/posible inscripción, documento «intro», condiciones de carga incluida/exceso y significado exacto de $250 para invitados. La periodicidad anual no confirma débito automático; esa elección técnica sigue en el equipo según Kevin. [Detalle y límites de interpretación](DEFINICIONES-CLIENTE-2026-11-27.md#cartaq--oq-02--f12). El contexto siguiente es histórico, no una solicitud de repetir lo ya respondido.

**Estado:** abierta, 24 nov 2026. **Responsable:** cliente; frontend debe trasladar la definición acordada al equipo backend.

**Origen del inconveniente.** Hoy se puede registrar una vigencia con un pago de pedido confirmado, pero no existe un catálogo de planes que permita al cliente comprar una membresía y activar su vigencia automáticamente. Se investigó el cobro recurrente por petición del equipo, mientras que el alcance inicial contemplaba renovación con pago completo sin recurrencia. No hay una decisión definitiva que permita sustituir un flujo por otro.

**Respuesta necesaria:** nombres de planes, precio/moneda, duración e inicio de vigencia; beneficios y requisitos (incluido equipo propio), si son personales o aplican a acompañantes; condiciones para renovar antes/después del vencimiento; y elección explícita entre pago único con renovación manual o cobro recurrente. Si eligen recurrencia, definir frecuencia, cancelación de renovaciones, impago y efecto sobre la vigencia ya pagada. No trasladar automáticamente la política de reservas a suscripciones.

**Qué queda pendiente:** catálogo de planes, compra/checkout de membresía, activación automática y cualquier suscripción. La gestión de vigencias ya disponible se conserva. **Cierre:** registrar respuesta y reglas acordadas, actualizar F12/contrato y después implementar; no crear precios o beneficios de ejemplo como definitivos.

### OQ-03 · F03/R06: responsivas, llegada y QR

**Actualización del 27 nov:** aceptación mediante casilla y firma dibujada, cada adulto firma; tutor firma por menor con sus nombres y edad del menor. Firma por reserva, revisión de responsiva por Recepción y consulta/descarga de Administrador/Recepción/cliente dentro de su acceso autorizado. Texto oficial se enviará después. **2.5 sigue pendiente:** pregunta retención; «por cada visita» no indica años/plazo. Falta precisar acceso del organizador en grupos. [Detalle](DEFINICIONES-CLIENTE-2026-11-27.md#responsivas--oq-03--r06). No habilitar admisión con un texto ficticio ni eliminar documentos al terminar una visita. El contexto siguiente es histórico.

**Estado:** abierta, 24 nov 2026. **Responsable:** cliente para contenido y procedimiento; frontend para concretar captura/firma y escaneo.

**Origen del inconveniente.** La llegada exige pago verificado y responsiva por participante. Aceptar el reglamento en el wizard actual no equivale a firmar esa responsiva. Falta definir qué documento se acepta y cómo queda evidencia de quién lo firmó, especialmente cuando el participante es menor. Por eso tampoco se puede calcular una última visita real a partir de una reserva pagada.

**Respuesta necesaria:** texto/documento oficial y su versión; mecanismo de aceptación/firma (digital, carga de documento o validación presencial); quién firma por cada adulto/menor y cómo se vincula al tutor; si se firma por reserva o tiene vigencia reutilizable; datos/evidencias a conservar, plazo de conservación y acceso/descarga. Frontend debe indicar en qué pantalla se firma y cómo Recepción revisa la evidencia antes de admitir.

**Reglas ya confirmadas, no volver a decidir:** llegada desde tres minutos antes y tolerancia de tres minutos; validar pago y responsivas; al admitir, registrar utilización y descontar consumibles una sola vez. No asistió conserva el cobro, libera la línea y no consume inventario. La cuenta del cliente es obligatoria.

**QR pendiente:** acordar con frontend el formato y recorrido de escaneo/búsqueda manual. Propuesta técnica sin implementar: identificador de reserva sin datos personales, distinto del enlace de pago; la lectura del QR solo localiza la reserva y nunca sustituye las validaciones del servidor ni los permisos de Recepción/Administrador.

**Qué queda pendiente:** evidencia de responsivas, admisión/llegadas, ausencias y QR; última visita de F14 y asistencia en F15. **Cierre:** disponer de contenido y procedimiento acordados, documentar contrato y luego implementar/pruebar. No habilitar llegada saltándose responsivas para completar otro pendiente.

### OQ-04 · Eventos e inscripciones

**Actualización del 27 nov:** todos pagados, cupo por persona, una persona inscribe a varios, apartado temporal, asistencia por QR, pase a otro evento si el organizador cancela e inscripción trasladada a nueva fecha al reprogramar. Pendientes expresos: edades/menores (3.4), plazo de apartado (3.8; prefieren menos de 10 min sin fijarlo), líneas afectadas (3.9), límite por grupo y condiciones del traslado. [Detalle](DEFINICIONES-CLIENTE-2026-11-27.md#eventos--oq-04). No se implementaron esas operaciones con esta actualización; el contexto siguiente es histórico.

**Estado:** abierta, 24 nov 2026. **Responsables:** cliente y frontend.

**Origen del inconveniente.** Existen pantallas, pero falta acordar el modelo operativo de inscripciones. El 24 nov Kevin autorizó desarrollar únicamente eventos informativos: catálogo/gestión, fechas, descripción, imagen por URL y publicación. Esta entrega no implica inscripción, reserva de cupo ni cobro. Generar un reporte operativo antes de acordar y registrar esas operaciones produciría datos sin una definición común.

**Respuesta necesaria:** datos del evento, fecha/hora/zona/duración y ubicación; cupo y unidad de inscripción (persona/grupo), edades y tutor; inscripción con cuenta, pago gratuito/de pago y plazo de apartado si corresponde; participantes y comprobación de asistencia; relación con líneas y disponibilidad de reservas ordinarias. Precisar si cancelación por organizador, reprogramación y devoluciones tienen reglas propias, sin asumir que la política de reservas aplica a eventos. Frontend debe identificar pantallas y estados que necesita gestionar.

**Qué queda pendiente:** inscripción/pago, cupos, inscritos/asistencia, relación con reservas/líneas y reporte F15. Catálogo y gestión informativa disponibles según [EVENTOS.md](EVENTOS.md); frontend por integrar. **Cierre:** acordar el flujo operativo y sus reglas, documentar contrato y después implementar; no tratar datos de demostración como eventos reales. OQ-04 permanece abierta.

### OQ-05 · Alimentos: cobro, preparación y entrega

**Desarrollo A01, 27 nov:** base implementada con rutas propias, estados de preparación/pago separados, registro manual de cobro local y entrega transaccional/idempotente con inventario. Se publica **desactivada para producción** hasta acordar protección de stock durante preparación y casos de faltantes/no recogidos. Crear comprueba existencias sin reservarlas; no garantiza suministro a todas las órdenes abiertas. No se creó rol Barra ni cancelaciones/reembolsos. El equipo/Thiago deben coordinar además la transición desde alimentos en `/api/pedidos`, que conserva su comportamiento anterior. [Contrato](ORDENES-ALIMENTOS.md). OQ-05 sigue abierta.

**Actualización del 27 nov:** acepta pago online/local; local con tarjeta o transferencia. Preparar sin exigir pago previo; cobro local al entregar. Bebidas/snacks/comida, compra sin reserva, recogida en Recepción y flujo Recibido → En preparación → Listo → Entregado. Recepción/barra avanzan estados; stock se descuenta solo al entregar. Política declarada: no cancelaciones, reembolsos ni devoluciones, y señalar agotados desde Administración. **No confundir entrega con confirmación de pago de la tienda actual.** Falta decidir acceso de barra, interpretar 4.5 («No, todo el horario del club» frente a horario restringido), proteger stock aceptado y resolver faltantes posteriores/no recogida. [Detalle](DEFINICIONES-CLIENTE-2026-11-27.md#alimentos-y-bebidas--oq-05). Las opciones A/B/C y preguntas siguientes son históricas: la respuesta nueva combina pago online/local y preparación sin pago previo; no habilitar automáticamente B con la antigua exigencia de pago antes de preparar.

**Estado:** abierta, 24 nov 2026. **Responsables:** cliente y frontend.

**Origen del inconveniente.** Ya existen productos categoría Alimento, inventario, pedidos generales y reporte de sus partidas pagadas/recogidas. Esto no define el flujo específico de cocina ni cuándo se acepta y prepara una orden de alimentos.

**Respuesta necesaria:** pago en línea, en el club o ambos y forma de registrar/verificar el cobro; si se prepara antes o después de confirmar pago; carta/subcategorías, disponibilidad y horarios; relación de la orden con reserva, cliente o recogida; estados y quién puede avanzar recibido → preparación → listo → entregado; momento del descuento de stock y tratamiento de falta de existencias, pedidos no recogidos o cancelaciones antes de preparar. Frontend debe describir el recorrido esperado en `/alimentos` y `/admin/alimentos`.

**Propuesta de frontend (25 nov 2026) — para decidir con el cliente y Kevin.**

Hoy la carta `/alimentos` es **solo de consulta** (precio, foto, agotado). No se piden alimentos desde la web **ni al reservar**: se piden y pagan en barra. La cotización sí incluye el pedido desde el sitio con estados recibido → en preparación → listo → entregado; agregar alimentos a la reserva (combo) **no** está en la cotización y sería un extra.

| Opción | Recorrido | Backend (Kevin) | Frontend |
| --- | --- | --- | --- |
| **A. Pedido web · pago en barra** *(recomendada)* | Durante su visita el cliente pide desde el celular (a su línea o en barra). Recepción ve el pedido, lo avanza recibido → en preparación → listo → entregado y cobra al entregar. | Orden de alimentos con estados de cocina, permisos de Recepción para avanzar estados, registro del cobro en barra y descuento de stock al entregar (o al cobrar). | Pantallas ya diseñadas en modo demo (`/alimentos` con carrito y seguimiento; `/admin/alimentos` con tablero por estado y venta en mostrador): solo conectar. |
| **B. Pedido web · pago en línea** | Igual que A, pero se paga con Mercado Pago al pedir; cocina prepara solo pedidos pagados. | Lo de A + cobro con Mercado Pago (depende de R05) y reglas de reembolso si se agota. | Lo de A + botón de pago. |
| **C. Solo carta** | Como hoy: consultar en la web, pedir y pagar en barra. | Nada. | Nada (ya conectado). |

**Preguntas para el cliente:**
1. ¿Opción A, B o C? (¿Se cobra en barra, en línea o ambas?)
2. ¿Se puede pedir solo si el cliente está en el club / tiene reserva ese día, o cualquiera?
3. Entrega: ¿a la línea de tiro, en barra o ambas?
4. Horario de cocina (¿igual al del club?) y categorías de la carta (bebidas, snacks, comida…).
5. Si un producto se agota después de pedir o el cliente no recoge: ¿qué pasa?

**Para Kevin (cuando el cliente responda):** contrato de órdenes de alimentos con estados, permisos de Recepción, momento del descuento de stock y, si aplica, cobro. Mientras tanto no se cambia nada en el frontend: la carta sigue como consulta.

**Qué queda pendiente tras A01:** activar el flujo operativo cuando se acuerden protección de stock, faltantes/no recogidos y permisos de barra; integración de pago online, frontend, transición desde pedidos anteriores, subcategorías/horarios y métricas propias. La carta básica sigue disponible según [ALIMENTOS.md](ALIMENTOS.md). El reporte existente conserva sus partidas en pedidos generales pagados/recogidos y no representa las nuevas órdenes de cocina. El contrato de la base ya está documentado, pero **OQ-05 permanece abierta y el módulo comercial no se declara cerrado**.

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

## Avance F08/F11/F13/F15 · 23 nov 2026

Stock al crear pedidos, existencias/agotado públicos, historial global de inventario con autor y CSV legibles implementados. F08 conserva pendientes los estados adicionales; F15 conserva pendientes los nuevos reportes por módulo. Sin migración. La prueba de falta de stock al pagar ahora retira existencias después de crear el pedido, para seguir comprobando esa segunda validación.

Verificación: **290 pruebas locales aprobadas, cero fallidas y 40 PostgreSQL omitidas**. Diez pruebas PostgreSQL específicas aprobadas por separado (nueve de escrituras sin concurrencia y una de stock/autor/reportes). Concurrencia omitida por indicación del usuario. Se corrigió la traducción SQL del filtro de autor antes de cerrar. Modelo sin cambios de esquema pendientes.

F09 requiere definir tallas/variantes, SKU e inventario por variante y relación con pedidos. F10 se normaliza como plantillas de avisos; marcas queda solo en F17. F10 no tiene bloqueo técnico y puede implementarse separada del envío R07. Trabajo disponible: F10/F17 y F16; F06 requiere coordinar cancelación con pagos iniciados.

## Cierre F10 · 23 nov 2026

Persistencia de correo remitente y dos plantillas actuales implementada, solo Administrador, versión obligatoria y auditoría. Valores iniciales vacíos. Migración `20261123213918_ConfiguracionAvisos` aplicada en Supabase. Frontend por conectar; no se envían mensajes ni se configura proveedor en este bloque. [Contrato](AVISOS.md).

Verificación: **296 pruebas locales aprobadas, cero fallidas y 41 PostgreSQL omitidas**; una prueba PostgreSQL específica aprobada por separado (migración, RLS, persistencia, auditoría y límites). Concurrencia omitida por indicación del usuario. Modelo sincronizado con migración. Revisión sin hallazgos importantes.

Definición posterior a los pendientes anteriores: F09 ya tiene tallas S/M/L/XL, colores como productos independientes y precio común del producto; implementación pendiente. F17 (marcas) y F16 (portadas del blog) continúan disponibles como siguientes mejoras. R05 sigue esperando credenciales/HTTPS y pruebas integradas.

## Cierre F09 · 23 nov 2026

Backend de tallas implementado: catálogo/ficha/gestión, stock por talla, movimientos e historial, pedidos con varias tallas del mismo producto, pagos y nombres de talla en checkout/reportes. Mantiene precio común y productos independientes por modelo/color. Activación explícita para conservar la compatibilidad; cambiar el modo exige saldo cero por grupo y ausencia de pedidos pendientes. No se asignan tallas al stock histórico.

Migración `20261123214835_TallasPlayeras` aplicada en Supabase. Frontend por conectar según [contrato](TALLAS.md). Verificación: **300 pruebas locales aprobadas, cero fallidas y 42 PostgreSQL omitidas**, más una prueba PostgreSQL específica aprobada por separado para migración/datos anteriores y flujos por talla. Concurrencia omitida por indicación del usuario. Modelo sincronizado; revisión sin hallazgos importantes. Siguientes mejoras disponibles: F17 y F16; R05 sigue pendiente de credenciales, HTTPS y pruebas integradas.

## Cierre F17/F16 · 23 nov 2026

Marcas y carga de logos, junto con portadas del blog, implementadas. Migración `20261123223905_MarcasYPortadasBlog` aplicada: tablas con RLS y buckets públicos `pcp-marcas`/`pcp-blog` con políticas ligadas a cargas reservadas por Administrador. Logos 2 MiB, portadas 5 MiB, PNG/JPG/WebP; SVG excluido del contrato y selector. Los archivos anteriores se conservan, y las operaciones antiguas no restauran imágenes sustituidas/retiradas. Se ajustó el panel de marcas para recargar versiones y reintentar sin editar otra vez los mismos campos.

Verificación: **310 pruebas locales aprobadas, cero fallidas y 44 PostgreSQL omitidas**. Tres comprobaciones PostgreSQL específicas aprobadas por separado (migración/cargas simuladas, provisión real de Storage y migración anterior del blog). Modelo sincronizado y revisión sin hallazgos importantes. TypeScript/Vite/prerender compilan. Concurrencia omitida por indicación del usuario.

Pendiente prueba de carga con JWT real y conexión del selector de portada del blog. Marcas ya dispone del panel/consumo público existente. [Contrato](MARCAS-Y-PORTADAS.md). R05 continúa pendiente de credenciales, HTTPS y pruebas integradas; F06, F12, F14 y los reportes restantes de F15 siguen abiertos con sus dependencias.

## Avance R05 · 24 nov 2026

Conciliación programada implementada para pedidos de Mercado Pago y reservas, con configuración validada, lotes, timeout, rotación y progreso persistente. Excluye pagos manuales y conserva la ocupación durante revisiones parciales o fallidas. Reabrir un checkout rechazado lo vuelve elegible; un intento posterior pendiente puede reemplazar un rechazo anterior sin sobrescribir confirmaciones. La conciliación manual puede completar la protección de una reserva interrumpida.

Migración `20261124141606_ConciliacionProgramada` aplicada en Supabase. Clasifica pagos anteriores solo cuando existe una preferencia Mercado Pago. **Servicio deshabilitado por defecto y no activado en este entorno**; depende de credenciales y de mantener la API encendida. No tiene exclusión distribuida entre réplicas; las confirmaciones reutilizan la idempotencia existente.

Verificación: **324 pruebas locales aprobadas, cero fallidas y 45 PostgreSQL omitidas**; una prueba PostgreSQL específica aprobada por separado en esquema temporal para migración, datos anteriores, exclusión de pagos manuales y confirmaciones sin doble descuento. Modelo sin cambios pendientes y revisión realizada. Concurrencia omitida por indicación del usuario. No hubo compras sandbox reales ni cambios en frontend.

R05 sigue abierto por credenciales de Mercado Pago, publicación HTTPS, conexión frontend y pruebas sandbox. F06/R06 deben coordinar cancelaciones/llegadas con pagos pendientes y `ConciliacionEnCurso`; F12, F14 y reportes restantes F15 conservan sus dependencias. [Configuración y operación](MERCADO-PAGO.md).

## Actualización a .NET 10 · 24 nov 2026

API y pruebas migradas a `net10.0`, paquetes Microsoft/EF Core 10.0.15 y proveedor PostgreSQL Npgsql 10.0.4. SDK estable .NET 10 seleccionado mediante `global.json` y herramienta EF reproducible con `dotnet tool restore`. Se mantiene User Secrets y el contrato del frontend. No se cambiaron tablas, datos ni migraciones históricas; EF no detecta cambios de modelo pendientes.

Verificación: **324 pruebas locales aprobadas, cero fallidas y 45 PostgreSQL omitidas**; cuatro pruebas PostgreSQL específicas aprobadas por separado y secuencialmente para conciliación, reservas, tallas/inventario y flujo HTTP administrativo/auditoría. Concurrencia omitida por indicación del usuario. Compilación sin advertencias y publicación Release correctas; artefactos publicados arrancados localmente, Swagger HTTP 200 con 70 rutas. Sin paquetes vulnerables detectados en la API por NuGet al verificar. No se desplegó a Azure ni se hicieron pagos sandbox reales.

Siguiente: configurar/publicar App Service con .NET 10 y verificar login real, CORS y webhook HTTPS cuando estén las credenciales. F06 continúa pendiente de la definición de cancelación anterior. [Guía de ejecución y publicación](DOTNET10.md).

## Corrección del flujo Azure · 24 nov 2026

Corregido `MSB1003` en `.github/workflows/main_pcpshootingapi.yml`: compilación y publicación apuntan explícitamente a `PCP-Shooting/PCP-Shooting/PCP-Shooting.csproj`, en lugar de buscar un proyecto en la raíz. SDK fijado a la familia 10.0; artefactos publicados y descargados en carpetas explícitas, con error si no existen archivos. Se conservan aplicación, slot y autenticación Azure existentes. Comandos de compilación y publicación comprobados localmente, sin errores ni advertencias, con DLL generada. La ejecución remota y el despliegue siguen pendientes de comprobar en GitHub Actions.

## Avance F14/F15 · 24 nov 2026

F14 agrega reservas web/mostrador al historial administrativo del cliente, con `paginaReservas`, orden estable, aislamiento por usuario y los estados actuales de pago. F15 agrega `GET /api/reportes/reservas` en JSON/CSV: filtros de usuario, estado y rango UTC sobre el inicio de actividad, total confirmado sobre el conjunto filtrado, límite y protección CSV existentes. Ambas consultas requieren Administrador activo. La ficha y el reporte respetan pagos pendientes y `ConciliacionEnCurso`; no interpretan una reserva pagada como visita realizada.

Sin migración; modelo sincronizado. Frontend por conectar según [contratos](FRONTEND-CONTRATOS.md) y [reportes](REPORTES.md). Despliegue de la base .NET 10 confirmado por Kevin; estos cambios nuevos todavía deben subirse y desplegarse.

Verificación: **335 pruebas locales aprobadas, cero fallidas y 45 PostgreSQL omitidas**; cuatro pruebas PostgreSQL específicas aprobadas por separado y secuencialmente (historial con reservas y reporte en los tres estados, contratos administrativos, tienda/inventario y tallas). Se corrigió y comprobó la traducción SQL de los filtros antes de cerrar. Revisión sin hallazgos materiales; concurrencia omitida por indicación del usuario.

F14 sigue parcial: correo de Supabase Auth y última visita pendientes (esta última depende de R06). F15 sigue parcial: reportes de alimentos/eventos y datos de llegada/ausencia pendientes; el modelo actual no tiene entidades de eventos ni registros de visitas. F12 conserva la dependencia de planes y reglas CARTAQ definitivos. F06 sigue en OQ-01, sin cambios en cancelaciones ni reembolsos.

## Avance F14/F15 · correo y alimentos · 24 nov 2026

F14 agrega `correo` a la raíz del historial administrativo. Se consulta por UUID exacto en Supabase Auth mediante la conexión del servidor existente, sin duplicar el correo en perfiles ni modificar cuentas. Devuelve nulo para cuenta ausente/eliminada o sin correo; errores de conexión/permisos se propagan como errores. Solo Administrador activo; los otros roles no llegan a consultar Auth. La última visita continúa pendiente de R06.

F15 agrega `GET /api/reportes/alimentos` JSON/CSV para partidas de categoría Alimento en pedidos Pagado/Recogido. Filtra usuario, producto y fechas de creación del pedido; conserva cantidades/precios unitarios históricos, suma solo las partidas alimenticias y excluye ropa de pedidos mixtos. CSV mantiene exportación completa filtrada, límite 10000 y protección de fórmulas. Categoría/nombre/SKU actuales del producto: la clasificación histórica no es inmutable. Este reporte no sustituye el módulo pendiente de preparación/entrega de alimentos ni es un reporte por fecha de cobro.

Verificación: **347 pruebas locales aprobadas, cero fallidas y 47 PostgreSQL omitidas**. Cuatro pruebas PostgreSQL específicas aprobadas por separado y secuencialmente: alimentos, consulta real de correo de Auth (solo lectura), historial y contratos administrativos. Compilación Release sin errores/advertencias; EF sin cambios de modelo pendientes. Sin migración; revisión sin hallazgos materiales. Concurrencia omitida por indicación del usuario. Frontend por conectar y cambios por desplegar.

Pendientes actuales: F06/OQ-01, F12/planes CARTAQ, última visita y llegadas/ausencias R06, eventos y su reporte F15, flujo operativo de alimentos e integración/pruebas reales de Mercado Pago. [Contrato actualizado](FRONTEND-CONTRATOS.md).

## Diagnóstico Azure · 24 nov 2026

HTTPS responde y CORS permite http://localhost:5173 después de configurar las variables de entorno. Las rutas protegidas sin sesión devuelven 401. Catálogo y ventana de disponibilidad siguen devolviendo 500; falta la excepción del servidor para identificar la causa. No dar por verificada la conexión PostgreSQL ni el login real del despliegue.

Se agregó Microsoft.Extensions.Logging.AzureAppServices 10.0.15 y AddAzureWebAppDiagnostics para integrar ILogger con los registros de App Service. Debe publicarse el cambio, conservar activo el registro de aplicaciones en sistema de archivos y reproducir la petición con Secuencia de registro conectada. No se activaron páginas públicas de errores detallados ni se cambiaron secretos. Ver docs/DOTNET10.md.

Verificación local: 347 pruebas aprobadas, cero fallidas y 47 PostgreSQL omitidas; concurrencia excluida por indicación del usuario. Sin cambios de modelo ni migración. La captura remota y la causa del 500 quedan pendientes de comprobar después del despliegue.

### Resolución de conexión Azure · 24 nov 2026

Kevin corrigió un salto de línea accidental en la contraseña de ConnectionStrings__Postgres. La excepción de formato de Npgsql quedó resuelta. Comprobación remota posterior: GET /api/productos y /api/disponibilidad/ventana devuelven 200; /api/usuarios/yo y /health/postgres sin sesión devuelven 401 con desafío Bearer. Preflight para http://localhost:5173 devuelve 204 y permite ese origen. Las consultas públicas verifican acceso a PostgreSQL desde Azure; no se ejecutó el health check administrativo autenticado.

Pendientes de comprobar en el despliegue: login con JWT real, permisos por rol con sesiones reales, subida a Storage y frontend conectado a esta URL. No se modificaron productos, cuentas ni otros datos durante estas comprobaciones.

### Frontend local conectado a Azure · 24 nov 2026

Configurado PCP-ShootingWeb/.env.local con VITE_DATA_SOURCE=api y VITE_API_URL apuntando al App Service publicado, conservando configuración Supabase existente. El archivo local está ignorado por Git; la plantilla .env.example incluye la URL pública de Azure. Compilación TypeScript/Vite/prerender aprobada y servidor local en http://localhost:5173 comprobado con HTTP 200; módulo API servido por Vite contiene el destino Azure. Login real todavía pendiente de comprobar por el usuario.

Vercel necesita variables propias (datos en modo api, URL Azure, URL y publishable key Supabase); configurar su dominio exacto como origen adicional en Azure y URLs de retorno en Supabase. No se publicó frontend desde esta sesión. Guía en docs/DOTNET10.md.

## Galería implementada · 24 nov 2026

Diseño y ejecución aprobados por Kevin. Backend con álbumes/fotos, versiones, orden, portada derivada de la primera foto visible, publicación/ocultación, paginación y carga idempotente. Administración exclusiva para Administrador activo. JPG/PNG/WebP hasta 5 MiB; sin videos ni borrado físico. Bucket público pcp-galeria: ocultar quita de listados, no revoca enlaces anteriores, según lo aprobado.

Migración 20261124171633_GaleriaInicial aplicada en Supabase después de comprobar esquema, RLS y función/política de Storage. Verificada la provisión real del bucket y permisos. Frontend conectado en /galeria y Contenido → Galería, con carga múltiple por archivo, recuperación de fichas sin imagen, reemplazo, edición de metadatos, orden y publicación; filtros/visor públicos sin datos ficticios.

Verificación: 355 pruebas locales aprobadas, cero fallidas y 50 PostgreSQL omitidas en esa ejecución; tres comprobaciones PostgreSQL específicas aprobadas por separado. Modelo sin cambios pendientes y compilación TypeScript/Vite/prerender correcta. Revisión independiente corrigió filtro seleccionado, duplicación de páginas y conservación de aviso/borrador ante conflicto. Concurrencia omitida por indicación del usuario.

Comprobación en navegador: galería vacía contra API local y panel denegado con la sesión no administrativa disponible. Sin contenido ficticio público ni carga con JWT real de Administrador. Pendientes: desplegar API/frontend, primera carga/publicación con Administrador y comprobación visual completa con fotos reales, incluida móvil. El frontend local quedó nuevamente apuntando a Azure. Contrato y límites en docs/GALERIA.md; registro de ejecución en docs/GALERIA-PLAN.md. Los OQ de otros módulos permanecen abiertos.

## Contenido institucional editable · 24 nov 2026

API pública y gestión solo Administrador para textos de Nosotros/seguridad, testimonio opcional, pie del sitio, contacto, mapa y redes. Configuración única JSONB tipada, validación de documento completo/enlaces/teléfonos, versión y auditoría; migración 20261124173746_ContenidoInstitucional aplicada en Supabase con RLS. No se inicializa un testimonio ficticio ni se cambia la configuración operativa de reservas.

Panel Contenido → Textos conectado; guardar publica y actualiza la pestaña actual. Fuente compartida en Nosotros, Contacto, pie, menú lateral, portada, recogida de pedidos y enlaces de soporte. Contactos/redes actualizados también en JSON-LD de inicio; horario informativo separado de turnos y horario SEO estructurado. Si la API pública falla se conserva el contenido conocido del sitio; otras pestañas deben recargar. Sin cambios en formularios de contacto, correo de avisos ni Auth.

Verificación: 367 pruebas locales aprobadas, cero fallidas y 51 PostgreSQL omitidas; cinco comprobaciones PostgreSQL aprobadas por separado y secuencialmente (contenido, galería x3 e historial). Frontend compila y modelo sincronizado. El fixture PostgreSQL ahora ejecuta los scripts generados mediante ADO para soportar JSON literal sin interpolación de formato. Revisión corrigió datos de contacto obsoletos en JSON-LD. Concurrencia omitida por indicación del usuario.

Pendientes: desplegar API/frontend y comprobar edición con sesión real de Administrador y recorrido visual completo. Avisos públicos y formulario de contacto siguen disponibles como próximos desarrollos; los OQ ajenos permanecen abiertos. Contrato en docs/CONTENIDO-SITIO.md.

## Formulario y bandeja de contacto · 24 nov 2026

Recepción pública implementada con nombre, correo, teléfono opcional, motivo y mensaje; exige aceptación declarada de privacidad y registra su fecha UTC. UUID estable evita duplicar reintentos del mismo contenido; respuestas públicas no exponen estado de atención ni notas. Límite de 5 intentos por ventana fija de 5 minutos por IP remota/instancia, campo trampa y tamaño de petición limitado. Sin guardar IP ni confiar directamente en X-Forwarded-For del navegador.

Administración dispone de listado paginado con búsqueda/filtros, detalle y seguimiento Pendiente/Atendida/Archivada con notas internas y control de versión. Solo Administrador activo; Recepción no tiene acceso a esta bandeja. Archivar conserva el registro. No hay envío automático de correos ni suscripción al newsletter.

Frontend /contacto envía a la API real, conserva datos/operación al fallar y confirma únicamente tras respuesta satisfactoria; campos deshabilitados durante envío. Nuevo menú Mensajes y /admin/contacto conectados con recuperación de errores y conservación de notas ante conflictos. Migración 20261124175502_SolicitudesContacto aplicada en Supabase con RLS y revocación de acceso directo.

Verificación: 377 pruebas locales aprobadas, cero fallidas y 52 PostgreSQL omitidas; una prueba PostgreSQL específica aprobada por separado para migración, RLS, flujo HTTP, filtros, idempotencia y auditoría. Frontend compila; modelo sincronizado y revisión independiente sin hallazgos materiales. Formulario público revisado visualmente en local sin enviar datos a producción. Concurrencia omitida por indicación del usuario.

Pendientes: desplegar API/frontend y comprobar envío y atención desde el sitio publicado con Administrador real; revisión visual móvil completa. Avisos públicos se completaron después en backend (ver bloque siguiente); correo/notificaciones siguen pendientes de R07. Newsletter excluido del alcance actual por decisión posterior de Kevin. Contrato en docs/CONTACTO.md.

## Avisos públicos · 24 nov 2026

**Terminado en backend.** API pública GET `/api/avisos-publicos` y gestión solo Administrador GET/listado/detalle, POST y PUT `/api/administracion/avisos-publicos`. Título opcional, texto plano, activación, orden y fechas UTC opcionales de inicio/fin; inicio incluido y fin excluido. Estados calculados Inactivo/Programado/Vigente/Vencido, filtros administrativos, paginación, versión obligatoria al editar y auditoría. Desactivar conserva el registro. Los avisos no cambian la disponibilidad de las reservas ni las plantillas de F10.

Migración aditiva `20261124180906_AvisosPublicos` aplicada en Supabase, con RLS y acceso directo revocado a `anon`/`authenticated`. Verificación: 382 pruebas locales aprobadas y 53 PostgreSQL omitidas en la suite general; una prueba PostgreSQL de este módulo aprobada por separado. Después se añadió una comprobación de orden/paginación/errores: las seis pruebas HTTP del módulo pasan. Modelo sincronizado y revisión completada. Concurrencia omitida por indicación del usuario.

**Frontend sin cambios por indicación del usuario:** el compañero conectará la presentación pública y el panel administrativo usando [AVISOS-PUBLICOS.md](AVISOS-PUBLICOS.md). Pendiente publicar la API y comprobar después el flujo integrado. No se requieren respuestas ni credenciales nuevas del cliente para este bloque.

## Eventos informativos · 24 nov 2026

**Terminado el alcance informativo en backend**, autorizado por Kevin para avanzar mientras llegan las respuestas de OQ-04. Gestión solo Administrador activo: listado/detalle, alta y edición con versión. Título, slug único, resumen, descripción Markdown, ubicación opcional, imagen opcional mediante URL HTTPS, inicio y fin UTC; estados Borrador/Publicado/Archivado. Consulta pública de lista y detalle por slug muestra solo publicados, incluidos pasados; filtros de inicio en rango semiabierto, búsqueda, próximos/finalizados y paginación. No se asignaron precios, cupos ni contenido ficticio. Archivar oculta y conserva auditoría; no es una cancelación operativa.

Migración aditiva `20261124215554_EventosInformativos` aplicada en Supabase, con restricciones de fechas/estado/textos, índice único de slug, versión de concurrencia y RLS/acceso directo revocado. Verificación: **391 pruebas locales aprobadas, cero fallidas y 54 PostgreSQL omitidas**; una comprobación PostgreSQL específica aprobada por separado para migración, RLS, traducción de filtros, publicación/archivo, persistencia y auditoría. Ocho pruebas HTTP del módulo incluidas en la suite local. Modelo sincronizado y revisión sin hallazgos materiales; concurrencia omitida por indicación del usuario.

**Frontend sin cambios:** Thiago puede integrar catálogo y gestión con [EVENTOS.md](EVENTOS.md). La imagen se asocia por URL de recurso existente (por ejemplo Storage público); no se implementó carga de archivo ni bucket propio de eventos. Pendiente desplegar la API y comprobar el flujo integrado. Inscripciones, cupos, pagos, participantes/asistencia, relación con líneas y reporte F15 siguen pendientes de OQ-04; no cambian reservas, disponibilidad o inventario.

## Carta pública de alimentos y bebidas · 24 nov 2026

**Terminado el catálogo básico en backend.** GET `/api/alimentos` paginado con búsqueda/rango de precio y GET `/api/alimentos/{sku}` con detalle, precio, imagen, notas Markdown y existencias actuales. Solo productos Alimento activos y vendibles; agotados visibles con indicador, ocultos/no vendibles/otras categorías excluidos también por SKU. Consulta anónima de solo lectura; no aparta ni descuenta inventario. Gestión existente de productos, imágenes e inventario conserva permisos de Administrador/Recepción.

Se reutilizan tablas, datos y proyección del catálogo general, sin migración ni nuevos productos de ejemplo. Verificación: **399 pruebas locales aprobadas, cero fallidas y 55 PostgreSQL omitidas**; una comprobación PostgreSQL específica aprobada por separado para filtros SQL, precio/stock, aislamiento y actualización desde gestión real. Ocho pruebas HTTP del módulo incluidas en la suite, con gestión de alimentos por Recepción. Modelo sin cambios pendientes y revisión sin hallazgos materiales; concurrencia omitida por indicación del usuario.

**Frontend sin cambios:** contrato para Thiago en [ALIMENTOS.md](ALIMENTOS.md); pendiente desplegar API e integrar carta y gestión. OQ-05 sigue abierta para subcategorías/horarios, método de pago y flujo específico de preparación/entrega. No se crearon estados de cocina ni otra forma de cobro. Los reportes mantienen su alcance actual.

## Newsletter fuera de alcance · 24 nov 2026

Kevin decidió no desarrollar newsletter: no hay evidencia de una solicitud explícita del cliente. Se retiró la propuesta y no cuenta como pendiente, dependencia ni criterio de entrega. No se creó código, modelo, migración ni suscripciones reales. Solo se reconsiderará con una petición explícita y alcance acordado. Esta decisión no afecta las membresías CARTAQ ni los avisos operativos de R07.

## F18 · Búsqueda de clientes para mostrador · 25 nov 2026

**Terminado en backend.** GET `/api/reservas/mostrador/clientes` permite a Administrador/Recepción activos buscar por nombre/teléfono de clientes activos, con término obligatorio 2–150 caracteres y paginación. Devuelve únicamente ID, nombre y teléfono; excluye personal e inactivos. El directorio general de perfiles conserva su restricción de Administrador. La creación de mostrador mantiene su revalidación de cliente y disponibilidad. [Contrato para frontend](FRONTEND-CONTRATOS.md#f18--buscar-clientes-para-reservas-de-mostrador--25-nov-2026).

Sin migración ni cambios en frontend. Verificación: **410 pruebas locales aprobadas, cero fallidas y 56 PostgreSQL omitidas**; una comprobación PostgreSQL específica aprobada por separado para filtros SQL, roles, paginación, teléfono nulo y exclusión después de desactivar un cliente. Once pruebas HTTP del módulo incluidas en la suite local. Modelo sin cambios pendientes y revisión sin hallazgos materiales; concurrencia omitida por indicación de Kevin.

Kevin confirmó configuradas `Supabase__PublishableKey` y `Supabase__Url` en Azure. Se actualizaron el aviso principal y F16: queda por comprobar una subida real, sin darla por aprobada. Pendiente publicar F18 y que Thiago conecte la búsqueda; siguientes solicitudes F19/F20. F21 espera el documento PORTADAS.md que Thiago debe incorporar.

**Respuesta frontend (Thiago, 25 nov):** el contrato de F21 ya está **dentro de este ROADMAP** (sección «F21 · Portadas de página», al final). El archivo `PORTADAS.md` no se subía por una regla local de git y se eliminó para no duplicar. A partir de ahora toda solicitud nueva del frontend se anota solo aquí. Siguiente prueba del frontend: subida real a Storage (galería, producto, logo, portada del blog, foto de alimento); después se conecta F18 cuando esté publicado en Azure (hoy responde 404).

## F21 · Portadas de página (contrato para backend) · 25 nov 2026

**Estado actualizado, 27 nov 2026:** backend implementado y migración `20261127180413_PortadasPagina` aplicada en Supabase. Frontend preparado por Thiago; pendiente publicar la API y comprobar integración con sesión real. Contrato original debajo; notas de implementación y seguridad en [MARCAS-Y-PORTADAS.md](MARCAS-Y-PORTADAS.md#f21--portadas-de-página--27-nov-2026).

### Qué se quiere

El encabezado negro de las páginas públicas puede llevar una **foto de fondo** con un velo oscuro para que el texto siga legible (como la portada de Inicio). El Administrador, desde **Panel → Portadas** (`/admin/portadas`), elige para cada página:

- si muestra portada o se queda en negro,
- la imagen (subir archivo o reutilizar la de otra página),
- el punto de enfoque de la foto (qué parte se ve al recortarla),
- la intensidad del velo: `suave`, `normal` o `fuerte`.

Las imágenes van a **Supabase Storage** (bucket público nuevo `pcp-portadas`) y la **configuración y la URL** se guardan en la base de datos, igual que marcas y portadas del blog (F16/F17). No se guardan binarios en tablas.

### Páginas (clave fija)

| `pagina` | Ruta | Nota |
| --- | --- | --- |
| `inicio` | `/` | Hoy tiene una foto fija en el código (`/img/hero-tirador.webp`). |
| `nosotros` | `/nosotros` | |
| `reservar` | `/reservar` | |
| `catalogo` | `/catalogo` | |
| `tienda` | `/tienda` | |
| `eventos` | `/eventos` | Lista. El detalle de cada evento usa su propia imagen. |
| `alimentos` | `/alimentos` | |
| `membresias` | `/membresias` | |
| `contacto` | `/contacto` | |

Blog y Galería no aplican (su encabezado es claro en tableta/escritorio). Rechazar con 404 cualquier otra clave. Si el cliente pide más páginas, se agregan a esta lista en ambos lados.

### Rutas propuestas

| Método y ruta | Acceso | Resultado |
| --- | --- | --- |
| GET `/api/portadas` | Anónimo | Páginas **ya configuradas**: `[{ pagina, activa, imagenUrl, posicion, velo }]`. Si `activa=false`, `imagenUrl` puede ir `null`. Sin versión ni auditoría. Lista vacía válida. |
| GET `/api/administracion/portadas` | Administrador activo | Una fila por página configurada: `{ pagina, activa, imagenUrl, posicion, velo, version, actualizadoEnUtc }`. (El frontend completa las que falten.) |
| PUT `/api/administracion/portadas/{pagina}` | Administrador activo | Crea o reemplaza la configuración de la página. Cuerpo abajo. |
| POST `/api/administracion/portadas/{pagina}/imagen` | Administrador activo | Sube la imagen (multipart). Respuesta `{ imagenUrl, version }`. |

Recepción y Cliente: 403. Sin sesión: 401. Respuestas con `Cache-Control: no-store` (o un caché corto en la pública).

#### PUT · cuerpo

```json
{
  "activa": true,
  "imagenUrl": "https://PROYECTO.supabase.co/storage/v1/object/public/pcp-portadas/portadas/inicio/OPERACION.webp",
  "posicion": "50% 30%",
  "velo": "normal",
  "version": "UUID leído (null si la página aún no tiene fila)"
}
```

- `activa` obligatorio. Si `activa=true`, `imagenUrl` es obligatoria.
- `imagenUrl`: `null`, una URL **del bucket `pcp-portadas` de este proyecto** (permite reutilizar la imagen de otra página) o una ruta del sitio que empiece con `/img/` (fotos incluidas en el frontend, p. ej. la de Inicio). Rechazar cualquier otra URL (400).
- `posicion`: `"X% Y%"` con X e Y enteros de 0 a 100 (regex `^(100|[1-9]?\d)% (100|[1-9]?\d)%$`). Predeterminado `"50% 50%"`.
- `velo`: `suave` | `normal` | `fuerte`. Predeterminado `normal`.
- `version`: obligatoria si la fila existe; 409 si no coincide (el frontend recarga y no sobrescribe). Para la primera vez se acepta `null`.
- Devuelve la fila completa con la nueva `version`.

#### POST imagen · formulario

Igual que F16 (portada del blog) y F17 (logos): `multipart/form-data` con `archivo` e `idOperacion` (UUID estable para reintentos del mismo archivo). JPG/PNG/WebP hasta **8 MB** (fotos anchas de encabezado). Valida firma/MIME/tamaño, genera la ruta (sin usar el nombre del archivo), sube a `pcp-portadas/portadas/{pagina}/{idOperacion}.{ext}`, asigna `imagenUrl` a la fila (creándola si no existe, con `activa=true`, `posicion="50% 50%"`, `velo="normal"`) y devuelve `{ imagenUrl, version }`. No cambia `activa` si la fila ya existía. Fallo del proveedor: conserva la imagen anterior (502/504). 503 si Storage no está configurado.

### Persistencia sugerida

Implementado: tabla `PortadasPagina` con UUID interno para la auditoría existente y `Pagina` única (texto, lista fija), `Activa`, `ImagenUrl`, `Posicion`, `Velo`, `Version` y `ActualizadoEnUtc`. Indicador interno `Configurada` oculta reservas de primera carga hasta completar la asociación; no se expone por API. Tabla `CargasPortadaPagina` conserva idempotencia, creador, huella, ruta y versión. RLS habilitado y acceso directo revocado a `anon`/`authenticated` (todo por la API). Bucket público `pcp-portadas`: insertar solo en la ruta reservada por un Administrador activo con versión vigente, sin sobrescritura ni borrado directo. Migración aditiva, **sin datos iniciales**.

### Comportamiento acordado en el frontend

- **Página sin fila** en `GET /api/portadas` → usa la portada fija del código (hoy solo Inicio) o fondo negro.
- **Fila con `activa=true` e `imagenUrl`** → muestra esa foto con su `posicion` y `velo`.
- **Fila con `activa=false`** → fondo negro (permite quitar incluso la foto fija de Inicio).
- Si `/api/portadas` responde 404/405 (backend sin F21), el sitio sigue con la foto fija y el panel entra en **modo vista previa** (prueba local sin guardar, botón «Guardar (pendiente de backend)»).

Archivos frontend: `src/features/portadas/api.ts` (contrato y `usePortada`), `src/components/ui/PageHero.tsx` (lee la portada según la ruta), `src/pages/admin/AdminPortadasPage.tsx` (panel).

### Cómo probar cuando esté publicado

1. Panel → Portadas → Nosotros: subir una foto, mover el enfoque, velo «fuerte», guardar. `/nosotros` debe verse con foto.
2. Tienda: «usar la imagen de Nosotros», guardar. `/tienda` usa la misma URL (sin subir de nuevo).
3. Inicio: apagar «Mostrar portada» y guardar → `/` queda en negro; volver a encender.
4. Dos pestañas guardando la misma página → la segunda recibe 409 y recarga.
5. Recepción no ve el módulo (403 en la API).

## F24 · Tienda por categorías y marcas (contrato para backend) · 27 nov 2026

**Backend implementado el 27 nov**, migración `20261127172920_CategoriasTiendaYMarcas` aplicada en Supabase. Sin cambios de frontend. Pendiente publicar API y comprobar la integración. [Guía de implementación y compatibilidad](TIENDA-CATEGORIAS.md); ver el cierre al final para verificaciones y límites.

**Prioridad 1 para Kevin.** Pedido expreso del cliente: la tienda vende rifles, municiones, cargadores, accesorios, tanques, compresores, repuestos y ropa PCP. El cliente quiere entrar por **botones de categoría → marcas de esa categoría → productos de la marca** (como la tienda de su distribuidor) y **poder crear categorías nuevas él solo** desde el panel. Los rifles se venden en la tienda; `/catalogo` (informativo, «Por cotizar») sigue aparte.

### Por qué no basta el enum `CategoriaProducto`

`CategoriaProducto` (SinClasificar, Alimento, Playera, ReplicaDecorativa, Consumible) controla reglas internas (tallas, carta de alimentos, consumibles de paquetes, catálogo informativo) y es fijo en código. La categoría de tienda es **otra dimensión, editable**: no reemplaza al enum ni cambia sus reglas.

### Datos

- **Tabla nueva `CategoriasTienda`:** `Id`, `Nombre` (≤ 60, obligatorio), `Slug` (≤ 60, único, `^[a-z0-9]+(-[a-z0-9]+)*$`; **reservados: `carrito`, `checkout`, `pedido`, `producto`** porque son rutas del sitio), `Descripcion` (≤ 300, nulo), `ImagenUrl` (≤ 2048, nulo; bucket público nuevo `pcp-tienda`), `Orden` (int), `Activa` (bool), `Version` (UUID de concurrencia), `ActualizadoEnUtc`, auditoría. RLS como el resto (todo por la API). Sin datos iniciales obligatorios; opcional sembrar las 8 del cliente: Rifles, Municiones, Cargadores, Accesorios, Tanques, Compresores, Repuestos, Ropa PCP.
- **Marcas (`MarcaAliada`, F17) — se reutilizan**, no se crea otra tabla: agregar `Slug` (≤ 80, único, mismo formato; la migración lo genera desde `Nombre`) y `VisibleEnCarrusel` (bool; migración = `Activo`). `GET /api/marcas` (carrusel de la portada) devuelve solo `Activo && VisibleEnCarrusel`. Una marca con `Activo=false` no aparece en ningún lado.
- **Producto:** `CategoriaTiendaId` (FK nula) y `MarcaId` (FK nula). Independientes de `Categoria` (enum).
- **Las marcas de una categoría NO se configuran aparte:** salen de los productos. Una marca aparece en «Municiones» si tiene productos activos, vendibles y con precio en esa categoría. (Así la libreta del cliente se vuelve simplemente dar de alta los productos con su categoría y marca.)

### Rutas públicas (anónimas)

| Método y ruta | Resultado |
| --- | --- |
| GET `/api/tienda/categorias` | Activas, ordenadas por `orden` y `nombre`: `[{ id, nombre, slug, descripcion, imagenUrl, orden, cantidadProductos }]`. `cantidadProductos` = productos activos, vendibles y con precio de esa categoría. Lista vacía válida. |
| GET `/api/productos` (existente) | Agregar a cada `ProductoCatalogo`: `categoriaTienda: { id, nombre, slug }` o `null`, y `marca: { id, nombre, slug, logoUrl, orden }` o `null` (marca `null` si está inactiva). Filtros opcionales nuevos: `categoriaTienda={slug}` y `marca={slug}`. |
| GET `/api/productos/{sku}` (existente) | Los mismos dos campos. |

### Rutas de administración (Administrador activo; Recepción y Cliente 403)

| Método y ruta | Resultado |
| --- | --- |
| GET `/api/administracion/tienda/categorias` | Todas (también inactivas): `{ id, nombre, slug, descripcion, imagenUrl, orden, activa, version, cantidadProductos }` (aquí `cantidadProductos` cuenta todos los productos asignados, activos o no). |
| POST `/api/administracion/tienda/categorias` | `{ nombre, slug, descripcion, orden, activa }` → fila completa. 409 si el `slug` existe o es reservado. |
| PUT `/api/administracion/tienda/categorias/{id}` | Mismo cuerpo + `version` → fila con versión nueva. 409 si la versión no coincide (el frontend recarga). |
| POST `/api/administracion/tienda/categorias/{id}/imagen` | `multipart/form-data` con `archivo` e `idOperacion` (igual que F16/F17). JPG/PNG/WebP ≤ 5 MB → `{ imagenUrl, version }`. 503 si Storage no está configurado. |
| DELETE `/api/administracion/tienda/categorias/{id}` | 204. **409** si tiene productos asignados («Mueve sus productos a otra categoría o desactívala»). |
| GET/POST/PUT `/api/administracion/marcas` (existentes) | Agregar `slug` y `visibleEnCarrusel` al cuerpo y a la respuesta. `slug` opcional al crear (se genera del nombre). |
| GET `/api/administracion/productos` (existente) | Agregar `categoriaTiendaId` y `marcaId`. |
| POST/PUT `/api/productos` (existentes) | Aceptar `categoriaTiendaId` y `marcaId` (nulos permitidos; 400 si no existen). El PUT sigue reemplazando todo: el frontend reenvía ambos. |

### Cómo lo usa el frontend (ya construido)

- **Sitio:** `/tienda` muestra botones con foto por categoría (y abajo «Todos los productos» con búsqueda). `/tienda/{categoria}` muestra los logos de las marcas de esa categoría y sus productos. `/tienda/{categoria}/{marca}` muestra solo esa marca. La vista previa del producto lleva la ruta «Tienda › Categoría › Marca».
- **Panel:** Productos → pestaña **«Categorías»** (crear, editar, ordenar, foto, activar, eliminar si está vacía) y en el editor de producto las listas **«Categoría de la tienda»** y **«Marca»**. Contenido → Marcas agrega **«Mostrar en el carrusel de la portada»**.
- **Mientras F24 no exista** (`GET /api/tienda/categorias` → 404): la tienda se ve como hoy (lista única), las ligas de categoría dicen «pendiente» y el panel muestra «pendiente de backend (F24)». Sin datos inventados.

### Cómo probar cuando esté publicado

1. Panel → Productos → Categorías: crear «Municiones» con foto; `/tienda` muestra el botón.
2. Asignar a 3 productos categoría Municiones y marcas JSB/H&N: `/tienda/municiones` muestra los 2 logos; `/tienda/municiones/jsb` solo los de JSB.
3. Marca con «Mostrar en el carrusel» apagado: sale en la tienda pero no en el carrusel de `/`.
4. Eliminar una categoría con productos → 409 legible. Slug `carrito` → 409.
5. Dos pestañas editando la misma categoría → la segunda recibe 409 y recarga.

## Revisión de estado para configurar Mercado Pago · 27 nov 2026

Revisión de código/configuración y consultas remotas, sin crear compras ni modificar datos de negocio. Checkout y webhook existentes requieren AccessToken, WebhookSecret, NotificationUrl y retornos SuccessUrl/FailureUrl/PendingUrl. No se encontró configuración MercadoPago en User Secrets ni variables del proceso local; no se mostraron sus valores. La configuración de Azure no se inspeccionó: pendiente verificarla desde App Service.

Comprobaciones remotas sobre el App Service conocido: GET `/api/productos` → 200; GET `/api/reservas/mostrador/clientes?busqueda=cliente` sin sesión → 404 (F18 todavía no disponible allí); GET `/api/webhooks/mercadopago` → 405 y POST sin firma/data.id → 401, coherente con un webhook publicado protegido por firma. No confirma credenciales, firma válida, pagos ni conciliación. El workflow despliega `main`; el checkout local actual está en `dev`.

Kevin confirmó el frontend de pruebas: **https://pcp-shooting.vercel.app**. Tiene correo/contraseña de la cuenta de Mercado Pago, pero el formulario de acceso rechaza el correo; todavía no se ha accedido ni comprobado si existe aplicación Developers. Pendiente resolver acceso, obtener credenciales de prueba y configurar secretos/URLs. No registrar contraseña, AccessToken o WebhookSecret en el repositorio. F24 conserva su prioridad y puede desarrollarse sin credenciales de Mercado Pago. No se ejecutó la suite de pruebas en esta revisión documental.

## Cierre F24 · Categorías y marcas de tienda · 27 nov 2026

**Terminado en backend; pendiente publicar API.** Categorías de tienda editables con CRUD exclusivo de Administrador, slugs únicos/reservados, versión, orden, visibilidad, conteos público/administrativo y carga de imagen. Producto agrega categoría/marca opcionales y devuelve referencias públicas; filtros por slugs. Marca añade slug único y visibilidad independiente en carrusel. Se mantienen tipos internos, tallas, alimentos, consumibles y reglas de venta/pago existentes. Sin categorías ni productos de ejemplo; no se asignan asociaciones ficticias.

Migraciones **`20261127172920_CategoriasTiendaYMarcas`** y **`20261127173922_CompatibilidadMarcasF24`** aplicadas en Supabase. Backfill con nombres/estados actuales de marcas y sufijo UUID, versiones renovadas; productos previos conservados sin asignaciones. Defaults adicionales permiten altas de marcas desde la API anterior durante el despliegue. Bucket público **`pcp-tienda`** (5 MiB, JPG/PNG/WebP), RLS y función/política de inserción limitada a cargas reservadas por Administrador activo con versión vigente; provisión real verificada. Se conservan los archivos públicos al eliminar categoría vacía; eliminar categoría con productos asignados, incluso ocultos, devuelve 409.

Verificación final: **418 pruebas locales aprobadas, cero fallidas y 60 PostgreSQL omitidas** en la suite general; **siete comprobaciones PostgreSQL aprobadas por separado y secuencialmente**: migración/F24 completo, función Storage aislada, provisión de tienda y galería, compatibilidad con alta de marcas de API anterior, regresión de logos/blog y carta de alimentos. Siete pruebas HTTP nuevas de F24 y una selección de bucket añadidas a la suite local. Modelo sin cambios pendientes, `git diff --check` limpio y revisión sin hallazgos materiales. Concurrencia omitida por indicación de Kevin.

**Frontend sin cambios.** Thiago tiene el [contrato y notas de compatibilidad](TIENDA-CATEGORIAS.md) para publicar/probar categorías, asignación de productos, filtros, carrusel e imágenes con sesión real. No se desplegó a Azure, no se hicieron compras ni se cargaron archivos reales de usuario. Próximos pendientes definidos al cerrar este bloque: F19/F20, F21 y F22/F23; R05 sigue a la espera de resolver el correo correcto y configurar sus credenciales API.

## Cierre F19 · Disponibilidad de mostrador · 27 nov 2026

**Terminado en backend.** GET `/api/reservas/mostrador/disponibilidad` exige Administrador/Recepción activos y devuelve el DTO existente sin datos de ocupantes. Comparte cálculo/instantánea de disponibilidad pública y elimina únicamente el mínimo de anticipación web: inicio estrictamente futuro y máximo configurado inclusivo. Conserva bloqueo global/por línea, inactivas, apartados y ocupación de reservas pagadas/pendientes/con conciliación incompleta. Consulta de solo lectura, sin caché ni cambios de inventario.

La creación de mostrador ahora valida mediante esa variante, corrigiendo la reimposición del mínimo web que impedía crear un turno inmediato aunque el mostrador estuviera autorizado. Mantiene idempotencia, transacción y revalidación de disponibilidad/cliente. La API pública y su ventana no cambian ni aceptan una bandera para obtener permisos de mostrador.

Sin migración ni frontend modificado. Verificación: **426 pruebas locales aprobadas, cero fallidas y 61 PostgreSQL omitidas**; dos comprobaciones PostgreSQL aprobadas por separado (consulta/creación de mostrador en el mínimo con transacciones reales y regresión pública de bloqueos/margen). Ocho pruebas HTTP del módulo incluidas en la suite: permisos, mapa real, privacidad de consulta pública, límites, creación/reintentos y protección por pagos/conciliación. Modelo sin cambios pendientes, revisión sin hallazgos y `git diff --check` limpio. Concurrencia omitida por indicación de Kevin.

Pendiente publicar API e integrar/probar el selector de mostrador por Thiago, según [FRONTEND-CONTRATOS.md](FRONTEND-CONTRATOS.md#f19--disponibilidad-para-mostrador--27-nov-2026) y [DISPONIBILIDAD.md](DISPONIBILIDAD.md#f19--consulta-de-mostrador-para-personal). Sigue **F20**, lectura de plantillas de WhatsApp para Recepción. R06/llegadas y demás OQ conservan su estado.

## Cierre F20 · Plantillas WhatsApp para Recepción · 27 nov 2026

**Terminado en backend.** GET `/api/avisos/whatsapp` exige Recepción/Administrador activos y devuelve únicamente `waOrderReady`/`waReminder`, incluso si están vacíos. Proyección específica, sin correo remitente, versión, fechas o valores ficticios. Solo lectura y sin caché. GET/PUT `/api/administracion/avisos` siguen exclusivos de Administrador; Recepción no puede leer configuración completa ni escribirla. No se envían mensajes, no se sustituyen marcadores y R07 no cambia.

Sin migración ni frontend modificado. Verificación: **432 pruebas locales aprobadas, cero fallidas y 61 PostgreSQL omitidas** en la suite general; una comprobación PostgreSQL aprobada por separado para configuración/versión/auditoría y nueva proyección limitada. Seis casos HTTP nuevos incluidos en la suite cubren roles, campos exactos, plantillas actuales/vacías, caché y permisos de escritura. Se usa reloj fijo en la prueba PostgreSQL anterior para evitar diferencias entre precisión de DateTime y microsegundos de PostgreSQL. Modelo sin cambios pendientes y `git diff --check` limpio; concurrencia omitida por indicación de Kevin.

Thiago debe usar la nueva ruta para los textos de WhatsApp en Pedidos y conservar la administrativa para el editor. [Contrato](AVISOS.md#f20--lectura-operativa-de-whatsapp--27-nov-2026). Pendiente publicar API y verificar el flujo integrado con Recepción real. Siguiente bloque definido: **F21**, portadas de página, según el contrato incorporado en este roadmap.

## Cierre F21 · Portadas de página · 27 nov 2026

**Terminado en backend.** Cuatro rutas del contrato implementadas: consultas pública/administrativa, PUT por página y carga multipart. Nueve páginas fijas, activación, URL del bucket propio o `/img/`, enfoque porcentual, velo y versión. Público solo recibe configuración visible sin versión/auditoría; una fila desactivada permite quitar el respaldo fijo. Primera carga fallida no publica configuración incompleta. Carga sobre una portada existente conserva su estado activo/inactivo y metadatos. Rutas, permisos y campos del frontend conservados; no se modificó frontend.

Migración **`20261127180413_PortadasPagina`** aplicada en Supabase: tablas de configuración/cargas con UUID internos para auditoría y página única, sin datos iniciales, RLS/permisos directos revocados y bucket público **`pcp-portadas`** de **8 MiB**. Función y política de inserción limitadas a ruta reservada por su Administrador activo con versión vigente; sin sobrescritura/borrado directo. Objetos públicos previos se conservan. Desactivar no revoca URLs conocidas.

Verificación: **446 pruebas locales aprobadas, cero fallidas y 64 PostgreSQL omitidas** en la suite general; **cuatro comprobaciones PostgreSQL aprobadas por separado**: migración/carga/estado/auditoría, función de permisos aislada, provisión real del bucket/política y regresión de cargas de logos/blog. Doce casos HTTP nuevos de F21 y dos comprobaciones del transporte compartido (bucket y recuperación de duplicado mayor de 5 MiB). La revisión detectó y se corrigió el límite antiguo de lectura del archivo duplicado: ahora verifica longitud esperada y SHA-256, sin ampliar límites de otros módulos. Modelo sin cambios pendientes y `git diff --check` limpio. Concurrencia omitida por indicación de Kevin.

Pendiente **publicar API en Azure**, recargar frontend y comprobar con Administrador real los cinco recorridos del contrato F21. Guía de implementación en [MARCAS-Y-PORTADAS.md](MARCAS-Y-PORTADAS.md#f21--portadas-de-página--27-nov-2026). No se hicieron subidas reales de usuario ni despliegue desde esta sesión. Siguen F22/F23; R05 mantiene su dependencia de credenciales API de Mercado Pago.

## Cierre F22 · Eliminar productos sin historial · 27 nov 2026

Implementado DELETE `/api/productos/{id}` con los permisos existentes de edición: Administrador activo en todas las categorías y Recepción activa solo en alimentos. Devuelve 409 ante cualquier partida de pedido, movimiento de inventario, composición de paquete, consumo de reserva, imagen compartida o carga pendiente. No elimina relaciones de negocio en cascada. Comprueba también imágenes antiguas y referencias en Markdown/JSON de contenido, paquetes y reservas.

El producto, sus registros de carga, la auditoría y la lista de limpieza se confirman atómicamente antes de borrar archivos mediante la API de Storage. Cada archivo conserva su progreso; un fallo 502/504 permite repetir el mismo DELETE/id sin recrear la ficha. Un id desconocido devuelve 404; una eliminación ya completada devuelve 204 al reintentar. Las URL externas no autorizan eliminar archivos ajenos y las rutas de Storage sin propiedad verificable se bloquean. [Contrato completo](ELIMINAR-PRODUCTOS.md).

Migración **`20261128014109_EliminacionSeguraProductos` aplicada en Supabase**: tablas privadas con RLS, permisos de limpieza limitados por perfil/categoría/ruta pendiente y protección de referencias a imágenes eliminadas. Verificación: **463 pruebas locales aprobadas, cero fallidas**; **dos pruebas PostgreSQL aprobadas por separado** (migración/transacciones/auditoría/reintentos/protección de referencias y permisos aislados; provisión real de tablas/políticas/funciones). Storage simulado en las pruebas de borrado. Revisión sin hallazgos importantes pendientes, modelo sin cambios pendientes y `git diff --check` limpio. Concurrencia omitida por indicación de Kevin.

**Frontend sin cambios.** Falta publicar API en Azure y comprobar un borrado de un producto desechable con sesión real, incluyendo los permisos de Recepción y el manejo de limpieza pendiente. No se borraron productos ni archivos reales del cliente. Siguiente bloque definido: **F23, galería de imágenes por producto**; R05 sigue pendiente de credenciales API de Mercado Pago.

## Cierre F23 · Galería por producto · 27 nov 2026

Implementado `imagenes: [{ id, url, orden }]` en catálogo, ficha por SKU y listado administrativo. Hasta **8 fotos**, incluyendo reservas pendientes de agregar. POST de imagen acepta `agregar=true` para sumar al final; omitido/false conserva la operación de reemplazar principal y mantiene las secundarias. Reintentos exigen la modalidad original y no duplican ni restauran fotos retiradas. PUT de orden exige todos los ids actuales una sola vez y actualiza también `imagenUrl`. DELETE retira solo secundarias; para quitar la principal hay que elegir otra primero y no se permite vaciar la galería desde la edición general. Permisos de Administrador/Recepción en alimentos sin ampliaciones. [Contrato](PRODUCTOS-GALERIA.md).

Se conservan los archivos y el historial al retirar una foto; F22 limpia las cargas propias al eliminar el producto completo. F22 comprueba también fotos secundarias de otros productos y borra explícitamente las asociaciones propias, sin cascadas de negocio. Cambios transaccionales con auditoría y tabla privada con RLS.

Migración **`20261128020056_GaleriaImagenesProducto` aplicada en Supabase**: conserva la principal actual, incluidas URL externas, sin publicar versiones históricas automáticamente. Una conciliación diferida mantiene compatibles las escrituras del backend anterior que solo actualiza `imagenUrl` mientras se publica la nueva versión; no pisa una galería ya consistente.

Verificación: **473 pruebas locales aprobadas, cero fallidas y 68 PostgreSQL omitidas** en la suite general; **dos pruebas PostgreSQL de F23 aprobadas por separado** (migración, foto previa, carga, lecturas reales, orden, retirada, compatibilidad y limpieza; provisión real y restricciones RLS). **Dos regresiones PostgreSQL de F22 aprobadas**, incluida su protección de referencias y auditoría; **seis regresiones PostgreSQL de imágenes/productos/logos/blog aprobadas**. Diez comprobaciones PostgreSQL en total, ejecutadas por bloques. Revisión sin hallazgos importantes pendientes, modelo sin cambios pendientes y `git diff --check` sin errores. Concurrencia omitida por indicación de Kevin.

**Frontend sin cambios.** Pendiente publicar API en Azure, conectar el editor por Thiago y probar con sesión real: agregar sin cambiar principal, reemplazar, reordenar y retirar. No se hicieron cargas ni borrados reales de archivos del cliente. No requiere nuevas credenciales ni bucket. R05 mantiene su dependencia de credenciales API de Mercado Pago; los restantes pendientes se conservan en sus filas/OQ.

## Cierre A01 · Base de órdenes de alimentos · 27 nov 2026

Implementado un flujo independiente de los pedidos generales, con alta web propia y mostrador, consulta privada, bandeja de Administrador/Recepción, preparación sin pago previo, registro manual del cobro local completo y entrega. Estados Recibido → EnPreparacion → Listo → Entregado, pago separado, precios/nombres/SKU históricos, fecha UTC, UUID de versión e idempotencia por operación/actor/contenido. Mostrador permite identificar por nombre una visita sin cuenta; la web requiere perfil activo y no puede elegir otra cuenta. Sin reserva obligatoria, cancelaciones ni reembolsos nuevos. [Contrato](ORDENES-ALIMENTOS.md).

Entregar valida todas las existencias antes de registrar salidas, en una transacción SERIALIZABLE con auditoría. El cobro no descuenta inventario. FK e índice único por partida evitan duplicar salidas; reintentos originales no repiten cambios. Una falta de stock devuelve 409 sin entrega parcial ni alterar el pago. No se puede cambiar la categoría del producto con órdenes nuevas sin entregar ni eliminar productos con partidas de este historial.

Migración **`20261128030835_BaseOrdenesAlimentos` aplicada en Supabase**, tablas privadas con RLS y permisos directos revocados. Verificación: **484 pruebas locales aprobadas, cero fallidas**; **dos comprobaciones PostgreSQL de A01 aprobadas** (migración/flujo real/reintentos/privacidad/inventario/auditoría y provisión real de RLS/índice único); **dos regresiones PostgreSQL de F22 aprobadas**. Revisión sin hallazgos importantes, modelo sin cambios pendientes y `git diff --check` sin errores. Concurrencia omitida por indicación de Kevin.

**No habilitado para producción:** `Alimentos:OrdenesHabilitadas=false` por defecto; nuevas escrituras devuelven 503. Crear comprueba stock pero no lo reserva: falta acordar protección durante preparación, faltantes/no recogidos y acceso de barra. El cobro actual es un registro manual de personal autorizado, no validación bancaria ni integración de Mercado Pago. Faltan pago online, frontend por Thiago, coordinación de la transición desde alimentos en `/api/pedidos`, horarios/subcategorías y reportes de estas órdenes (F15 conserva el alcance anterior). **OQ-05 permanece abierta.** No se activó en Azure, no se desplegó la API, no se modificó el frontend ni se generaron cobros/consumos reales.
