# Roadmap compartido · PCP Shooting

Última actualización: **20 de noviembre de 2026**.

Este documento reúne el estado actual y el orden de trabajo del equipo. Las guías de cada módulo explican los contratos de la API. Una pantalla con datos de demostración no significa que su integración esté terminada.

**Siguiente bloque de backend: R03.3 — Cierre de vencimientos y casos límite. R03.2 terminado a nivel de backend.**

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
| Catálogo de alimentos, playeras y decorativos no funcionales | Terminado para gestión básica, categoría, imagen y notas Markdown | Catálogo de tienda conectado cuando `VITE_DATA_SOURCE=api`; gestión administrativa por conectar/verificar. |
| Imágenes de productos | Terminado el endpoint de carga y la relación de múltiples imágenes | Pendiente primera carga completa contra Storage con una sesión real y conexión del editor/galería de producto. |
| Pedidos de clientes y pagos manuales | Terminado el flujo básico: crear, consultar, registrar/confirmar pago, cancelar pendientes y recoger | Creación/consulta del pedido en tienda siguen simuladas. Sin checkout real de Mercado Pago. |
| Inventario | Terminado: movimientos, existencias, mínimos, alertas, búsqueda y paginación; categoría interna Consumible (4), no vendible y oculta en tienda | Panel administrativo de demostración; por conectar/verificar. |
| Membresías CARTAQ | Parcial: registro de vigencias y renovación con pago completo confirmado | Falta cerrar planes, precios, beneficios y renovación definitiva. Cobro recurrente no implementado. |
| Historial del cliente | Terminado para perfil, pedidos, pagos y membresías | Por conectar/verificar. Se ampliará con reservas cuando existan. |
| Reportes CSV/JSON | Terminado para pedidos, pagos, membresías e inventario | Pantalla existente usa demostración; falta conectarla. Reservas y eventos aún no tienen reporte real. |
| Blog | Terminado: borradores, programación, publicación, archivo, filtros y auditoría | Editor y lectura pública siguen usando datos locales; falta conectarlos. Portada por URL, sin carga propia del blog. |
| Configuración operativa, líneas y bloqueos | Terminado el punto R01; migración aplicada en Supabase | Falta conectar el panel a los valores reales y a sus permisos. |
| Paquetes y consumibles | Terminado R02: gestión por persona, precios general/CARTAQ opcional, composición, versiones, permisos y catálogo público | Frontend aún en demostración. Falta conectar y cargar el catálogo real del cliente. |
| Consulta de disponibilidad | Terminado R03.1 y ampliado en R03.2: turnos, mapa, bloqueos, anticipación y apartados vigentes | Frontend por conectar. Consultar no bloquea capacidad ni garantiza stock. |
| Apartados temporales | Terminado R03.2: selección atómica de líneas, plazo configurable, idempotencia, privacidad y expiración lógica | Frontend por conectar. No crea reservas confirmadas ni pagos; falta el flujo R04/R05. |
| Galería del sitio | Pospuesto; no implementada en backend | Existe demostración. Retomar después por acuerdo del equipo. |

Última verificación del backend al cerrar R03.2: **291 pruebas aprobadas**, incluidas las pruebas PostgreSQL en esquemas temporales. Las migraciones de blog, configuración operativa, paquetes/consumibles y apartados están aplicadas en Supabase. Esta cifra es evidencia de ese cierre, no una sustitución de las pruebas de cambios posteriores.

## 2. Orden de trabajo de reservaciones

Los IDs R01–R08 son estables para usarlos en mensajes y tareas. No son los números del plan anterior de mejoras, cuyo punto 5 era blog/galería.

