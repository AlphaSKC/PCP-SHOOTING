# F17, F16 y F21 · Marcas y portadas

Backend implementado y migración aplicada en Supabase el 23 nov 2026. Reutiliza la validación y el transporte de imágenes de productos, con buckets separados. Los archivos admitidos son PNG, JPG/JPEG y WebP. SVG no se admite en este bloque: el selector de marcas se ajustó al mismo contrato.

El bloque original F16/F17 y su verificación histórica se conservan abajo. F24 añadió categorías a las cargas de contenido y F21 añade portadas de página mediante tablas propias, descritas al final y en el contrato del roadmap. Las pruebas integradas posteriores reportadas por Thiago están en ROADMAP.md.

## F17 · Marcas

| Método y ruta | Acceso y resultado |
| --- | --- |
| `GET /api/marcas` | Público. Lista de activas, ordenada por `orden`, nombre e ID. |
| `GET /api/administracion/marcas` | Administrador activo. Todas, incluyendo `activo` y `version`. |
| `POST /api/administracion/marcas` | Administrador. Crea y devuelve la marca, 201. |
| `PUT /api/administracion/marcas/{id}` | Administrador. Edita con la versión vigente, 200; antigua: 409. |
| `POST /api/administracion/marcas/{id}/logo` | Administrador. Archivo hasta 2 MiB; devuelve `logoUrl` y nueva `version`. |
| `DELETE /api/administracion/marcas/{id}/logo` | Administrador. Desvincula el logo, 204. |

Crear/editar recibe `nombre` obligatorio (máximo 80), `descripcion` opcional (60), `url` opcional HTTPS sin credenciales (2048), `activo` y `orden` entero no negativo. PUT requiere además `version`. Los campos públicos son `id`, `nombre`, `descripcion`, `url`, `logoUrl`, `orden`. Los campos administrativos son los mismos más `activo` y `version`.

No hay borrado físico de marcas: usar `activo=false`. La migración incorpora las seis marcas ya incluidas en los datos del frontend: Airguns FV, Altaros, ZAN Projectiles, Donny FL, AEA y H&V. Sin logos, descripciones ni URLs inventadas; pueden completarse desde el panel.

El panel y la portada existentes ya llaman estas rutas en modo API. Se ajustaron los formatos del selector y el manejo de versiones/reintentos del panel: después de subir/quitar el logo se consulta la versión actual. Una carga fallida conserva su operación mientras no cambien los campos; una edición posterior requiere otra operación.

## F16 · Portada del blog

`POST /api/administracion/blog/{id}/portada`, solo Administrador activo. JPG/JPEG, PNG o WebP hasta 5 MiB. Respuesta 200:

```json
{
  "imagenUrl": "https://PROYECTO.supabase.co/storage/v1/object/public/pcp-blog/blog/ARTICULO/OPERACION.png",
  "version": "UUID actualizado del artículo"
}
```

Primero crear/guardar el artículo para obtener su ID. La carga cambia únicamente la portada, versión y fecha de actualización; no publica borradores ni altera la programación. La URL aparece en las consultas existentes cuando el artículo es visible. Se mantiene la alternativa de pegar una URL HTTPS en el editor y la eliminación por edición con `imagenUrl=null` y versión vigente.

El frontend aún debe añadir el selector y llamar a este endpoint. Tras cargar, actualizar la URL y la versión del editor o volver a consultar el artículo antes de guardar otros campos. El botón de archivo del blog no se agregó en este cambio.

## Formulario de carga y reintentos

Las dos rutas de carga usan `Authorization: Bearer TOKEN_REAL` y `multipart/form-data`:

```typescript
const datos = new FormData();
datos.append('archivo', archivo);
datos.append('idOperacion', operationId); // conservar UUID y archivo al reintentar
```

El navegador debe añadir el Content-Type con su boundary. No reutilizar un ID para otro archivo, usuario o destino. El backend comprueba firma/estructura básica, extensión, MIME, tamaño y SHA-256, y genera la ruta. Las cargas no utilizan el nombre de archivo para construir rutas.

Se reserva el registro antes de llamar a Storage; la URL se asigna después de completar la carga, en transacción con auditoría. Un fallo del proveedor conserva la imagen anterior y permite reintentar. Se comprueban permisos y versión del destino antes y después de Storage. Una edición, sustitución o retirada posterior produce 409 en operaciones antiguas; no restaura imágenes previas. Ante 409, recargar y usar una nueva operación tras revisar el contenido.

Códigos: 400 datos inválidos/vacíos, 401 sin sesión Bearer, 403 rol/permiso insuficiente, 404 destino inexistente, 409 operación incompatible o versión cambiada, 413 tamaño, 415 formato, 502/504 fallo del proveedor. Recepción y Cliente no gestionan marcas ni portadas.

## Persistencia y Storage

`MarcasAliadas` y `CargasImagenContenido`: RLS habilitado y permisos directos revocados para `anon`/`authenticated`. Cada carga queda ligada a un creador y exactamente una marca o artículo. Los registros y objetos anteriores se conservan para auditoría/reintentos; quitar el logo desvincula la URL, sin borrar físicamente el objeto.

