# Avisos públicos

Backend disponible. La integración y el diseño del frontend quedan a cargo del compañero de frontend por indicación del usuario (24 nov 2026).

Este módulo publica mensajes informativos del sitio. Es independiente de las plantillas de correo/WhatsApp de F10 (`/api/administracion/avisos`) y no modifica horarios, disponibilidad, apartados ni reservas.

## Consultas

| Método y ruta | Acceso | Resultado |
| --- | --- | --- |
| GET `/api/avisos-publicos` | Anónimo | Solo avisos activos y vigentes; sin versión ni auditoría. |
| GET `/api/administracion/avisos-publicos` | Administrador activo | Todos los avisos, con estado calculado y versión. |
| GET `/api/administracion/avisos-publicos/{id}` | Administrador activo | Detalle; 404 si no existe. |
| POST `/api/administracion/avisos-publicos` | Administrador activo | Crea; 201 con detalle y `Location` consultable. |
| PUT `/api/administracion/avisos-publicos/{id}` | Administrador activo | Sustituye los campos editables; requiere versión vigente. |

Ambos listados admiten `pagina` (desde 1) y `tamanoPagina` (1–100; predeterminado 20). Respuesta `{ elementos, total, pagina, tamanoPagina }`. Orden: `orden` ascendente, fecha de creación descendente y `id` ascendente como desempate.

El listado administrativo admite `estado=Inactivo|Programado|Vigente|Vencido` y `busqueda` (hasta 120 caracteres; título/texto sin distinguir mayúsculas). Cliente y Recepción no pueden administrar.

## Escritura

Ejemplo de cuerpo completo para crear:

```json
{
  "titulo": "Aviso del club",
  "texto": "Consulta los horarios de atención antes de tu visita.",
  "activo": true,
  "orden": 0,
  "inicioUtc": "2026-12-01T15:00:00Z",
  "finUtc": "2026-12-02T15:00:00Z"
}
```

Para editar se envían todos esos campos y `version` (UUID del último detalle). `texto`, `activo`, `orden`, `inicioUtc` y `finUtc` deben estar presentes; las fechas admiten `null`. No es un PATCH. Título opcional hasta 120 caracteres, texto obligatorio hasta 1000 y orden entero no negativo. Se recortan espacios exteriores. Texto y título son texto plano: el consumidor debe mostrarlos como texto, sin interpretar HTML ni Markdown.

Se aceptan fechas ISO 8601 con `Z` o desplazamiento; se guardan y devuelven en UTC. Si ambas existen, fin debe ser posterior al inicio. Inicio nulo publica inmediatamente al activar; fin nulo mantiene vigencia indefinida. Para desactivar, editar con `activo=false`: se conserva registro y auditoría, sin borrado físico.

Estados calculados con el reloj UTC de la API:

- **Inactivo:** `activo=false`, con independencia de las fechas.
- **Programado:** activo, pero aún no llega el inicio.
- **Vencido:** activo y ya se alcanzó el fin.
- **Vigente:** activo, inicio alcanzado (o nulo) y fin todavía no alcanzado (o nulo).

El instante inicial se incluye; el final se excluye. La API filtra al consultar, sin tarea programada ni cambio de `activo`. Un cliente que mantiene la pantalla abierta debe volver a consultar para reflejar vencimientos; las respuestas llevan `Cache-Control: no-store`.

Administración recibe además `estado`, `version`, `creadoEnUtc` y `actualizadoEnUtc`. El público recibe únicamente `id`, `titulo`, `texto`, `orden`, `inicioUtc` y `finUtc`. No existe detalle público por ID ni publicación de borradores. Lista vacía es una respuesta válida.

Errores: 400 datos inválidos o versión ausente al editar; 401 sin sesión; 403 sin rol; 404 inexistente; 409 versión desactualizada (consultar otra vez antes de reintentar). No sobrescribir automáticamente un conflicto. Los cambios se auditan junto con la escritura; la tabla tiene RLS y acceso directo revocado a `anon`/`authenticated`. Las consultas públicas se realizan mediante la API.

## Persistencia y verificación

Migración aditiva `20261124180906_AvisosPublicos` aplicada en Supabase, sin datos de ejemplo. Las pruebas cubren permisos, límites temporales, desactivación, conflicto de versión, validación, consultas reales PostgreSQL, migración, RLS y auditoría. Se omiten pruebas de concurrencia por indicación del usuario.

Pendiente publicar la API y que frontend implemente la consulta pública y la gestión administrativa. No se cambió frontend en este bloque.
