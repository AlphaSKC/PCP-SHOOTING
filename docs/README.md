# Documentación de PCP Shooting

Las guías del proyecto se encuentran en esta carpeta.

| Documento | Contenido |
| --- | --- |
| [Autenticación del backend](AUTH-BACKEND.md) | Supabase Auth, tokens JWT, perfiles, roles y primer administrador. |
| [Imágenes de productos](IMAGENES-API.md) | Subida de imágenes, permisos, Storage y reintentos. |
| [Reportes administrativos](REPORTES.md) | Endpoints, filtros, importes y exportación CSV. |
| [Configuración de Supabase](SUPABASE.md) | Lista de configuración del panel y seguridad. Conserva el estado de revisión fechado en el documento; para el contrato de roles y la inicialización del administrador, consultar la guía de autenticación. |
| [Contexto e instrucciones del proyecto](../CLAUDE.md) | Contexto funcional, frontend y acuerdos del equipo. Se conserva en la raíz para que las herramientas lo encuentren. |

## Archivos relacionados

- [Ejemplos de peticiones a la API](../PCP-Shooting/PCP-Shooting/PCP-Shooting.http).
- [Script de seguridad de Supabase](../supabase/seguridad/01_rls_bloquear_acceso_publico.sql).

Las nuevas guías Markdown deben agregarse a `docs/` y enlazarse en este índice. Las rutas de ejecución indicadas en las guías parten de la raíz del repositorio, salvo que se indique otra ubicación.
