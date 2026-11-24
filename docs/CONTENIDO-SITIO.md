# Contenido institucional editable

Implementado el 24 nov 2026. Migración `20261124173746_ContenidoInstitucional` aplicada en Supabase. API y frontend conectados en código; pendiente desplegar y comprobar edición/publicación con una sesión real de Administrador.

## Alcance

Administración → Contenido → Textos permite editar:

- Título/texto principal de Nosotros y bloque informativo de seguridad.
- Testimonio y autor opcionales: ambos completos o ambos vacíos. No se inicializa un testimonio de ejemplo.
- Descripción del pie del sitio.
- Dirección, horario informativo, teléfono mostrado, número internacional de llamada, número WhatsApp y correo público.
- Consulta de ubicación de Google Maps y enlaces opcionales de Instagram, Facebook, TikTok y YouTube.

Guardar publica directamente; no hay borradores/programación ni carga de archivos en este módulo. Los textos son planos y React los escapa; no se interpreta HTML ni se añade un editor de páginas arbitrarias. No cambia reservas, pagos, cartas responsivas, avisos ni formularios de contacto.

**Horario informativo:** solo texto mostrado al visitante. No cambia horarios, bloqueos, turnos, anticipación ni disponibilidad de reservas. Esas reglas se administran en Configuración operativa. El horario estructurado de SEO conserva su configuración independiente, no se interpreta desde este texto libre.

## API

| Método/ruta | Acceso | Resultado |
| --- | --- | --- |
| GET `/api/contenido-sitio` | Público | Objeto de contenido, sin versión ni auditoría. |
| GET `/api/administracion/contenido-sitio` | Administrador activo | `{contenido, version, actualizadoEnUtc}`. |
| PUT `/api/administracion/contenido-sitio` | Administrador activo | Mismo objeto con contenido completo y versión actual; devuelve nueva versión. |

Campos de `contenido`: `nosotrosTitulo`, `nosotrosTexto`, `seguridadTitulo`, `seguridadTexto`, `testimonio`, `autorTestimonio`, `descripcionPie`, `direccion`, `horarioPublico`, `telefono`, `telefonoMarcacion`, `whatsAppNumero`, `correo`, `ubicacionMapa`, `instagramUrl`, `facebookUrl`, `tikTokUrl`, `youTubeUrl`.

PUT reemplaza el documento completo: todos los campos deben enviarse, incluidos los opcionales con null si se retiran. No admite actualizaciones parciales que restablezcan valores iniciales. La versión UUID es obligatoria; contenido obsoleto devuelve 409, sin sobrescribir el actual. El editor conserva el borrador ante ese error y ofrece recargar explícitamente. Cliente/Recepción reciben 403; sin sesión 401.

Límites: título Nosotros 300, textos Nosotros/seguridad 2000, título seguridad 150, testimonio 1000, autor 150, pie 500, dirección/horario/consulta de mapa 300, teléfono mostrado 40, correo 254 y enlaces 2048. Teléfono de llamada con `+` y código de país; WhatsApp de 7–15 dígitos sin `+`, espacios o guiones. Redes con URL HTTPS absoluta sin credenciales; null retira el enlace. El mapa y enlaces tel/WhatsApp se generan desde datos tipados, sin aceptar código iframe ni esquemas arbitrarios.

## Persistencia y publicación

Una configuración única con documento JSONB tipado, versión y fecha UTC, auditoría y RLS. Las tablas no quedan expuestas a anon/authenticated. Valores iniciales de contacto/textos provienen de los ya presentes en el sitio; no son precios ni reglas operativas nuevos. No modificar el seed en futuras migraciones para sustituir contenido que Administración haya editado: cualquier ampliación debe conservar sus valores actuales.

Una fuente compartida actualiza contacto/pie/menú lateral, Nosotros, Contacto, datos de recogida de tienda y enlaces de soporte existentes. Inicio utiliza los datos actuales de contacto en su JSON-LD; Contacto actualiza su descripción y Nosotros su texto descriptivo. Las páginas demo y enlaces legales no editables conservan sus fuentes anteriores. Actividades, estadísticas, metadatos globales/prerender y horario SEO estructurado no se convierten en otro CMS en este bloque.

Tras guardar, la pestaña actual recibe los nuevos datos. Otras pestañas/dispositivos deben recargar; no hay difusión en tiempo real. Si la consulta pública falla o la API todavía no está desplegada, se conserva el contenido público previamente conocido del sitio; no se reinicia la configuración del servidor ni se muestran testimonios ficticios en modo API. El editor muestra el error de carga y permite reintentar.

El correo público no cambia remitentes, destinatarios de avisos ni Supabase Auth. Este bloque no envía mensajes.

## Verificación

367 pruebas locales aprobadas, 51 PostgreSQL omitidas en esa ejecución y 5 comprobaciones PostgreSQL aprobadas por separado: contenido institucional, galería (3) e historial del cliente. Incluye persistencia JSONB, RLS, auditoría, versión/409 y conservación de configuración operativa. Se corrigió la ejecución de scripts generados en el fixture PostgreSQL para soportar llaves JSON literales mediante ADO, sin formato compuesto.

Frontend TypeScript/Vite/prerender compila. Revisión independiente realizada; corregida la publicación de contactos/redes anteriores en JSON-LD. Sin pruebas de concurrencia por indicación del usuario. La edición en navegador como Administrador y el recorrido visual completo permanecen pendientes; no se publicó a Azure/Vercel desde esta sesión.
