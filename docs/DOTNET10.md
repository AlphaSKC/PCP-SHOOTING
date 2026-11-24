# Backend .NET 10

La API y las pruebas usan `net10.0`. Instalar un SDK estable .NET 10; `global.json` permite usar la banda estable 10.0 más reciente instalada, sin saltar a .NET 11. El frontend React/TypeScript mantiene sus herramientas actuales.

ASP.NET Core (JWT y pruebas) y EF Core usan 10.0.15; el proveedor PostgreSQL Npgsql usa 10.0.4. Se conservan Swagger y las herramientas de pruebas que superaron las comprobaciones. La herramienta EF se fija en el manifiesto del repositorio: no usar el ejecutable antiguo de `.tools` para este backend.

Desde la raíz del repositorio:

```powershell
dotnet --version
dotnet tool restore
dotnet restore PCP-Shooting/PCP-Shooting.Tests/PCP-Shooting.Tests.csproj
dotnet build PCP-Shooting/PCP-Shooting.Tests/PCP-Shooting.Tests.csproj --configuration StorageCheck
dotnet ef migrations has-pending-model-changes --project PCP-Shooting/PCP-Shooting --configuration StorageCheck
dotnet run --project PCP-Shooting/PCP-Shooting
```

`StorageCheck` permite verificar sin reemplazar los archivos Debug de una API que ya esté abierta. Reiniciar esa API para ejecutar la versión nueva. El identificador de User Secrets permanece igual; se conservan conexión PostgreSQL y configuración de autenticación.

La actualización no cambia el modelo y no necesita una migración nueva ni modificar las migraciones históricas. No se ejecutó `database update` como parte de esta actualización. Se comprobó que EF 10 no detecta cambios pendientes; las pruebas PostgreSQL se ejecutan sobre esquemas temporales.

## Azure App Service

Seleccionar .NET 10 como entorno de ejecución. Generar los archivos de publicación:

```powershell
dotnet publish PCP-Shooting/PCP-Shooting/PCP-Shooting.csproj --configuration Release --output artifacts/azure-api
```

Publicar el contenido de esa carpeta. En Linux, si se solicita comando de inicio, usar `dotnet PCP-Shooting.dll`. Esta publicación depende del runtime .NET/ASP.NET Core 10 del servidor. No se ha realizado un despliegue a Azure en este bloque.

Configurar los secretos y opciones en Azure; User Secrets locales no se publican. Revisar `ConnectionStrings__Postgres`, autenticación Supabase, orígenes CORS del frontend y, cuando estén disponibles, credenciales/URLs HTTPS de Mercado Pago según sus guías. No guardar secretos en los archivos publicados. Mantener deshabilitada la conciliación programada hasta completar su configuración.

## Evidencia de compatibilidad

- Compilación de API y pruebas sin errores ni advertencias.
- 324 pruebas locales aprobadas y 45 PostgreSQL omitidas en esa ejecución; concurrencia excluida por indicación del usuario.
- Cuatro pruebas PostgreSQL aprobadas por separado y de forma secuencial: conciliación programada, reservas y sus copias históricas, tallas/inventario y flujo HTTP administrativo con auditoría.
- Publicación Release correcta; arranque local de los archivos publicados y Swagger HTTP 200 con 70 rutas.
- EF sin cambios pendientes de modelo y sin paquetes vulnerables detectados en la API por los orígenes de NuGet consultados.

La migración se revisó siguiendo los [cambios de ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview?view=aspnetcore-10.0) y las [notas del proveedor PostgreSQL 10](https://www.npgsql.org/efcore/release-notes/10.0.html). Las pruebas simuladas de pagos no sustituyen las compras sandbox ni la prueba de login real después del despliegue.
