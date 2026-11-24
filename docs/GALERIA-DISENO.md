# Galería del sitio · diseño para revisión

Fecha: 24 nov 2026. Estado: diseño y ejecución aceptados por Kevin; implementado según [contrato y evidencias](GALERIA.md). Primera carga con Administrador y despliegue pendientes.

## Objetivo y alcance

Sustituir la demostración de `/galeria` y la pestaña Galería de `/admin/contenido` por fotografías reales administradas desde la API. Esta galería es independiente de las imágenes de productos, del blog y de los futuros eventos. No requiere catálogo ni fotografías definitivas para construir el módulo.

Primera entrega: álbumes, fotografías, carga a Storage, orden, portada de álbum, visibilidad y visor público. Solo fotografías JPG/PNG/WebP, máximo 5 MiB por archivo; videos quedan fuera de esta entrega. No se inventan álbumes ni fotos reales como datos iniciales. Se conservan los componentes y estilo existentes.

## Datos y reglas

- `AlbumGaleria`: UUID, nombre (120), descripción opcional (500), orden no negativo, publicado, versión UUID y fechas UTC de creación/actualización. Nace oculto salvo publicación explícita.
- `FotoGaleria`: UUID, álbum obligatorio, título (150), texto alternativo obligatorio (250), descripción opcional (1000), orden no negativo, publicada, versión y fechas UTC. Ruta/URL de imagen solo las asigna el servidor después de verificar la carga.
- `CargaFotoGaleria`: UUID de operación, foto, actor, ruta, huella SHA-256, tipo, tamaño, versión de destino y estado de carga. Sigue el patrón de cargas idempotentes existente; no es un catálogo público.
- Una foto pertenece a un solo álbum. Para esta entrega no se mueve entre álbumes ni se borra físicamente: ocultar conserva el historial y permite corregir/republicar.
- La portada es la primera foto publicada y disponible según orden y UUID. Administrar el orden permite elegirla sin una segunda relación que pueda apuntar a otro álbum o a una foto oculta.
- Publicar una foto exige imagen cargada y título/texto alternativo válidos. Un álbum sin fotos publicadas disponibles no aparece en público. Ocultar el álbum retira todas sus fotos del listado público sin cambiar el estado individual de cada foto.
- Ediciones requieren versión actual; conflicto devuelve 409. Cargas repetidas con el mismo UUID y archivo son idempotentes. Una carga antigua no puede restaurar una imagen sustituida ni revertir cambios posteriores.
- Orden estable por orden y UUID. Listados paginados, tamaño máximo 100; consultas públicas excluyen siempre cargas incompletas, fotos ocultas y álbumes ocultos.

## API

| Ruta | Acceso y resultado |
| --- | --- |
| `GET /api/galeria/albums` | Público; álbumes publicados con portada y cantidad de fotos visibles. |
| `GET /api/galeria/fotos?albumId=...&pagina=1&tamanoPagina=20` | Público; fotos publicadas, filtrables por álbum, sin datos de auditoría/cargas. |
| `GET /api/administracion/galeria/albums` | Administrador activo; incluye ocultos y versiones. |
| `POST /api/administracion/galeria/albums` | Administrador; crear álbum. |
| `PUT /api/administracion/galeria/albums/{id}` | Administrador; editar metadatos, orden y publicación con versión. |
| `GET /api/administracion/galeria/albums/{id}/fotos` | Administrador; listar fotos y cargas terminadas del álbum. |
| `POST /api/administracion/galeria/albums/{id}/fotos` | Administrador; crear ficha de fotografía oculta. |
| `PUT /api/administracion/galeria/fotos/{id}` | Administrador; editar título, texto alternativo, descripción, orden y publicación con versión. |
| `POST /api/administracion/galeria/fotos/{id}/imagen` | Administrador; multipart con archivo y UUID de operación; devuelve imagen y versión actualizada. |

Recepción y Cliente no administran la galería. Los errores siguen los contratos existentes: 400 validación, 401 sin sesión, 403 rol insuficiente, 404 inexistente, 409 conflicto. Todas las escrituras conservan auditoría y la configuración de transacciones existente.

## Storage y migración

Bucket separado `pcp-galeria`, con rutas generadas por el servidor y verificación de tamaño, formato y firma del archivo reutilizando el validador de imágenes. No aceptar SVG ni direcciones externas como sustituto de la carga. La API usa el JWT del administrador para subir con las políticas de Storage; ninguna clave privilegiada se envía al navegador.

Bucket de lectura pública, coherente con las fotos publicadas del sitio. **La visibilidad controla los listados de la API: ocultar una foto no revoca una URL pública previamente compartida.** No es almacenamiento de documentos privados, firmas ni responsivas. La eliminación definitiva de archivos y su retención quedan fuera de esta entrega.

Migración con tablas, claves foráneas, índices, restricciones, tokens de versión y RLS para impedir acceso directo de usuarios a las tablas de negocio. Políticas de carga limitadas a un Administrador activo y una operación de carga registrada, siguiendo marcas/blog. Probar la migración y las políticas en esquema temporal antes de aplicar en Supabase; no alterar buckets anteriores.

## Frontend

- Público: leer álbumes y fotos reales, filtros dinámicos, carga paginada, visor existente, textos alternativos, estados de carga/error/vacío y ausencia de datos ficticios en modo API.
- Administración: lista/editor de álbumes; alta de ficha y carga de fotografías, edición de texto alternativo, orden, publicación/ocultación y versión. Cada archivo lleva su UUID de operación estable para reintentos; mostrar errores individuales sin afirmar que terminó toda la selección.
- La carga múltiple del selector se procesa por archivo y conserva los resultados parciales. Un fallo deja la ficha oculta para reintentar; no publica fotos sin imagen.
- El contrato de esta galería no modifica productos, blog, reservas ni pagos.

## Verificación y cierre

Casos necesarios: permisos, validación de formatos/tamaño, reintentos y versiones, publicación sin imagen, portada y orden, ocultación del álbum/foto, paginación, aislamiento de cargas, errores de Storage y traducción SQL real. Mantener omitidas las pruebas de concurrencia por indicación del usuario.

Ejecutar pruebas locales relevantes, comprobación PostgreSQL/migración y compilación del frontend. Documentar por separado lo simulado, migración aplicada, carga real con JWT y despliegue. Actualizar roadmap y guía de contratos y entregar mensaje de commit en inglés. El sitio solo se marca integrado después de comprobar el recorrido con una sesión real.
