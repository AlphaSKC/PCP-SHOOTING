# Galería de fotografías

Implementada el 24 nov 2026. API, gestión y página pública conectadas en código; migración `20261124171633_GaleriaInicial` aplicada en Supabase. Pendientes despliegue y primera carga con sesión real de Administrador. No contiene fotografías ni álbumes ficticios iniciales.

## Consulta pública

- `GET /api/galeria/albums?pagina=1&tamanoPagina=20`: página de álbumes publicados que tienen al menos una foto publicada con imagen. Campos `id`, `nombre`, `descripcion`, `orden`, `portadaUrl`, `cantidadFotos`.
- `GET /api/galeria/fotos?albumId=UUID&pagina=1&tamanoPagina=20`: página de fotos visibles de álbumes publicados; omitir álbum para ver todas. Campos `id`, `albumId`, `titulo`, `textoAlternativo`, `descripcion`, `orden`, `imagenUrl`.

Formato paginado habitual: `elementos`, `total`, `pagina`, `tamanoPagina`; máximo 100. Orden ascendente por orden/UUID. Portada del álbum: primera foto pública según ese orden. Ocultar la portada hace que la siguiente foto visible ocupe su lugar. Un álbum vacío o con todas las fotos ocultas no aparece en público. Las rutas públicas no entregan versiones, actores ni cargas pendientes.

## Gestión (solo Administrador activo)

| Método y ruta bajo `/api/administracion/galeria` | Acción |
| --- | --- |
| GET `albums` | Listado paginado, incluye ocultos y versiones. |
| POST `albums` | Crear con nombre, descripción opcional, orden y publicado. |
| PUT `albums/{id}` | Editar los mismos datos más versión actual. |
| GET `albums/{id}/fotos` | Fotos paginadas del álbum, ocultas y publicadas. |
| POST `albums/{id}/fotos` | Crear ficha: título, texto alternativo, descripción opcional, orden, publicada=false e `idOperacion` opcional. |
| PUT `fotos/{id}` | Editar título, texto alternativo, descripción, orden, publicada y versión actual. |
| POST `fotos/{id}/imagen` | Multipart con `archivo` e `idOperacion` UUID; requiere JWT Bearer del administrador. |

Nombre del álbum: 120 caracteres, descripción 500; título de foto 150, texto alternativo obligatorio 250 y descripción 1000. Orden entero no negativo. Crear no publica por defecto. Publicar una foto sin imagen da 400. Al crear una ficha, frontend utiliza un UUID estable en `idOperacion`: repetir misma ficha/álbum no duplica; otros datos con ese UUID dan 409. Para cambiar datos de una ficha existente usar PUT.

Editar requiere `version` UUID actual; 409 indica recargar y revisar antes de guardar. La carga devuelve `{ "url": "...", "version": "..." }`; utilizar esa versión para publicar/editar. Un reintento de carga completado conserva la URL vigente y no vuelve a publicar ni restaurar una imagen sustituida. No hay borrado físico ni movimiento entre álbumes en esta entrega.

## Archivos y Storage

Bucket público `pcp-galeria`; rutas `galeria/{fotoId:N}/{idOperacion:N}.{ext}` generadas por servidor. JPG/PNG/WebP, máximo 5 MiB, verificación de firma/estructura básica y coincidencia de extensión/MIME mediante el validador existente. No se decodifica ni transforma la imagen, ni se admiten SVG/videos. Carga fallida deja ficha oculta recuperable; reintentar mismo archivo/operación. Cambios incompatibles o foto editada durante una carga pendiente devuelven 409; revisar y usar una operación nueva.

Las tablas tienen RLS y revocación de acceso directo para anon/authenticated. Storage solo permite insertar mediante una reserva de carga propia, Administrador activo y versión vigente. No se usa service_role en frontend ni se habilita sobrescritura/borrado. El bucket es público: ocultar retira de la API, pero no revoca enlaces ya compartidos ni elimina el archivo. No guardar documentos privados en esta galería.

## Pantallas y despliegue

En modo API, `/admin/contenido` → Galería administra álbumes/fotos, selecciona varios archivos, muestra resultados individuales, reintenta y publica explícitamente. Se puede recuperar una ficha sin imagen o reemplazar su imagen desde su editor; guardar primero los cambios de texto. El orden de las fotos define la portada.

`/galeria` consulta los álbumes reales, filtra y carga más fotos con visor. Hay estados vacío/error y reintento; el modo demo anterior permanece separado. La pestaña administrativa conserva los permisos del panel.

Para usarlo en el sitio publicado hay que subir los cambios y desplegar API y frontend. El frontend local se dejó apuntando nuevamente a Azure después de una comprobación temporal contra API local. Hasta desplegar la API nueva, Azure no tendrá estas rutas aunque la migración ya esté aplicada.

## Evidencia y límites

355 pruebas locales aprobadas, 50 PostgreSQL omitidas en esa ejecución; tres comprobaciones PostgreSQL aprobadas por separado: migración/RLS y consultas/cargas con transporte simulado; función de autorización de Storage en transacción revertida; provisión real del bucket/permisos después de migrar. Modelo sincronizado. Compilación TypeScript/Vite/prerender correcta; avisos de anotaciones de dependencias Zod preexistentes, sin fallo de compilación. Revisión corrigió filtros repetidos, paginación y manejo de conflictos del editor.

Se verificó en navegador el estado vacío público contra API local y denegación del panel con la sesión no administrativa disponible. No se creó contenido de prueba público ni se realizó una carga con JWT real de Administrador. Primera carga, publicación/visor con fotos reales y comprobación visual móvil/administrativa completa quedan pendientes. Concurrencia omitida por indicación del usuario.
