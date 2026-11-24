# Formulario y bandeja de contacto

Implementado el 24 nov 2026. Migración `20261124175502_SolicitudesContacto` aplicada en Supabase. Formulario `/contacto` y bandeja `/admin/contacto` (menú Mensajes) conectados en código, pendientes de desplegar y comprobar con una sesión real de Administrador.

## Recepción pública

`POST /api/contacto`, sin cuenta obligatoria, JSON:

```json
{
  "idOperacion": "UUID generado por el cliente",
  "nombre": "Nombre del visitante",
  "email": "visitante@example.com",
  "telefono": null,
  "motivo": "otro",
  "mensaje": "Necesito información del club.",
  "privacidad": true,
  "sitioWeb": ""
}
```

Nombre 2–150 caracteres; correo válido hasta 254; teléfono opcional, normalizado como 10 dígitos mexicanos con prefijo 52/+52 opcional; mensaje 10–2000 caracteres, sin contar espacios externos para el mínimo. Motivos: `reservacion`, `grupos`, `tienda`, `cotizacion` y `otro`. El motivo `cotizacion` se muestra como Información de catálogo y no crea un pedido ni un cobro.

Exige `privacidad=true`; registra la fecha UTC de recepción/aceptación que declara el formulario. No es firma de responsiva ni se reutiliza como autorización de llegada. Campo antiautomatización `sitioWeb` debe quedar vacío. Tamaño de petición limitado a 16 KiB.

Respuesta 201: `{ "id": "UUID", "estado": "Recibida" }`. Conservar el mismo UUID al reintentar el mismo contenido: no duplica registros. Si ese UUID se usa con otro contenido normalizado, devuelve 409; un mensaje distinto requiere UUID nuevo. El frontend mantiene la operación mientras el formulario está abierto, incluso tras errores de red; al cambiar el contenido o elegir Enviar otro mensaje crea otra operación. Recargar la página no conserva este identificador en almacenamiento persistente.

Repetir un envío recibido conserva el acuse público `Recibida`, incluso si Administración ya lo atendió o archivó. No revela notas, estado interno ni fechas de seguimiento. No existe consulta pública por ID, correo ni listado.

## Control de frecuencia

El POST admite 5 intentos por ventana fija de 5 minutos por dirección remota reportada a ASP.NET Core, sin cola; después devuelve 429 con `Retry-After` y ProblemDetails. Incluye intentos inválidos/reintentos, no solo mensajes nuevos. El frontend conserva los datos para reintentar.

No se confía directamente en una cabecera `X-Forwarded-For` enviada por el navegador ni se almacenan IP en las solicitudes. El límite es en memoria por instancia; reinicios o varias réplicas no comparten cuota. La infraestructura debe entregar correctamente la dirección remota si hay proxies. Es una protección básica junto con límites de datos y campo trampa; no incluye CAPTCHA ni servicio distribuido de prevención de abuso.

## Administración

Solo Administrador activo. Recepción/Cliente reciben 403, sin sesión 401. Sin caché para respuestas administrativas.

| Método/ruta bajo `/api/administracion/contacto` | Acción |
| --- | --- |
| GET | Bandeja paginada. Filtros `estado`, `motivo`, `busqueda` por nombre/correo, `desdeUtc` inclusivo y `hastaUtc` exclusivo sobre creación. |
| GET `/{id}` | Detalle de remitente, mensaje original, aceptación declarada de privacidad, estado, notas internas, versión y fecha de actualización. |
| PUT `/{id}` | Guardar `{estado, notasInternas, version}` completos. Versión actual obligatoria, 409 si cambió. |

Estados `Pendiente`, `Atendida`, `Archivada`; se puede reabrir una solicitud cambiando a Pendiente. Archivar no elimina el registro. Notas internas hasta 4000 caracteres. No se editan desde este endpoint los datos ni el mensaje originales. Lista por creación descendente/UUID; tamaño máximo 100, por defecto 20. Detalle inexistente: 404.

El panel permite búsqueda, motivo/estado, paginación y seguimiento. Mantiene las notas si hay 409 y ofrece recarga explícita; las lecturas antiguas no sustituyen una selección posterior. Mensajes y notas se muestran como texto escapado, sin interpretar HTML. Responder por correo abre el programa de correo del operador; guardar seguimiento no envía mensajes.

## Persistencia y límites

Tabla privada `SolicitudesContacto`, con restricciones de datos, índices, RLS y revocación de acceso directo para anon/authenticated. La escritura y su auditoría se guardan de forma atómica mediante SaveChanges. Las ediciones conservan actor en auditoría y versión. No se guardan contraseñas, IP ni tokens en las solicitudes.

Se conservan las solicitudes archivadas; no hay borrado físico ni purga automática en esta entrega. No se envían notificaciones por correo/WhatsApp, ni se suscribe al visitante al newsletter. Las plantillas F10, el correo público del sitio y Supabase Auth permanecen separados.

## Verificación y despliegue

377 pruebas locales aprobadas y 52 PostgreSQL omitidas en esa ejecución; una prueba PostgreSQL específica aprobada por separado verifica migración/RLS, flujo HTTP, filtros de fechas y motivo, idempotencia, notas y auditoría con transacciones reales. Frontend TypeScript/Vite/prerender compila. Revisión independiente sin hallazgos materiales; se deshabilitan campos durante el envío para no perder cambios aparentes. Concurrencia omitida por indicación del usuario.

Formulario público revisado visualmente en navegador local, sin enviar datos a producción. Bandeja con Administrador real y comprobación móvil completa pendientes. Publicar API antes de conectar la nueva versión del frontend en el sitio desplegado: hasta entonces un 404/error no se convierte en éxito simulado. No se hizo despliegue desde esta sesión.
