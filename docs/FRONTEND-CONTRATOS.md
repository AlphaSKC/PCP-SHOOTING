# Ampliaciones para frontend · F04, F05 y F07

Implementadas el 23 de noviembre de 2026. Cambios aditivos: se conservan rutas, campos anteriores y permisos. No requieren migración. Falta consumir los nuevos campos desde las pantallas.

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

Disponible en cada elemento de `GET /api/administracion/pedidos`, en `pedido` de `GET /api/administracion/pedidos/{id}` y en los pedidos del historial y reporte administrativo JSON que reutilizan ese contrato. El CSV conserva sus columnas actuales; su mejora corresponde a F15.

Recepción y Administrador conservan acceso a las rutas administrativas de pedidos. No se habilita el directorio general de perfiles a Recepción. Los valores son actuales del perfil; teléfono puede ser `null`, y un perfil ausente produce ambos campos `null`. Los pedidos propios/públicos no agregan información de otros clientes.

## Verificación

- Tres pruebas HTTP cubren creación, reintentos, detalle e historial de F04, configuración dinámica y reloj de F05, datos y permisos de F07.
- Dos pruebas PostgreSQL específicas comprueban carga de líneas, conteo y proyecciones de pedidos/reportes, incluido teléfono nulo. Se ejecutan por separado en esquemas temporales.
- Concurrencia omitida por indicación del usuario. No requiere cambios de esquema ni publicación para probar localmente.
