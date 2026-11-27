# F24 · Categorías y marcas de tienda

Backend desarrollado según el contrato de F24 en ROADMAP.md; migraciones aplicadas en Supabase el 27 nov 2026. Integración frontend a cargo de Thiago, pendiente publicar la API y probar el recorrido real. Este documento concreta compatibilidad, validaciones y persistencia; el contrato de rutas solicitado permanece en el roadmap.

## Modelo y compatibilidad

`CategoriaProducto` conserva su función interna: alimentos, playeras/tallas, catálogo informativo y consumibles. Se añaden `CategoriaTiendaId` y `MarcaId` opcionales a Producto, independientes del enum. La migración no asigna categorías/marcas a los productos existentes ni cambia precios, venta, inventario o reglas de pago. No se crean categorías de ejemplo.

`MarcaAliada` se reutiliza con `Slug` único y `VisibleEnCarrusel`. La migración genera slugs desde los **nombres actuales**, con sufijo UUID completo para garantizar unicidad, y copia `Activo` a `VisibleEnCarrusel`; renueva versiones. Conserva nombre, estado, descripción, logo y orden incluso si se editaron las marcas iniciales. No se crean marcas duplicadas.

La migración adicional `20261127173922_CompatibilidadMarcasF24` establece valores de base de datos para que la API anterior pueda seguir creando marcas hasta desplegar la nueva: slug de respaldo `marca-{UUID}` y visibilidad en carrusel true. El nuevo servicio sigue generando slugs desde el nombre cuando se omiten. La compatibilidad no rellena asignaciones ni cambia estados de marcas existentes. Revertir esa migración retira esos valores de respaldo.

Una categoría inactiva desaparece del listado público; sus referencias públicas en productos son nulas y filtrar por su slug devuelve lista vacía. Un producto activo conserva su visibilidad general conforme al contrato existente. Una marca inactiva devuelve referencia pública nula y no permite resultados por su slug. `VisibleEnCarrusel=false` solo la retira de GET `/api/marcas`: una marca activa puede seguir apareciendo dentro de productos y categorías de tienda.

## Categorías

| Método/ruta | Acceso | Resultado |
| --- | --- | --- |
| GET `/api/tienda/categorias` | Público | Lista de activas con ID, nombre, slug, descripción, imagen, orden y cantidad de productos activos/vendibles/con precio. Consumibles internos excluidos. |
| GET `/api/administracion/tienda/categorias` | Administrador activo | Todas; añade activa y versión. Cuenta todos los productos asignados, incluso inactivos/no vendibles. |
| POST `/api/administracion/tienda/categorias` | Administrador activo | Crea; 201 con fila completa. |
| PUT `/api/administracion/tienda/categorias/{id}` | Administrador activo | Edita; 200 con nueva versión. |
| POST `/api/administracion/tienda/categorias/{id}/imagen` | Administrador activo + Bearer | Multipart; devuelve `imagenUrl` y `version`. |
| DELETE `/api/administracion/tienda/categorias/{id}` | Administrador activo | 204 si no tiene productos; 409 si hay asignaciones, aunque estén inactivas. |

Listas ordenadas por orden, nombre y UUID. Lista vacía válida. Datos públicos sin versión; respuestas sin caché. Recepción/Cliente no gestionan categorías; sin sesión 401, rol no autorizado/inactivo 403.

Crear/editar acepta `{ nombre, slug, descripcion, orden, activa }`, y al editar `version`. Nombre obligatorio hasta 60, descripción opcional hasta 300, orden entero no negativo, slug obligatorio hasta 60 con minúsculas ASCII/números/guiones internos. Se recortan espacios de nombre/descripción. Reservados `carrito`, `checkout`, `pedido`, `producto`: 409. Slug duplicado: 409. Versión ausente/inválida: 400; versión antigua: 409; categoría inexistente: 404. PUT conserva la imagen existente: se modifica mediante la carga de archivo.

Eliminar no cambia productos ni los mueve automáticamente: primero reasignarlos o quitar su categoría, o desactivar la categoría. El borrado elimina también sus registros de carga reservada, dentro de la misma transacción; se conservan archivos públicos y auditoría según el mecanismo de imágenes existente. Un reintento de carga después de eliminar devuelve 404.

