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

## Frontend local y Vercel contra Azure

En `PCP-ShootingWeb/.env.local`, usar `VITE_DATA_SOURCE=api` y `VITE_API_URL=https://pcpshootingapi-b6hdcuembxbnbxa7.mexicocentral-01.azurewebsites.net`. Conservar `VITE_SUPABASE_URL` y `VITE_SUPABASE_PUBLISHABLE_KEY` existentes. Reiniciar Vite después de cambiar variables; no es necesario ejecutar la API local. El login se realiza con Supabase y la API de Azure recibe el JWT para consultar perfiles/datos.

El archivo `.env.local` es configuración local ignorada por Git; Vercel necesita esas cuatro variables configuradas en su proyecto y una nueva compilación. Usar raíz `PCP-ShootingWeb`, comando de compilación `npm run build` y salida `dist`; se conserva el `vercel.json` existente. Configurar `VITE_SITE_URL` con la URL pública definitiva para generar SEO correcto.

Cuando se conozca la URL de Vercel, agregar su origen exacto en Azure como otro elemento `Cors__AllowedOrigins__1` (conservar localhost en el índice 0 para pruebas). Revisar Site URL y Redirect URLs en Supabase Auth para confirmación y recuperación de contraseña. La clave publishable es pública; no configurar contraseñas PostgreSQL, service_role ni secretos de Mercado Pago en variables VITE. La compilación no demuestra que se haya probado una sesión real en Azure.

## Registros en Azure App Service

La API registra `Microsoft.Extensions.Logging.AzureAppServices` mediante `AddAzureWebAppDiagnostics` para enviar sus mensajes al sistema de diagnóstico de App Service. En Azure, activar Registro de aplicaciones (sistema de archivos), nivel Error y guardar; abrir Secuencia de registro y reproducir la petición mientras está conectado el visor. Esta integración sigue la [configuración oficial de ASP.NET Core en App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore).

Publicar este cambio antes de comprobar los mensajes. Activar el visor por sí solo no demuestra que se estén capturando las excepciones. Mantener Production y no habilitar páginas públicas de errores detallados. Los errores 500 de consultas de datos siguen pendientes de diagnóstico hasta obtener la excepción real; no se consideran resueltos por agregar el registro.

## Evidencia de compatibilidad

- Compilación de API y pruebas sin errores ni advertencias.
- 324 pruebas locales aprobadas y 45 PostgreSQL omitidas en esa ejecución; concurrencia excluida por indicación del usuario.
- Cuatro pruebas PostgreSQL aprobadas por separado y de forma secuencial: conciliación programada, reservas y sus copias históricas, tallas/inventario y flujo HTTP administrativo con auditoría.
- Publicación Release correcta; arranque local de los archivos publicados y Swagger HTTP 200 con 70 rutas.
- EF sin cambios pendientes de modelo y sin paquetes vulnerables detectados en la API por los orígenes de NuGet consultados.

La migración se revisó siguiendo los [cambios de ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview?view=aspnetcore-10.0) y las [notas del proveedor PostgreSQL 10](https://www.npgsql.org/efcore/release-notes/10.0.html). Las pruebas simuladas de pagos no sustituyen las compras sandbox ni la prueba de login real después del despliegue.
