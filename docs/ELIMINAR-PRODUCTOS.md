# F22 · Eliminar productos creados por error

`DELETE /api/productos/{id}` con sesión Bearer. Administrador activo puede eliminar cualquier categoría; Recepción activa solo Alimento (1), incluyendo al reintentar una limpieza pendiente. Cliente y perfiles inactivos no tienen acceso. La validación de categoría se realiza dentro del servicio para poder autorizar también los reintentos después de borrar la ficha.

## Respuestas

| Código | Significado |
| --- | --- |
| 204 | Producto eliminado y limpieza completada, o reintento de una eliminación ya completada. Sin cuerpo. |
| 401 / 403 | Sesión ausente/vencida o falta de permisos. |
| 404 | Identificador desconocido: nunca se registró su eliminación. |
| 409 | Tiene historial, imágenes compartidas, una carga pendiente o una ruta que no permite verificar su propiedad. Mostrar el mensaje y ofrecer ocultarlo. |
| 502 / 504 | La ficha ya fue eliminada, pero Storage no confirmó la limpieza. Repetir **el mismo DELETE y el mismo id** con sesión vigente. |
| 503 | Configuración de Storage ausente; configurar las claves y repetir el mismo DELETE. |

Se bloquea cualquier partida de pedido, movimiento de inventario (incluso entrada inicial o movimientos que sumen cero), composición de paquete o consumo de reserva. No depende del estado del pedido ni de las existencias actuales. No hay borrado en cascada de estas relaciones.

Se comprueban la imagen principal y todas las cargas antiguas. Una referencia en otro producto, blog, evento, marca, categoría, galería, portada, contenido institucional, paquete o reserva también bloquea la operación, incluyendo enlaces dentro de Markdown/JSON y registros ocultos. La base de datos protege además las rutas borradas contra nuevas referencias. Una carga sin finalizar bloquea el borrado para no dejar una subida en curso sin registro: completar/reintentar esa carga o usar ocultar.

Con F23 se comprueban también las fotos secundarias de otros productos en `FotosProducto`. Las asociaciones de galería del producto eliminado se borran explícitamente dentro de su transacción; las cargas retiradas de su galería siguen incluidas en la limpieza de Storage.

Solo se eliminan objetos de `pcp-productos` con ruta generada por la API y registro propio en `ImagenesProducto`: `productos/{productoId:N}/{operacionId:N}.jpg|png|webp`. Se eliminan todas sus cargas completadas, no solo la principal. Las URL externas no autorizan borrar archivos de otro proveedor. Una URL de `pcp-productos` sin carga propia verificable devuelve 409 para evitar un borrado parcial o ajeno.

## Consistencia y auditoría

Una transacción SERIALIZABLE comprueba las reglas, registra `EliminacionProducto`/`ArchivoProductoEliminado`, elimina explícitamente los registros de sus imágenes y la ficha, y guarda la auditoría habitual con actor, entidad, fecha y solicitud. Después se llama a la API de Storage con el JWT del operador; nunca se elimina un archivo antes de confirmar la transacción de negocio.

Cada ruta conserva su progreso de limpieza. Una desconexión o fallo del proveedor permite reanudar la operación sin recrear el producto. Un archivo ya ausente se considera limpiado; una respuesta vacía de Storage se verifica para no confundir falta de permisos con éxito. No se almacena el JWT, no hay tarea automática ni se requiere service-role. Un operador autorizado debe repetir el DELETE si quedó pendiente; el identificador se conserva en la solicitud y en los registros internos de eliminación/auditoría.

La migración `20261128014109_EliminacionSeguraProductos` añade tablas privadas con RLS, protección de referencias y permisos SELECT/DELETE de Storage limitados a rutas pendientes y al perfil activo autorizado. Las rutas se conservan después de completar la limpieza para impedir reutilizar un enlace borrado. El borrado físico usa la [API de Supabase Storage](https://supabase.com/docs/guides/storage/management/delete-objects), no SQL sobre sus objetos.

Ocultar con `PATCH /api/productos/{id}/estado` sigue siendo la operación normal para productos con historial. El frontend existente queda a cargo de Thiago; falta publicar la API y comprobar un borrado de un producto desechable con sesión real. Las pruebas automatizadas utilizan esquemas PostgreSQL aislados y Storage simulado; no borran productos ni archivos de usuario.
