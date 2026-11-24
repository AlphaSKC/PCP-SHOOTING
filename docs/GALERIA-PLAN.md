# Galería · plan de implementación

> Para ejecución: usar executing-plans si se implementa en esta sesión o subagent-driven-development si Kevin elige delegación. Completar las tareas y verificaciones en orden.

**Objetivo:** gestionar álbumes y fotografías reales y conectarlos a las pantallas existentes.

**Arquitectura:** modelos EF separados para álbumes, fotos y cargas. Servicio de galería para consultas/metadatos; servicio de carga que reutiliza validador, transacciones y transporte Storage existentes. Frontend usa la API sin acceso directo a tablas.

**Tecnologías:** ASP.NET Core/EF Core 10, PostgreSQL Supabase, Storage, React/TypeScript, componentes existentes.

**Diseño:** [GALERIA-DISENO.md](GALERIA-DISENO.md), aceptado por Kevin el 24 nov 2026.

## Restricciones comunes

- JPG/PNG/WebP hasta 5 MiB; sin videos ni SVG.
- Bucket público `pcp-galeria`; ocultar retira del listado, sin revocar URLs anteriores.
- Administración requiere perfil Administrador activo; no habilitar Recepción.
- Álbumes/fotos nuevos ocultos, sin datos ficticios iniciales; no publicar sin imagen.
- Orden no negativo, paginación de hasta 100 y control de versión UUID.
- Conservar fotos ocultas y archivos anteriores, sin borrado físico ni traslado entre álbumes.
- Mantener la omisión de pruebas de concurrencia solicitada por Kevin.
- Documentación en `docs/`; entregar mensaje FT en inglés, sin publicar el sitio ni confirmar integración real sin comprobarla.

## Riesgos que deben tener comprobación

1. Álbum con todas las fotos ocultas/incompletas: no aparece en público ni conserva una portada oculta.
2. IdOperacion reutilizado con otro archivo, foto o actor: 409, sin reasignar el archivo.
3. Reintento después de cambiar/ocultar/sustituir la foto: no restaurar una versión anterior.
4. Fallo de carga entre ficha y Storage: ficha oculta recuperable y resultados individuales en carga múltiple.
5. Página/filtro/álbum manipulado: no filtrar fotos ocultas por rutas públicas ni cargar en otro destino.

## Tarea 1 · Álbumes, fotos y publicación

**Archivos nuevos:** `PCP-Shooting/PCP-Shooting/Persistence/Models/Galeria.cs`, configuración en `Persistence/Configurations/GaleriaConfiguration.cs`, `Domain/Models/Galeria.cs`, `Domain/Interfaces/IGaleriaService.cs`, `Domain/Services/GaleriaService.cs`, `Controllers/GaleriaController.cs` y `Controllers/GaleriaAdministracionController.cs`. Usar el prefijo `PCP-Shooting/PCP-Shooting/` para las rutas abreviadas anteriores.

**Modificar:** `Persistence/PcpShootingDbContext.cs` y `Program.cs` para modelos/servicio; añadir migración en `Persistence/Migrations`.

**Interfaz producida:** `IGaleriaService`: consultas públicas de álbumes/fotos paginadas, consultas administrativas y guardar álbum/foto con DTO de versión. Respuestas públicas separadas de gestión; la foto pública incluye ID, álbum, URL, título, texto alternativo, descripción y orden. DTO y longitudes según diseño.

- [ ] Escribir `GaleriaApiTests.cs`: público vacío al inicio, permisos 401/403, alta oculta, validaciones, edición con versión/409 y publicación sin imagen/400.
- [ ] Ejecutar filtro `FullyQualifiedName~GaleriaApiTests` en StorageCheck y observar fallo por contratos ausentes.
- [ ] Implementar modelos, configuración, servicio y rutas definidas en el diseño. Consultar portada/cantidad solo sobre fotos publicadas disponibles; paginar y ordenar en SQL, sin cargar todos los archivos en memoria.
- [ ] Añadir casos de álbum oculto, foto oculta, portada retirada, orden empatado por UUID, álbum inexistente y página fuera de rango. Probar contra PostgreSQL con `GaleriaPostgresTests.cs` en esquema temporal.
- [ ] Generar migración con `dotnet ef migrations add GaleriaInicial --project PCP-Shooting/PCP-Shooting --configuration StorageCheck`; verificar restricciones e índices, sin valores de ejemplo. Su provisión Storage se completa en tarea 2 antes de aplicar a Supabase.
- [ ] Ejecutar pruebas relevantes y comprobar resultados. Mensaje propuesto: `FT: Add gallery albums and photo management`.

## Tarea 2 · Carga idempotente y Storage

