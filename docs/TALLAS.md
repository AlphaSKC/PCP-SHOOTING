# F09 · Playeras con inventario por talla

Backend implementado el 23 nov 2026. Tallas fijas `S`, `M`, `L`, `XL`; cada modelo/color es un producto independiente. Se comparte SKU, imagen y precio del producto: no hay precio distinto por talla ni variantes de color dentro de un producto. Frontend pendiente de conexión.

## Activar tallas en un producto

`POST /api/productos` y `PUT /api/productos/{id}` aceptan `usaTallas`. Solo puede ser `true` para categoría Playera (2). Los permisos existentes se conservan: Administrador gestiona playeras; Recepción no puede gestionarlas.

```json
{
  "sku": "PLAYERA-NEGRA",
  "nombre": "Playera negra",
  "categoria": 2,
  "precio": 125,
  "esVendible": true,
  "usaTallas": true
}
```

En productos nuevos, omitir `usaTallas` conserva el comportamiento sin tallas. En edición, omitirlo o enviarlo `null` conserva la configuración actual. El frontend debe enviar `true` expresamente al crear una playera con tallas.

Cambiar entre con/sin tallas requiere saldo cero en **cada grupo de stock** y ningún pedido pendiente que incluya ese producto; de lo contrario, 409. No se reparte automáticamente stock histórico. Para incorporar una playera anterior, revisar sus cantidades reales, resolver los pedidos pendientes, dejar el saldo antiguo en cero con un movimiento administrativo trazable, activar tallas y registrar la distribución real por talla. No adivinar tallas ni borrar movimientos anteriores.

## Existencias y catálogo

`ProductoCatalogo`, `ProductoGestion` y `FichaProducto` agregan `usaTallas` y `tallas`. Las cuatro tallas aparecen aunque no tengan existencias:

```json
{
  "usaTallas": true,
  "tallas": [
    { "talla": "S", "existencia": 2, "agotado": false },
    { "talla": "M", "existencia": 1, "agotado": false },
    { "talla": "L", "existencia": 0, "agotado": true },
    { "talla": "XL", "existencia": 0, "agotado": true }
  ]
}
```

Sin tallas, `tallas` es `null`. En las consultas públicas también es nulo para productos no vendibles. El listado público conserva la existencia total y su indicador de agotado; la compra se valida contra **la talla seleccionada**, no contra el total. La página administrativa y el catálogo cargan las tallas por lote, con una instantánea consistente en PostgreSQL.

`GET /api/inventario/{productoId}/tallas` devuelve esa lista de cuatro existencias, para los operadores que ya pueden gestionar el producto. Playeras requieren Administrador; Recepción continúa limitada a alimentos. Producto inexistente: 404; producto sin tallas: 409. Los listados de inventario agregan `usaTallas`; la existencia, mínimo y alertas generales del producto continúan siendo agregados, sin mínimos independientes por talla.

## Movimientos

La ruta existente acepta `talla`:

```http
POST /api/inventario/{productoId}/movimientos
Authorization: Bearer TOKEN_ADMIN
Content-Type: application/json

{
  "idOperacion": "UUID nuevo conservado para reintentos",
  "tipo": 0,
  "variacionCantidad": 5,
  "motivo": "Entrada talla M",
  "talla": "M"
}
```

La talla es obligatoria si `usaTallas=true` y debe omitirse/ser nula si el producto no usa tallas. Una talla inexistente o incompatible produce 400; una salida que deja esa talla negativa produce 409. Cambiar talla bajo el mismo `idOperacion` produce 409. No se descuenta de otra talla para compensar.

Los historiales global y por producto agregan `talla` a cada movimiento. El global admite `?talla=M`, junto con sus filtros anteriores y permisos de Administrador.

## Pedidos y pagos

`POST /api/pedidos` identifica una partida por **producto + talla**. Se pueden comprar varias tallas del mismo producto en un pedido; una combinación repetida se rechaza con 400.

```json
{
  "idOperacion": "UUID nuevo conservado para reintentos",
  "partidas": [
    { "productoId": "UUID playera", "talla": "S", "cantidad": 2 },
    { "productoId": "UUID playera", "talla": "M", "cantidad": 1 }
  ]
}
```

Crear el pedido valida stock por talla sin reservarlo. El pago revalida y descuenta esa talla una sola vez; una falta de stock no se resuelve con stock de otra talla. Se guarda el precio unitario común vigente del producto. Cambiar talla bajo el mismo ID de pedido produce 409. Las consultas propias y administrativas de detalle devuelven `talla` por partida; productos sin talla siguen devolviendo `null`.

Los nombres de artículos en checkout de Mercado Pago y los productos de reportes/CSV incluyen la talla. El frontend debe usar una clave de carrito basada en producto+talla, mostrar la talla en confirmación/pedidos y enviar la selección a la API. Una integración que solo envía producto no puede comprar playeras con tallas activadas.

## Migración y verificación

`20261123214835_TallasPlayeras` aplicada en Supabase el 23 nov 2026. Agrega `Productos.UsaTallas=false` y talla nullable a detalles/movimientos, índice para stock por talla y restricciones de categoría/enum. No cambia precios, cantidades, UUID ni distribuye stock anterior. Las tablas mantienen sus permisos y RLS existentes. Reiniciar el backend para cargar los nuevos contratos.

Cuatro pruebas HTTP cubren compra de dos tallas, permisos, stock por talla al crear y al pagar, reintentos, confirmación repetida, activación segura, preservación del modo al editar y datos inválidos. Una prueba PostgreSQL reconstruye el esquema anterior en un esquema temporal, aplica la migración y comprueba conservación de datos, catálogo, reportes, historial y pagos nuevos y anteriores.

Suite local: **300 aprobadas, cero fallidas y 42 PostgreSQL omitidas**. La prueba PostgreSQL específica pasó por separado. Concurrencia omitida por indicación del usuario; no se afirma compra sandbox ni integración frontend completada.
