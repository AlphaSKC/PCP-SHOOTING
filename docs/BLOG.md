# Blog

## Acceso y endpoints

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/api/blog` | Público: artículos visibles paginados, sin contenido completo. |
| GET | `/api/blog/{slug}` | Público: detalle con contenido Markdown. |
| GET | `/api/administracion/blog` | Administrador activo: todos los estados. |
| GET | `/api/administracion/blog/{id}` | Administrador activo: detalle editable con versión. |
| POST | `/api/administracion/blog` | Administrador activo: crear; responde 201 con Location. |
| PUT | `/api/administracion/blog/{id}` | Administrador activo: sustituir datos, publicar, programar, devolver a borrador o archivar. |

Las operaciones administrativas requieren `Authorization: Bearer <access_token>`. Recepción y Cliente no gestionan contenido. Archivar conserva el artículo y su auditoría; no hay borrado físico ni comentarios de lectores.

## Datos de escritura

```json
{
  "slug": "noticias-del-club",
  "titulo": "Noticias del club",
  "resumen": "Conoce las novedades del club.",
  "contenidoMarkdown": "## Bienvenidos\n\n- Primera novedad\n- Segunda novedad",
  "categoria": "Club",
  "imagenUrl": "https://example.com/portada.jpg",
  "destacado": false,
  "estado": "Borrador",
  "publicarEnUtc": null
}
```

`PUT` recibe todos esos campos más `version`, copiada del último GET administrativo o de la última escritura. Si falta, responde 400; si otra edición ya cambió el registro, responde 409. El servidor genera una nueva versión al guardar. El autor inicial se obtiene de la sesión y se conserva al editar.

Límites: slug 160, título 200, resumen 600, contenido Markdown 100000 e imagen URL 2048 caracteres. Los textos obligatorios no aceptan espacios vacíos. El slug debe usar minúsculas ASCII, números y guiones simples entre palabras; se elige explícitamente y es único incluso en artículos archivados. Editarlo cambia la dirección pública sin crear una redirección.

La portada es opcional y debe ser una URL HTTPS absoluta sin credenciales. Además de referenciar imágenes ya alojadas, F16 agrega `POST /api/administracion/blog/{id}/portada` para cargar PNG/JPG/WebP hasta 5 MiB a Storage. Devuelve URL y versión actualizada; no publica borradores. Ver [contrato de carga](MARCAS-Y-PORTADAS.md). El servidor no descarga las URLs introducidas manualmente. Markdown se guarda y devuelve como texto: el frontend deberá usar un renderizador seguro, sin HTML crudo y con enlaces seguros; no debe insertarlo directamente como HTML.

## Categorías y estados

Categorías: `Guias`, `Tecnica`, `Eventos`, `Club` (valores internos 0, 1, 2, 3). Estados: `Borrador`, `Programado`, `Publicado`, `Archivado` (0, 1, 2, 3). JSON devuelve los nombres; las peticiones aceptan nombres sin distinguir mayúsculas o los valores numéricos válidos.

- Borrador: nunca público, sin fecha de publicación.
- Programado: requiere `publicarEnUtc` futura en ISO 8601 con zona horaria. Se guarda en UTC.
- Publicado: aparece cuando su fecha sea menor o igual al reloj UTC del servidor. Si no se proporciona fecha al publicar, se conserva una fecha previa ya alcanzada o se usa la actual. Una fecha futura requiere el estado Programado.
- Archivado: nunca público, conserva la fecha anterior si no se envía otra. Se archiva mediante PUT con ese estado y la versión actual.

La programación se resuelve en las consultas, sin cron ni proceso en segundo plano. Al llegar la fecha, un registro programado aparece públicamente y la API administrativa lo presenta como Publicado; el estado almacenado sigue siendo Programado hasta la próxima escritura. Este cambio de visibilidad no modifica su versión ni genera un evento de edición.

## Filtros

Ambos listados aceptan `busqueda` (título o resumen, hasta 150 caracteres, sin distinguir mayúsculas), `categoria`, `destacado`, `pagina` (1 por defecto) y `tamanoPagina` (20 por defecto, máximo 100). Devuelven `{ elementos, total, pagina, tamanoPagina }`.

El listado administrativo admite además `estado`, que usa el estado efectivo según la fecha. El público siempre impone visibilidad; enviar un estado u otros parámetros administrativos no permite ver borradores. El detalle de un borrador, programado futuro, archivado o slug inexistente devuelve 404. El detalle público no revela autor interno, versión ni fechas de gestión.

## Base de datos y pruebas

La migración `BlogArticulos` crea la tabla, restricciones, clave de autor, token de concurrencia e índices de slug, estado/fecha y categoría/fecha. Activa RLS y revoca el acceso directo de `anon` y `authenticated`: las lecturas públicas pasan por la API C#, no por PostgREST. La conexión del backend debe conservar su acceso al esquema.

Desde la raíz del repositorio, aplicar las migraciones pendientes antes de usar los endpoints:

```powershell
.\.tools\dotnet-ef database update --configuration StorageCheck --project PCP-Shooting/PCP-Shooting
```

Las pruebas incluyen HTTP, permisos, programación con reloj controlado, archivo, filtros, slug único, auditoría, concurrencia y ejecución de la migración en un esquema PostgreSQL temporal. No se insertan artículos de ejemplo en producción. El frontend existente sigue usando sus datos de demostración hasta que se conecte a estos endpoints.
