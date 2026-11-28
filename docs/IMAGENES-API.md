# Subida de imágenes de producto

La API recibe JPG/JPEG, PNG y WebP de hasta 5 MiB. Comprueba extensión, tipo MIME y firma/estructura básica del archivo, y genera la ruta del objeto. Los archivos se guardan en el bucket público pcp-productos para mostrarlos en el catálogo.

Administrador puede gestionar imágenes de todas las categorías. Recepción puede hacerlo para alimentos. El servidor y las políticas de Storage validan el perfil activo y la categoría del producto.

## Petición

POST /api/productos/{id}/imagen con Authorization Bearer y multipart/form-data.

F23 añade [galería de hasta ocho fotos](PRODUCTOS-GALERIA.md): `agregar=true` suma una foto conservando la principal; sin ese campo la carga reemplaza la principal y mantiene las secundarias. Las consultas devuelven `imagenes`, con rutas para ordenar y retirar asociaciones. Se conserva el contrato de respuesta de carga.

- archivo contiene el fichero.
- idOperacion contiene un UUID nuevo para cada carga. Al reintentar la misma carga se conserva ese UUID y se envía el mismo archivo.

Ejemplo para el frontend, donde archivo es un File seleccionado y accessToken pertenece a la sesión de Supabase.

```typescript
const datos = new FormData();
datos.append('idOperacion', crypto.randomUUID());
datos.append('archivo', archivo);

const respuesta = await fetch('/api/productos/' + productoId + '/imagen', {
  method: 'POST',
  headers: { Authorization: 'Bearer ' + accessToken },
  body: datos,
});

```

El navegador añade el Content-Type con su boundary. La respuesta contiene id, productoId, url, tipoContenido, tamano y esImagenActual. Tras una carga completada, ImagenUrl del producto queda actualizada y aparece en las consultas existentes del catálogo. Si esImagenActual es false, otra carga ya sustituyó esa imagen y se debe volver a consultar el producto.

Los códigos de respuesta incluyen 400 para datos incompletos, 401 para una sesión inválida, 403 para falta de permisos, 404 para producto inexistente, 409 para reutilización incompatible de idOperacion, 413 para exceso de tamaño y 415 para formato rechazado. Los fallos del proveedor devuelven 502 o 504 y permiten reintentar con el mismo idOperacion.

## Consistencia

Antes de subir se reserva una ruta y se guarda su huella SHA-256. Un fallo de Storage conserva la imagen previa y una reserva pendiente. Si la subida termina pero falla el guardado final, el reintento verifica el objeto y completa la asignación. La imagen nueva y su estado se guardan en una misma transacción con auditoría.

Cada reemplazo crea un archivo nuevo. Las versiones anteriores se conservan. Repetir una operación ya completada devuelve su resultado y no restaura una imagen antigua encima de una carga posterior.

F22 permite [eliminar un producto creado por error](ELIMINAR-PRODUCTOS.md) junto con todas sus cargas completadas, exclusivamente si no tiene historial ni imágenes compartidas. Las cargas pendientes bloquean el borrado. Ocultar conserva los archivos y sigue siendo la vía habitual.

## Configuración

Supabase:Url apunta al proyecto. Supabase:PublishableKey contiene su clave pública. En desarrollo se ha guardado esta última en User Secrets. En despliegue se puede configurar como Supabase__PublishableKey.

El cliente Storage utiliza el JWT del usuario autenticado. La migración ImagenesProductos crea el bucket, sus límites, el registro de cargas y una política de inserción para rutas previamente reservadas por la API. Su función de permisos consulta el perfil real y el producto. El registro de cargas está protegido por RLS y sus permisos directos están revocados para anon y authenticated.

El backend que ya estaba en ejecución debe reiniciarse para cargar el endpoint nuevo. Swagger permite seleccionar el fichero y completar idOperacion.

## Verificación realizada

Las pruebas de validación y transporte cubren límites, extensiones, MIME, firmas, solicitudes HTTP y respuestas de error del proveedor. Las pruebas HTTP con PostgreSQL verifican permisos, asociación al producto, reemplazos y reintentos usando un proveedor Storage simulado.

También se comprobaron en el proyecto real el bucket, su límite y la función de permisos, con registros transaccionales revertidos. Queda pendiente la primera carga completa a Storage con un JWT real del usuario desde Swagger o el frontend.
