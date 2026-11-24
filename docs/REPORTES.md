# Reportes administrativos

Requieren un token de usuario activo con rol **Administrador**. Cliente y Recepción reciben 403; una petición sin sesión recibe 401. Los reportes son consultas y no requieren migraciones.

## Endpoints

| GET | Filtros |
| --- | --- |
| `/api/reportes/pedidos` | `usuarioId`, `estado`, `busqueda` por SKU o nombre de producto, `desdeUtc`, `hastaUtc` |
| `/api/reportes/pagos` | `usuarioId`, `pedidoId`, `estado`, `referencia`, `desdeUtc`, `hastaUtc` |
| `/api/reportes/membresias` | `usuarioId`, `desde`, `hasta`, `vigenteEn` |
| `/api/reportes/inventario` | `categoria`, `busqueda`, `activo`, `esVendible`, `conExistencias`, `bajoMinimo` |
| `/api/reportes/reservas` | `usuarioId`, `estado` (`PendientePago`, `Vencida`, `Pagada`), `desdeUtc`, `hastaUtc` |

Todos aceptan `formato=json` (predeterminado) o `formato=csv`. JSON admite `pagina` (predeterminado 1) y `tamanoPagina` (predeterminado 20, máximo 100).

```json
{
  "datos": { "elementos": [], "total": 0, "pagina": 1, "tamanoPagina": 20 },
  "importeConfirmado": 0
}
```

`importeConfirmado` se calcula para pagos, membresías y reservas. En pedidos e inventario es `null`. Corresponde al conjunto completo filtrado, no solo a la página actual. Un pedido conserva el precio unitario guardado en sus partidas; `total` de cada pedido no implica que se haya cobrado.

## Fechas e importes

- Pedidos: las fechas filtran `CreadoEnUtc`.
- Reservas: las fechas filtran `InicioUtc` de la actividad, no creación ni cobro. `importeConfirmado` suma `Total` de reservas con `PagoConfirmadoEnUtc` dentro del conjunto filtrado; es valor contratado confirmado de esas actividades, no ingreso agrupado por fecha de cobro. No sumarlo a otros reportes como si fuera un ingreso adicional. Un pago en proceso o una conciliación incompleta mantiene `PendientePago` aunque venza el plazo original.
- Pagos: las fechas filtran `ConfirmadoEnUtc`. Al filtrar por fechas quedan fuera los pagos sin fecha de confirmación. Sin fechas se pueden consultar todos los estados.
- Membresías: `desde` y `hasta` filtran `InicioVigencia` y usan fechas `yyyy-MM-dd`. `vigenteEn` comprueba inicio inclusivo y fin exclusivo.
- Los límites `desde`/`desdeUtc` son inclusivos y `hasta`/`hastaUtc` exclusivos. Para incluir todo enero se usa desde el 1 de enero hasta el 1 de febrero. Los filtros UTC admiten ISO 8601 con zona horaria.
- El importe de pagos suma únicamente `EstadoPago.Confirmado`. El de membresías suma los pagos confirmados vinculados a las membresías seleccionadas. No sumar ambos reportes entre sí: pueden contener los mismos pagos. El reporte de membresías clasifica por vigencia, no por fecha de cobro.
- Inventario representa existencias actuales calculadas a partir de movimientos; incluye productos sin movimientos e inactivos salvo que se filtren. No es una reconstrucción del inventario a una fecha pasada.

## Descarga CSV

Desde el 23 nov 2026 se agregan nombre del cliente en pedidos/pagos/membresías, folio corto en pedidos/pagos y nombres de productos en pedidos. Se conservan las columnas anteriores y se amplía también el JSON. Los nombres son actuales; el folio es visual y el UUID sigue identificando la operación. Ver [contrato F15](FRONTEND-CONTRATOS.md). El 24 nov se agregó reservas; alimentos y eventos siguen pendientes.

Reservas exporta ID/folio, usuario, nombre/teléfono actual del cliente, nombre del paquete guardado al reservar, fecha/hora/zona de actividad, inicio UTC, estado actual, participantes, total y fecha de confirmación. No expone datos de nacimiento de participantes. Incluye web y mostrador. Aún no hay estados de llegada o ausencia: dependen de R06.

Ejemplo: `/api/reportes/pagos?estado=1&desdeUtc=2026-01-01T00:00:00Z&hastaUtc=2026-02-01T00:00:00Z&formato=csv`.

La respuesta es un archivo adjunto `text/csv; charset=utf-8`, UTF-8 con BOM, separador coma, decimales con punto y fechas ISO. Se escapan comillas, comas y saltos de línea. Los textos que podrían interpretarse como fórmulas reciben un apóstrofo inicial. Los estados y categorías aparecen por nombre en CSV.

La descarga ignora la paginación y exporta todas las filas coincidentes hasta 10000. Si hay más, devuelve 400 con `application/problem+json` solicitando filtros más acotados, sin archivo parcial. Un resultado vacío devuelve solo los encabezados. Los filtros y parámetros de paginación deben ser válidos incluso para CSV. Las consultas no reservan una instantánea transaccional entre solicitudes; los datos pueden cambiar si hay operaciones simultáneas.

El CSV contiene las filas detalladas; el resumen `importeConfirmado` se obtiene de la respuesta JSON con los mismos filtros. El frontend puede descargar la respuesta como un archivo usando el token habitual. No se modificó la pantalla de reportes del frontend.