| ID | Bloque | Estado backend | Dependencias y criterio de cierre |
| --- | --- | --- | --- |
| R01 | Configuración operativa, horarios, líneas y bloqueos | **Terminado** | Endpoints con permisos, auditoría, versiones, valores iniciales y migración aplicada. Integración del panel pendiente. |
| R02 | Paquetes y consumibles | **Terminado — backend** | Gestión implementada, composición por persona, tarifa general/CARTAQ opcional, consumibles internos, permisos, auditoría y concurrencia. Migración aplicada. Falta integrar frontend y recibir/cargar el catálogo definitivo; no se inventaron paquetes reales. |
| R03 | Disponibilidad y apartados temporales | **Parcial — R03.1/R03.2 terminados** | Consulta y apartados temporales implementados. Siguiente R03.3: cierre de vencimientos/casos límite. Aún no existen reservas confirmadas ni pagos asociados. |
| R04 | Reservaciones web y de mostrador | **Pendiente** | R02 y R03. Usuario, participantes, paquete, fecha, hora y líneas; permisos, historial y excepción de anticipación para mostrador. La confirmación de pago real depende de R05. |
| R05 | Mercado Pago en entorno de pruebas | **Pendiente** | Aplicación/credenciales y notificaciones HTTPS. Checkout, QR con enlace, validación del cobro, idempotencia, pago tardío y conciliación. Probar con R03/R04; preparar también el enlace con pedidos de tienda. |
| R06 | Responsivas, llegadas y ausencias | **Pendiente** | R04/R05 y texto/mecanismo de responsiva. QR de llegada, búsqueda manual, tutor, llegada, consumo único de inventario y estado No asistió conservando el cobro. |
| R07 | Avisos y solicitud de factura | **Pendiente** | R05/R06, correo correcto, proveedor de correo/WhatsApp y datos fiscales acordados. QR en perfil y avisos. Captura fiscal para facturación del club, sin emitir CFDI automáticamente. |
| R08 | Historial y reportes de reservas | **Pendiente** | R04–R06. Agregar reservas, llegadas y ausencias a historial/reportes existentes y probar con frontend. |

R03.2 ya crea apartados reales y libera su capacidad lógica al vencer, sin esperar a borrar registros. **Todavía no existen reservas confirmadas ni integración de pagos en ese flujo**. Eso corresponde a R04/R05.

### Entregas de R03

| ID | Entrega | Estado |
| --- | --- | --- |
| R03.1 | Consulta pública de turnos y mapa de líneas usando configuración, paquetes y bloqueos. | **Terminado — backend**. [Contrato y límites](DISPONIBILIDAD.md). |
| R03.2 | Crear apartados temporales con el plazo configurado y evitar empalmes entre clientes. | **Terminado — backend**. [Contrato](APARTADOS.md). Revalidación, grupos atómicos e idempotencia probados con solicitudes simultáneas. |
| R03.3 | Cierre de vencimientos, conservación/consolidación de registros y pruebas ampliadas de límites. | **Pendiente — siguiente**. La exclusión lógica de vencidos y la concurrencia básica ya están en R03.2; ampliar/cerrar esas garantías sin duplicar trabajo antes de habilitar pagos y reservas. |

La anticipación máxima se interpreta provisionalmente como una ventana móvil de 48 horas. La distinción entre 48 horas y dos fechas locales sigue pendiente del cliente. La consulta ya incluye apartados vigentes, pero todavía no comprueba stock ni incorpora reservas confirmadas.

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

Estas reglas se recopilaron del cuestionario del cliente y las aclaraciones del equipo del **20 de noviembre de 2026**. Algunas son configuración ya persistida; otras son requisitos para módulos aún pendientes.

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
| Pago fuera de plazo | Decidir qué hacer si Mercado Pago confirma después de vencer el apartado o tras una ausencia; no confirmar una línea ya ocupada. | R03/R05 |
| Cuenta de Mercado Pago | El cliente indicó que debe abrirla. Identificar al responsable que dará acceso a la aplicación/credenciales (pregunta 4.2). | R05 y producción |
| Pruebas de Mercado Pago | Crear/configurar la aplicación, entorno de pruebas, tarjetas/cuentas necesarias y URL HTTPS de notificaciones. | R05 |
| Reserva de mostrador | La creación inmediata y el cobro en la aplicación ya están confirmados. Falta concretar cómo identificar/crear la cuenta de quien llega sin registro previo. | R04 |
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
| 20 nov 2026 | R03.2 terminado: apartados privados con TTL configurable, selección atómica, reintentos sin renovación, vencimiento lógico y protección concurrente. Migración aplicada, 291 pruebas aprobadas. Siguiente R03.3. |

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
- [Contexto e instrucciones del proyecto](../CLAUDE.md): contiene acuerdos y estado histórico; contrastar sus pendientes antiguos con este roadmap y las aclaraciones posteriores.

Los PDF de preguntas y respuestas fueron proporcionados al equipo y analizados, pero no se incorporan automáticamente al repositorio desde la carpeta personal de descargas.
