# Ampliaciones para frontend

## F14 · Reservas en ficha del cliente · 24 nov 2026

`GET /api/administracion/usuarios/{id}/historial` agrega `reservas`, con la misma estructura paginada de las secciones existentes (`elementos`, `total`, `pagina`, `tamanoPagina`). Requiere Administrador activo; Recepción y Cliente conservan 403. Perfil inexistente: 404. Permite consultar perfiles desactivados para conservar el historial administrativo.

Nuevo parámetro `paginaReservas` (por defecto 1, mínimo 1). `tamanoPagina` sigue siendo común a todas las secciones, con máximo 100; `paginaPedidos`, `paginaPagos` y `paginaMembresias` permanecen independientes. Una página fuera de rango devuelve elementos vacíos y conserva el total. Sin reservas se devuelve una colección paginada vacía.

Cada reserva contiene `id`, `apartadoId` (nulo en mostrador), `nombrePaquete`, `inicioUtc`, `vencePagoEnUtc`, `total`, `estado` y `cantidadParticipantes`. Se ordenan por creación descendente y UUID para desempatar; incluyen web y mostrador del usuario indicado en la ruta. Los estados son `PendientePago`, `Vencida` y `Pagada`, con las reglas actuales de pagos: una conciliación incompleta o pago pendiente conserva `PendientePago` después del vencimiento original. No significa que la persona haya asistido.

Ejemplo: `/api/administracion/usuarios/{id}/historial?paginaReservas=2&tamanoPagina=10`. Para detalle usar el endpoint administrativo de reservas existente. F14 sigue **parcial**: correo de Auth y última visita no se agregaron. La última visita requiere registrar llegadas en R06; no se infiere de una reserva pagada o de su fecha.

## F15 · Reporte de reservas · 24 nov 2026

`GET /api/reportes/reservas`, solo Administrador activo, disponible como JSON paginado y CSV. Filtros: `usuarioId`, `estado`, `desdeUtc` inclusivo y `hastaUtc` exclusivo sobre **inicio de la actividad**. CSV ignora la página y exporta todas las filas filtradas, con el límite existente de 10000 y protección de fórmulas. Ver [campos, fechas e importes](REPORTES.md).

`importeConfirmado` suma el total contratado de reservas pagadas del conjunto filtrado, no ingresos por fecha de cobro. No usarlo como registro de asistencia ni sumar este valor a reportes financieros como un ingreso distinto. Reportes de alimentos y eventos siguen pendientes; llegada y ausencia dependen de R06. Cambios de este bloque sin migración; frontend pendiente de conectar.

F09 implementado después de estos contratos: [playeras con stock por talla](TALLAS.md). Incluye activación explícita, campos nuevos y requisitos de selección en carrito/pedidos. Los contratos sin tallas se mantienen para los productos anteriores.

Los contratos F04/F05/F07 y ampliaciones de tienda/inventario siguientes se implementaron el 23 de noviembre de 2026. Cambios aditivos: se conservan rutas, campos anteriores y permisos. No requieren migración. Falta consumir los nuevos campos desde las pantallas.

## F04 · Líneas y participantes

`POST /api/apartados`, `GET /api/apartados/{id}` y cada elemento de `GET /api/apartados` conservan `lineas` como lista de UUID y agregan:

```json
{
  "lineas": ["UUID de línea"],
  "lineasDetalle": [
    { "lineaId": "UUID de línea", "numeroLinea": 1, "nombreLinea": "Línea 1" }
  ]
}
```

Los detalles se ordenan por número de línea. No sustituir `lineas` por objetos: los contratos de creación siguen recibiendo UUID.

Cada elemento de `ReservaWebConsulta.participantes` agrega `numeroLinea` y `nombreLinea`. Se devuelve en la creación web/mostrador, reintentos y consulta propia. `GET /api/reservas` agrega `cantidadParticipantes` a cada resumen. El listado administrativo ya disponía de ese conteo.

Número y nombre provienen de la configuración actual de la línea; no son una copia histórica. Los demás datos contratados del paquete siguen siendo copias inmutables. El frontend puede mostrar la línea tras recargar sin depender de `sessionStorage`. Se mantienen las consultas propias por usuario y las políticas administrativas.

## F05 · Ventana pública de anticipación

`GET /api/disponibilidad/ventana`, sin autenticación y sin caché:

```json
{
  "zonaHoraria": "America/Cancun",
  "consultadoEnUtc": "2026-11-20T15:00:00Z",
  "anticipacionMinimaMinutos": 60,
  "anticipacionMaximaDias": 2,
  "inicioMinimoUtc": "2026-11-20T16:00:00Z",
  "inicioMaximoUtc": "2026-11-22T15:00:00Z",
  "primeraFecha": "2026-11-20",
  "ultimaFecha": "2026-11-22"
}
```

Valores del ejemplo según configuración inicial; leer siempre la respuesta actual. El servidor calcula los límites con su reloj y la configuración vigente. La ventana máxima sigue siendo móvil (días × 24 horas). Las fechas se expresan en la zona del club.

