# F23 · Galería por producto

## Lectura

GET `/api/productos`, GET `/api/productos/{sku}` y GET `/api/administracion/productos` incluyen `imagenes`, una lista ordenada de `{ id, url, orden }`. `orden` empieza en 0; la primera es la principal y su URL coincide con `imagenUrl`. Sin fotos se devuelve `[]`. Las consultas de alimentos que reutilizan el catálogo reciben también este campo adicional.

La migración conserva la imagen principal actual como primera foto, incluso si se registró por URL externa. No publica automáticamente las cargas antiguas: siguen siendo historial interno. Los identificadores de las fotos existentes se obtienen de la consulta actual, no se deducen del nombre de un archivo ni del id del producto.

## Carga

POST `/api/productos/{id}/imagen`, con el multipart existente (`archivo`, `idOperacion`) y un campo booleano opcional `agregar`:

- `agregar=true`: agrega al final y conserva la principal. Si es la primera foto, pasa a ser principal.
- Ausente o `false`: reemplaza la principal y conserva las fotos secundarias. Sigue siendo compatible con la carga anterior.

Máximo **8 fotos**, contando las reservas pendientes de agregar. JPG/PNG/WebP, hasta 5 MiB y mismas validaciones del cargador existente. La respuesta conserva el contrato actual (`id`, `productoId`, `url`, `tipoContenido`, `tamano`, `esImagenActual`). Después de completar una carga, volver a consultar la galería.

Un reintento conserva `idOperacion`, archivo y valor de `agregar`. Cambiar archivo, producto o modalidad con el mismo id devuelve 409. Repetir una carga completada no duplica fotos, no restaura una foto retirada ni revierte una principal posterior. Un fallo de Storage conserva la reserva: reintentar con esos mismos datos. El límite se valida antes de llamar a Storage y al completar la asociación.

## Orden y principal

PUT `/api/productos/{id}/imagenes/orden`, JSON:

```json
{ "ids": ["uuid-foto-que-sera-principal", "uuid-otra-foto"] }
```

Enviar **todos** los ids actuales exactamente una vez, entre 1 y 8. Devuelve 200 con la lista ordenada `{ id, url, orden }`. La primera actualiza también `imagenUrl`. Lista vacía, ids repetidos o UUID cero: 400; lista incompleta, ajena o desactualizada: 409, recargar antes de reintentar. No se aceptan imágenes de otro producto.

## Retirar una foto

DELETE `/api/productos/{id}/imagenes/{imagenId}` devuelve 204 si la foto es secundaria. Devuelve 404 si no pertenece a este producto y 409 si es principal o la única foto. Para retirar la principal, primero enviar el orden completo eligiendo otra y después retirar la anterior. Los órdenes restantes se compactan desde cero.

Retirar borra la asociación a la galería, **no el archivo físico ni su registro de carga**. Puede haber referencias compartidas y se conserva el historial de reintentos. [F22](ELIMINAR-PRODUCTOS.md) elimina todas las cargas propias, incluidas las retiradas, al borrar el producto completo si cumple sus reglas. No se añade ningún permiso de borrado directo de archivos al frontend.

La edición general de un producto con otra `imagenUrl` reemplaza/promueve la principal manteniendo las secundarias. Enviar `null` cuando ya existen fotos devuelve 409: no permite vaciar la galería ni eludir la protección de la última foto. Para ocultar el producto se conserva PATCH de estado.

## Permisos, persistencia y publicación

Carga, orden y retirada requieren Administrador activo, o Recepción activa para alimentos (categoría 1). Cliente no tiene acceso. Se valida la categoría real del producto, no un valor enviado por el navegador. No cambia el inventario, el precio ni los permisos generales del catálogo.

`FotosProducto` guarda las asociaciones públicas ordenadas; `ImagenesProducto` conserva el historial y la modalidad original de cada carga. Las escrituras son transaccionales con auditoría. La tabla nueva tiene RLS y los permisos directos de `anon`/`authenticated` revocados. F22 comprueba también fotos secundarias de otros productos y la protección en base de datos impide reutilizar una ruta eliminada.

La migración `20261128020056_GaleriaImagenesProducto` es aditiva y conserva la principal actual. Una conciliación diferida al final de la transacción mantiene compatible la API anterior que todavía escribe solo `imagenUrl`, sin alterar una galería ya consistente de la nueva API. No requiere nuevas claves ni un bucket adicional.

Frontend a cargo de Thiago: publicar la API, leer `imagenes`, añadir `agregar=true` al sumar fotos, enviar el orden completo, elegir otra principal antes de retirarla y manejar 409 recargando. Falta comprobar el recorrido con sesión real tras publicar. Las pruebas utilizan esquemas PostgreSQL aislados y Storage simulado; no cargan ni borran archivos reales del cliente.