**Archivos nuevos:** `Domain/Interfaces/IGaleriaImagenesService.cs`, `Domain/Services/GaleriaImagenesService.cs`. Modelo `CargaFotoGaleria` en `Persistence/Models/Galeria.cs`. **Modificar:** controlador administrativo, `Persistence/Services/SupabaseImagenStorage.cs`, configuración/modelo y migración de tarea 1.

**Interfaz producida:** `SubirAsync(Guid actorId, Guid fotoId, Guid idOperacion, Stream archivo, string nombre, string tipoContenido, string token, CancellationToken ct)` devuelve URL y versión de foto. Consume `IImagenStorage`, `ValidadorImagen` e `ITransaccionesPersistence` existentes.

- [ ] Escribir `GaleriaImagenesTests.cs` con Storage simulado: archivo inválido, tamaño, reintento igual, reutilización incompatible, destino ajeno, cambio de versión y fallo recuperable.
- [ ] Ejecutar las pruebas nuevas y verificar fallos antes de implementar.
- [ ] Reservar carga en transacción con actor/foto/hash/versión; subir fuera de la transacción; consolidar URL/versión sin publicar automáticamente. Validar pertenencia y estado en cada fase. Reintento completado no revierte una sustitución posterior.
- [ ] Extender resolución de bucket del transporte solo para prefijo `galeria/`; conservar marcas/blog/productos. Generar ruta `galeria/{fotoId:N}/{idOperacion:N}{extension}`.
- [ ] Completar migración: RLS en tablas, bucket público 5 MiB con MIME limitado y políticas de inserción ligadas a Administrador activo y reserva de carga propia. Reutilizar patrón de migración de marcas/blog sin permitir sobrescritura/borrado del cliente.
- [ ] Probar migración y políticas PostgreSQL/Storage; distinguir transporte simulado de carga real con JWT. Ejecutar `has-pending-model-changes`. Aplicar migración en Supabase solo después de verificaciones satisfactorias y registrar resultado real.
- [ ] Ejecutar comprobaciones de regresión de imágenes anteriores. Mensaje propuesto: `FT: Add validated gallery photo uploads`.

## Tarea 3 · Conexión pública y administración

**Crear:** `PCP-ShootingWeb/src/features/galeria/api.ts`, tipos y componentes administrativos propios en esa carpeta. **Modificar:** `src/pages/public/galeria/GaleriaPage.tsx`, `Lightbox.tsx` solo si sus tipos requieren adaptación, y la pestaña Galería de `src/pages/admin/AdminContenidoPage.tsx`/`AdminContenidoApi.tsx` según el recorrido API actual. Reutilizar componentes de formulario/carga/alertas, estilos y cliente `src/lib/api.ts`.

**Contrato consumido:** rutas/DTO de tareas 1 y 2, JWT Supabase del administrador y UUID estable de carga por archivo.

- [ ] Conectar consultas públicas con álbumes dinámicos, paginación y estado vacío/error; descartar respuestas antiguas al cambiar filtro. Conservar visor y navegación accesible.
- [ ] Implementar editor de álbumes y fichas de fotos con orden, título, texto alternativo, descripción, publicado y versión. Publicar solo tras imagen disponible. Ante 409, recargar y permitir al usuario revisar; no sobrescribir silenciosamente cambios.
- [ ] Carga múltiple secuencial por archivo con progreso/resultado individual; conservar UUID en reintentos y fichas fallidas ocultas. Manejar sesión vencida sin perder metadatos escritos.
- [ ] Ejecutar `npm run build`; comprobar UI vacía, edición, error de carga, publicación/ocultación y visor con API local. Usar contenido de prueba controlado, sin afirmar que son fotografías finales del cliente.
- [ ] Verificar una carga con sesión real si hay acceso disponible; si falta, dejarla explícitamente pendiente. No solicitar contraseñas ni inferir acceso a partir de pruebas simuladas.
- [ ] Actualizar `docs/GALERIA.md`, índice y roadmap con endpoints, migración, evidencias y pendientes de despliegue. Mensaje final propuesto: `FT: Add gallery management and public photo browsing`.

## Cierre global

- [ ] Pruebas locales sin concurrencia, PostgreSQL específicas en secuencia, compilación frontend y revisión final.
- [ ] Registrar qué se implementó, qué se aplicó en Supabase y qué se comprobó con JWT real.
- [ ] Mantener los temas OQ ajenos a galería sin cambios.

## Ejecución propuesta

Implementación en esta misma sesión por el agente principal y revisión independiente al final: las tareas comparten modelos, migración y contratos; mantenerlas juntas evita divergencias. Alternativa: delegar tareas con revisión por entrega si Kevin lo prefiere. Plan pendiente de revisión/elección antes de implementar.
