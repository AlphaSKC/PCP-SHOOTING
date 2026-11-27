# F10 · Configuración de avisos

Persistencia de los tres campos que muestra `/admin/configuracion`: correo remitente y dos plantillas de WhatsApp. Implementado en backend el 23 nov 2026; frontend conectado el 25 nov según el roadmap. F20 añade lectura limitada para Recepción el 27 nov, pendiente publicar e integrar esa nueva ruta. Envío efectivo y proveedor pertenecen a R07. Guardar un correo no verifica su dominio ni habilita el envío.

## Contrato

Solo JWT con perfil activo y rol Administrador. Sin sesión: 401; Cliente, Recepción o perfil inactivo: 403. Respuestas sin caché.

| Método y ruta | Resultado |
| --- | --- |
| `GET /api/administracion/avisos` | Configuración actual con versión y hora UTC de actualización. |
| `PUT /api/administracion/avisos` | Reemplaza los tres campos con la versión vigente; devuelve la configuración actualizada. |

```json
{
  "notifyEmail": "avisos@ejemplo.test",
  "waOrderReady": "Hola [nombre], tu pedido [número] está listo.",
  "waReminder": "Hola [nombre], te esperamos a las [hora] en la línea [línea].",
  "version": "UUID obtenido en GET"
}
```

Los cuatro campos son obligatorios en PUT para evitar borrados accidentales en peticiones parciales. Correo válido de hasta 254 caracteres, o `null`/vacío para dejarlo sin configurar; se recortan sus espacios exteriores. Plantillas de hasta 4000 caracteres cada una; pueden vaciarse con `""`, pero no enviarse como `null`. El texto conserva saltos de línea y marcadores literalmente: F10 no sustituye variables, no interpreta HTML y no envía mensajes. No se almacenan contraseñas SMTP ni claves del proveedor en este recurso.

La respuesta incluye esos campos y `actualizadoEnUtc`. Cada guardado cambia `version`; una versión anterior produce 409. Ante conflicto, volver a leer y revisar los cambios antes de guardar. Datos inválidos/incompletos: 400. PUT exitoso: 200.

## Persistencia e integración

Tabla única `ConfiguracionAvisos`, con una fila fija y límites de longitud. Datos iniciales: correo `null` y plantillas vacías. Auditoría automática de cambios con actor; token de versión evita sobrescrituras. RLS habilitado y permisos de `anon`/`authenticated` revocados; acceder por la API.

Migración `20261123213918_ConfiguracionAvisos` aplicada en Supabase el 23 nov 2026. Reiniciar el backend para cargar las nuevas rutas. Frontend debe leer GET, editar los tres campos y enviar PUT con la versión leída; sustituir el mensaje «guardado en este navegador» por el resultado real. No migrar automáticamente los textos de demostración del navegador: deben revisarse y guardarse expresamente por un Administrador.

## Verificación

### F20 · Lectura operativa de WhatsApp · 27 nov 2026

GET `/api/avisos/whatsapp`, con JWT de **Recepción o Administrador activos**, devuelve únicamente:

```json
{
  "waOrderReady": "Hola [nombre], tu pedido [número] está listo.",
  "waReminder": "Tu actividad es a las [hora], línea [línea]."
}
```

Es un DTO separado que omite correo remitente, versión y fechas administrativas. Las plantillas pueden estar vacías; se devuelven como `""`, sin inventar textos. El resultado refleja lo que guardó Administración. No requiere parámetros y lleva `Cache-Control: no-store`. Sin sesión 401; Cliente/perfil inactivo/no autorizado 403.

El recurso es de solo lectura. GET/PUT `/api/administracion/avisos` **siguen exclusivos de Administrador**: Recepción recibe 403 en ambos, incluso con una versión válida. No se amplió la política de edición ni se publicaron las plantillas anónimamente. No hay migración ni cambios de frontend.

Thiago puede usar la ruta nueva para rellenar los mensajes de Pedidos con cualquier rol operativo. El editor de configuración debe continuar usando la ruta administrativa completa. El frontend conserva su manejo de marcadores y texto alternativo cuando la plantilla está vacía; leer no sustituye variables, no abre WhatsApp y no envía mensajes automáticamente. R07 conserva sus dependencias.

Seis casos HTTP nuevos cubren ambos roles, campos exactos, lectura de guardados, respuestas vacías, caché, permisos y ausencia de escrituras. La prueba PostgreSQL existente de configuración agrega la proyección limitada junto con persistencia, versión y auditoría. Cifras de cierre en ROADMAP.md; concurrencia omitida por indicación de Kevin.

### Verificación original de F10 · 23 nov 2026

Seis casos HTTP: permisos de lectura/escritura, persistencia entre sesiones, versión antigua, campos obligatorios, correo y longitud, y vaciado explícito. Una prueba PostgreSQL en esquema temporal comprueba migración, RLS, persistencia, auditoría y límite de columna.

Suite local: **296 aprobadas, cero fallidas y 41 PostgreSQL omitidas**. La prueba PostgreSQL específica se aprobó por separado. Concurrencia omitida por indicación del usuario. No se afirma envío real ni ejecución completa de PostgreSQL.
