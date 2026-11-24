# Eventos informativos

Entrega parcial de eventos aprobada por Kevin el 24 nov 2026. Backend para publicar información; la integración frontend queda a cargo de Thiago. La operación de inscripciones permanece en OQ-04.

## Alcance

Título, slug único, resumen, descripción Markdown, ubicación opcional, imagen opcional mediante URL HTTPS y fechas de inicio/fin. Estados `Borrador`, `Publicado` y `Archivado`. Solo Administrador activo puede gestionar. Borradores y archivados nunca aparecen en consulta pública, aunque se conozca su slug.

Los eventos no ocupan líneas ni alteran reservas, pagos, inventario o disponibilidad. No se ofrecen inscripciones ni se almacenan cupo, precio, participantes o asistencia. No existe un botón de compra ni un endpoint de registro en este contrato. Archivar retira información pública y conserva el registro; no representa cancelar un evento, una inscripción ni un cobro.

## Rutas

| Método y ruta | Acceso | Respuesta |
| --- | --- | --- |
| GET `/api/eventos` | Público | Lista paginada de publicados, incluidos los pasados. |
| GET `/api/eventos/{slug}` | Público | Detalle publicado; 404 para inexistente, borrador o archivado. |
| GET `/api/administracion/eventos` | Administrador | Lista paginada de todos los estados. |
| GET `/api/administracion/eventos/{id}` | Administrador | Detalle con estado, versión y fechas de auditoría. |
| POST `/api/administracion/eventos` | Administrador | 201 con detalle y `Location` consultable. |
| PUT `/api/administracion/eventos/{id}` | Administrador | Sustitución de campos editables; exige versión vigente. |

Listados: `pagina` desde 1, `tamanoPagina` entre 1 y 100 (predeterminado 20), `busqueda` hasta 150 caracteres en título/resumen sin distinguir mayúsculas, `desdeUtc` y `hastaUtc` opcionales. El rango se aplica al **inicio del evento**, incluye desde y excluye hasta; si ambos existen, hasta debe ser posterior a desde. No es búsqueda de solapamiento.

`proximos=true` filtra eventos cuyo fin todavía no llegó, incluidos los que están en curso. `proximos=false` filtra finalizados (`finUtc <= ahora`). Omitirlo conserva ambos. No confundir este filtro con el estado de publicación. Administración añade `estado=Borrador|Publicado|Archivado`.

Orden por inicio ascendente y UUID ascendente como desempate. Respuesta `{ elementos, total, pagina, tamanoPagina }`. Una lista vacía es válida. La lista pública omite el cuerpo Markdown; el detalle lo incluye. Ninguno devuelve versión ni fechas de auditoría. Las respuestas llevan `Cache-Control: no-store`.

## Crear y editar

```json
{
  "slug": "encuentro-del-club",
  "titulo": "Encuentro del club",
  "resumen": "Información y programa del encuentro.",
  "descripcionMarkdown": "## Programa\n\n- Bienvenida\n- Actividades",
  "ubicacion": "PCP Shooting",
  "imagenUrl": "https://example.com/evento.webp",
  "inicioUtc": "2027-01-10T10:00:00-05:00",
  "finUtc": "2027-01-10T12:00:00-05:00",
  "estado": "Borrador"
}
```

Ejemplo ilustrativo, sin crear eventos de muestra en la base. Al editar enviar también `version` (UUID del último detalle), junto con el documento completo. Fechas y estado son obligatorios en JSON; se deben conservar los campos opcionales al editar para no borrarlos. `ubicacion` e `imagenUrl` pueden omitirse o enviarse nulos para quitarlos. La edición genera otra versión, incluso si se envían los mismos valores.

Validaciones: slug de 1–160 caracteres con minúsculas ASCII, números y guiones internos; título obligatorio hasta 200; resumen obligatorio hasta 600; descripción obligatoria hasta 30000; ubicación hasta 250; imagen hasta 2048. Se recortan espacios exteriores de textos. Imagen requiere URL absoluta HTTPS sin credenciales, por ejemplo de Storage público de Supabase; el servidor guarda la URL y no descarga el recurso. Esta entrega no añade un endpoint de subida de imágenes ni un bucket de eventos. El consumidor debe renderizar Markdown sin HTML crudo y usar la imagen como recurso visual, no como HTML.

Inicio y fin aceptan ISO 8601 con `Z` o desplazamiento. Fin debe ser estrictamente posterior al inicio. Se normalizan y devuelven en UTC: el ejemplo corresponde a 15:00–17:00 UTC. Frontend presenta las fechas en la zona del club (Cancún) y debe enviar el desplazamiento correcto. Se permiten fechas pasadas para conservar información histórica; publicar es inmediato y no está ligado al inicio de la actividad. No hay programación de publicación en esta entrega.

Estado publicado no implica inscripción abierta, pago gratuito ni lugar disponible. Los publicados pasados siguen consultables hasta archivarlos. Cambiar slug cambia su URL pública; no se crean redirecciones automáticas.

Errores: 400 validación o falta de versión al editar; 401 sin sesión; 403 sin Administrador activo (incluye Recepción); 404 inexistente/no público; 409 slug repetido o versión antigua. Ante 409 volver a consultar y resolver el conflicto; no sobrescribir automáticamente. No hay borrado físico.

## Persistencia y pendientes

Tabla `Eventos`, índice único de slug, índices de estado/inicio y fin, restricciones de fechas/estado/textos, versión de concurrencia y auditoría en la misma escritura. Migración aditiva `20261124215554_EventosInformativos` aplicada en Supabase, sin datos iniciales. RLS y revocación de acceso directo para `anon`/`authenticated`; las consultas se realizan mediante la API.

Pendientes OQ-04: cupo y unidad de inscripción, cuenta/participantes/menores, reglas de precio y pago, bloqueo temporal, asistencia, relación con líneas, cancelaciones/reprogramaciones y reporte operativo F15. La base informativa no da por resueltos esos puntos. Publicación de la API e integración de pantallas pendientes.