El calendario debe consultar disponibilidad únicamente entre `primeraFecha` y `ultimaFecha`, ambas inclusivas. Esa franja **no garantiza días abiertos ni líneas libres**: continuar usando `GET /api/disponibilidad?paqueteId=...&fecha=...&personas=...` para obtener turnos, cierres, bloqueos y capacidad del paquete/grupo. La creación del apartado revalida las condiciones. Con la regla inicial se consultan como máximo tres fechas locales en lugar de ocho fechas fijas. Mostrador conserva sus reglas específicas.

## F07 · Datos del cliente en pedidos administrativos

`PedidoAdministrativo` agrega:

```json
{
  "nombreCliente": "Nombre del perfil",
  "telefonoCliente": "1234567890"
}
```

Disponible en cada elemento de `GET /api/administracion/pedidos`, en `pedido` de `GET /api/administracion/pedidos/{id}` y en los pedidos del historial y reporte administrativo JSON. La ampliación del CSV se describe en F15 más abajo.

Recepción y Administrador conservan acceso a las rutas administrativas de pedidos. No se habilita el directorio general de perfiles a Recepción. Los valores son actuales del perfil; teléfono puede ser `null`, y un perfil ausente produce ambos campos `null`. Los pedidos propios/públicos no agregan información de otros clientes.

## Verificación

- Tres pruebas HTTP cubren creación, reintentos, detalle e historial de F04, configuración dinámica y reloj de F05, datos y permisos de F07.
- Dos pruebas PostgreSQL específicas comprueban carga de líneas, conteo y proyecciones de pedidos/reportes, incluido teléfono nulo. Se ejecutan por separado en esquemas temporales.
- Concurrencia omitida por indicación del usuario. No requiere cambios de esquema ni publicación para probar localmente.

## F08 y F11 · Existencias de tienda

`POST /api/pedidos` valida stock por partida antes de guardar pedidos nuevos; 409 si es insuficiente, sin pedido parcial. Repetir un ID con el mismo contenido devuelve el pedido existente aunque el stock cambie. Crear un pedido no reserva ni descuenta existencias: el pago sigue revalidando y descontando transaccionalmente. No se añadieron estados Preparando/Listo para recoger, pendientes de definición del cliente.

`GET /api/productos` agrega `existencia` (entero de 64 bits) y `agotado` por producto vendible. Sin movimientos: 0 y `true`. Para fichas no vendibles ambos son `null`. Consumibles internos e inactivos siguen ocultos. Frontend debe mostrar estos datos y actualizar el catálogo ante un 409; el dato público no garantiza stock al pagar.

## F13 · Movimientos con autor

`GET /api/inventario/movimientos` devuelve `PaginaConsulta<MovimientoConsulta>` y exige Administrador activo. Recepción/Cliente: 403; sin sesión: 401. Filtros: `pagina`, `tamanoPagina` (máximo 100), `productoId`, `actorId`, `tipo` (0 Entrada, 1 Salida, 2 Ajuste), `busqueda` por SKU/nombre, `desdeUtc` inclusivo y `hastaUtc` exclusivo. Orden: fecha descendente y UUID.

`MovimientoConsulta` conserva sus campos y agrega `productoId`, `sku`, `nombreProducto`, `actorId`, `actorNombre`. El autor procede del evento `Added` de auditoría con actor conocido; sin registro de usuario, queda `null`. El nombre del perfil es el actual y puede ser nulo si no existe. No se inventan autores históricos. También se amplía el historial por producto existente; Recepción mantiene su acceso limitado a alimentos.

## F15 · CSV actuales legibles

Se conservan las columnas anteriores y se agregan al final:

| Reporte | Columnas nuevas |
| --- | --- |
| Pedidos | `NombreCliente`, `Folio`, `Productos` (nombres separados por `; `). |
| Pagos | `NombreCliente`, `FolioPedido`. |
| Membresías | `NombreCliente`. |

JSON de pedidos conserva sus campos y agrega `folio`/`productos` (lista de nombres). JSON de pagos/membresías agrega `nombreCliente`. El folio de ocho caracteres es visual: el UUID completo sigue siendo el identificador único para acciones/enlaces. Nombres actuales de perfil/catálogo; importes guardados del pedido. Sin perfil, nombre nulo/celda vacía.

Se mantienen filtros, permisos, límite de 10000 filas, BOM UTF-8, comillas y protección contra fórmulas. F15 sigue parcial: los reportes específicos de reservas, alimentos y eventos quedan pendientes.

### Verificación del 23 nov 2026

290 pruebas locales aprobadas, cero fallidas y 40 PostgreSQL omitidas en esa ejecución. Diez pruebas PostgreSQL específicas aprobadas por separado: nueve flujos de escritura y una de stock, autor y reportes. Concurrencia omitida por indicación del usuario. Sin cambios pendientes de esquema ni migración.