## Productos y marcas existentes

POST/PUT `/api/productos` añade `categoriaTiendaId` y `marcaId`, UUID nulos permitidos; 400 si el destino no existe. Se permite asignar destinos inactivos para preparar contenido; no se exponen como referencias públicas. PUT sigue reemplazando ambos campos, por lo que el editor debe reenviarlos. GET `/api/administracion/productos` devuelve ambos IDs incluso si el destino está inactivo.

GET `/api/productos` y GET `/api/productos/{sku}` añaden:

```json
{
  "categoriaTienda": { "id": "UUID", "nombre": "Ropa", "slug": "ropa" },
  "marca": { "id": "UUID", "nombre": "Club Wear", "slug": "club-wear", "logoUrl": null, "orden": 0 }
}
```

Cada referencia puede ser nula. Se mantienen DTO y campos anteriores. El listado acepta `categoriaTienda={slug}` y `marca={slug}` opcionales, combinables con `esVendible=true`, filtros y paginación existentes. Slugs desconocidos/inactivos producen lista vacía; formato inválido produce 400. Frontend obtiene las marcas de los productos de cada categoría, sin tabla adicional de asignación categoría-marca. Los conteos no son existencias: un producto agotado sigue contando.

GET/POST/PUT de marcas añade `slug` y `visibleEnCarrusel` al contrato existente. Al crear, omitir slug lo genera del nombre con sufijo UUID; se puede enviar uno más corto y legible. Slug explícito hasta 80 con el mismo formato; duplicado 409. Al editar, omitir slug conserva el anterior y omitir `visibleEnCarrusel` conserva su valor; esto mantiene clientes previos. En nuevas marcas omitir el indicador equivale a true. Cambiar slug cambia la ruta; no crea redirección automática. No se altera el permiso de gestión de marcas.

Recepción conserva su capacidad de gestionar **productos Alimento**, sin acceso a CRUD de categorías o marcas ni a otras clases de producto. No se cambian las reglas de tallas ni se convierten consumibles en productos vendibles.

## Imágenes de categoría

Multipart con `archivo` e `idOperacion` (UUID estable para reintentar el mismo archivo/destino). JPG/PNG/WebP, firma/MIME válidos, máximo **5 MiB** de archivo; límite HTTP 6 MiB para el formulario. SVG excluido. Bucket público nuevo `pcp-tienda`, ruta generada `tienda/{categoriaIdN}/{idOperacionN}.{ext}` sin utilizar el nombre aportado.

Reserva de carga con autor/huella/versión antes de contactar Storage. Asociar imagen y actualizar versión solo tras éxito; fallo conserva la anterior. El mismo ID ya completado no sube otra vez, y un reintento antiguo no restaura una imagen sustituida. Si cambió la categoría mientras se cargaba, 409 y recargar antes de crear otra operación. 401 sesión inválida, 403 permiso denegado, 503 Storage no configurado, 502/504 error/timeout del proveedor. Desactivar una categoría no revoca enlaces de imágenes públicas ya conocidas.

RLS de Storage solo permite insertar en rutas reservadas por el mismo Administrador activo y con versión vigente. No hay política nueva de sobrescritura/borrado de archivos. Logos y portadas del blog conservan rutas, buckets y límites anteriores; comparten el servicio de carga con el destino de categoría añadido.

## Migración y verificación

Migraciones `20261127172920_CategoriasTiendaYMarcas` y `20261127173922_CompatibilidadMarcasF24` aplicadas: nueva tabla con RLS y permisos directos revocados, FKs restrictivas e índices, ampliación de cargas de contenido, backfill de marcas, valores de respaldo y bucket/función/política de Storage. Sin borrado de productos/marcas en Up. Reversión quita las nuevas asociaciones y reservas de carga de categorías; conserva el bucket y archivos públicos, igual que los módulos anteriores.

Pruebas HTTP del módulo, suite local, comprobaciones PostgreSQL de migración sobre datos previos y flujo completo, validación aislada de función Storage, y regresión de logos/blog. Concurrencia omitida por indicación de Kevin. Estado de aplicación de migración, cifras de verificación y despliegue se registran al cierre en ROADMAP.md. Primera carga con sesión real y recorrido integrado quedan pendientes después de publicar la API; no se modificó frontend.