Buckets públicos `pcp-marcas` (2 MiB) y `pcp-blog` (5 MiB). Son archivos públicos por URL; no están pensados para documentos privados. Se utiliza el JWT del usuario y la clave pública Supabase ya configurados, sin añadir claves privilegiadas al frontend. La política de Storage permite insertar solo en la ruta previamente reservada, por su administrador activo, con creador y versión vigentes. No habilita sobrescritura ni borrado directo.

Migración: `20261123223905_MarcasYPortadasBlog`, aplicada. Reiniciar el backend y recargar el frontend. No se modificaron las políticas/bucket de imágenes de productos.

## Verificación y alcance pendiente

- 310 pruebas locales aprobadas, cero fallidas y 44 PostgreSQL omitidas en esa ejecución; concurrencia excluida por indicación del usuario.
- Tres comprobaciones PostgreSQL aprobadas por separado: migración/asignación con Storage simulado en esquema temporal, buckets/permisos reales de la nueva función y compatibilidad de la migración anterior del blog con la nueva FK.
- Compilación TypeScript/Vite y prerender completados. Revisión sin hallazgos importantes.
- Pendiente una carga de logo y portada con JWT real desde el sitio/Swagger, y conectar el selector de portadas. No se presenta la simulación ni la compilación como prueba completa de integración.

## F21 · Portadas de página · 27 nov 2026

Contrato principal en [ROADMAP.md](ROADMAP.md#f21--portadas-de-página-contrato-para-backend--25-nov-2026). Se implementan sus cuatro rutas: GET público `/api/portadas`, GET administrativo `/api/administracion/portadas`, PUT `/{pagina}` y POST `/{pagina}/imagen`. La gestión exige Administrador activo; no se modifica frontend. No se crean filas públicas iniciales: la ausencia de configuración mantiene el comportamiento de respaldo del frontend.

Claves exactas admitidas: inicio, nosotros, reservar, catalogo, tienda, eventos, alimentos, membresias y contacto. Otras claves devuelven 404. GET público devuelve solo `pagina`, `activa`, `imagenUrl`, `posicion`, `velo`; administración añade versión y fecha UTC. Una portada desactivada sigue en el listado público con `activa=false` para poder quitar el respaldo de Inicio. Sin caché ni datos de auditoría públicos.

PUT exige `activa`; activa requiere imagen. Admite una imagen del bucket público `pcp-portadas` del proyecto configurado en `Supabase:Url`, sin credenciales/query/fragmento, o una ruta local `/img/` sin recorrido de directorios. Se puede reutilizar URL de otra página. Posición con enteros 0–100 en formato `X% Y%`, predeterminado `50% 50%`; velo suave/normal/fuerte, predeterminado normal. Versión nula en primera configuración; fila ya configurada exige su versión vigente o responde 409. Primera configuración concurrente también devuelve 409 en vez de sobrescribir. Las validaciones de cuerpo inválido responden 400. PUT reemplaza el documento completo, incluida la URL; no hay PATCH ni borrado físico.

Carga multipart: `archivo` e `idOperacion`, PNG/JPG/WebP hasta **8 MiB**; límite HTTP 9 MiB para formulario. Ruta generada `portadas/{pagina}/{idOperacionN}.{ext}`, sin usar el nombre del archivo. Usa JWT del Administrador y clave pública Supabase. La carga crea configuración activa por defecto si no existía; al cargar sobre una fila configurada conserva `activa`, enfoque y velo. Cambia URL/versión solo después de éxito. Reintento con el mismo ID, autor, destino y huella no duplica ni restaura una imagen sustituida; conflicto de versión/operación responde 409. 503 sin Storage configurado, 502/504 si falla el proveedor; fallo conserva la portada anterior.

Para la primera carga se reserva una fila **interna no configurada**, excluida de ambos listados hasta completar la asociación. Si falla la primera subida no se publica una portada vacía ni se desactiva el respaldo. Una escritura PUT posterior puede configurar esa página; cambia su versión e invalida la carga pendiente antigua. La configuración textual y los listados no necesitan resolver Storage ni su clave; solo la ruta de carga lo requiere.

Tablas `PortadasPagina` y `CargasPortadaPagina`, sin datos iniciales, con RLS y permisos directos revocados. La configuración usa un UUID interno de identidad para la auditoría común y `Pagina` única para las rutas; el UUID y el indicador interno `Configurada` no se exponen por API. Cada carga se vincula a autor, versión de portada y ruta reservada. Configuración/asociación y auditoría se guardan atómicamente. No se guardan los bytes de imagen en tablas.

Bucket público `pcp-portadas`, máximo 8 MiB y tres tipos MIME. Función con search_path vacío y referencias completas; inserción solo para la carga pendiente de su mismo Administrador activo y con versión vigente. No se añade sobrescritura ni borrado directo. Desactivar no revoca URLs conocidas; se conservan archivos previos. El transporte compartido verifica duplicados por longitud esperada y SHA-256, permitiendo recuperar también portadas mayores de 5 MiB sin ampliar los límites de productos/logos/blog.

Migración aditiva `20261127180413_PortadasPagina` aplicada en Supabase el 27 nov. Su reversión retira política/función y tablas, conservando bucket/archivos públicos. Verificación de cierre en ROADMAP.md. Pendiente publicar API, recargar el frontend para salir del modo de respaldo y comprobar carga/edición/reutilización con sesión real en el panel de Thiago.
