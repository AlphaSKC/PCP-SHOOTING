# Backend de autenticación de PCP Shooting

La identidad y las contraseñas pertenecen a Supabase Auth. React envía el access_token de la sesión en la cabecera Authorization con el esquema Bearer. La API valida firma, emisor, audiencia authenticated y vencimiento, y obtiene el rol del perfil guardado en PostgreSQL.

El emisor configurado es https://itucrxtwphlecugsbauf.supabase.co/auth/v1. Su endpoint público /.well-known/jwks.json publica una clave EC/ES256, comprobada el 19 de noviembre de 2026. La publicación también puede incluir claves en espera. Para verificar la firma activa hace falta una sesión real o consultar el estado Current en JWT Signing Keys del panel. La guía oficial explica la diferencia entre claves Current y Standby. [JWT Signing Keys](https://supabase.com/docs/guides/auth/signing-keys)

## Contrato para React

1. Iniciar sesión con el SDK de Supabase y enviar el access_token a la API.
2. Consultar GET /api/perfiles/yo.
3. Si devuelve 404, crear el perfil con PUT /api/perfiles/yo y el cuerpo con nombre y telefono.
4. Leer rol y activo de la respuesta de la API.

Los roles salen como texto Cliente, Recepcion o Administrador. Se conservan los identificadores internos 0, 2 y 1 respectivamente. El mapeo del frontend existente reconoce estos nombres. El rol del perfil propio lo controla el servidor. Las actualizaciones públicas de nombre y teléfono conservan el rol y estado existentes.

Una respuesta 401 indica un token inválido o ausente. Una respuesta 403 indica que falta permiso o que el perfil está desactivado. El 404 del primer acceso indica que falta crear el perfil.

## Permisos de Recepción

Recepción puede consultar todos los pedidos en /api/administracion/pedidos y su detalle, crear pedidos para un cliente activo indicando usuarioId, registrar y confirmar cobros manuales de pedidos, marcar recogidas y cancelar pedidos pendientes.

Puede crear y editar alimentos, activar o desactivarlos, consultar y ajustar sus existencias y configurar mínimos. /api/administracion/productos y /api/inventario/alertas limitan los resultados a alimentos para ese rol. También se valida la categoría actual del registro al modificarlo.

Las funciones de asignación de roles, membresías, auditoría y consulta global de pagos requieren Administrador. El catálogo público sigue disponible para clientes.

Reservas y llegadas todavía carecen de endpoints backend. La autorización de esos módulos se integra cuando se implementen sus operaciones.

## Primer administrador

Registrar la cuenta desde React y confirmar el correo. Después ejecutar desde la raíz del repositorio, sustituyendo el correo del ejemplo.

```powershell
dotnet run --project .\PCP-Shooting\PCP-Shooting -- --environment Development --bootstrap-admin correo@dominio.com
```

El comando valida la cuenta en auth.users y crea o promueve su perfil. La cuenta debe estar confirmada y disponible. La asignación inicial se guarda con transacción y auditoría. Repetirla para el mismo administrador conserva el registro. Cuando ya existe otro administrador, las asignaciones siguientes se hacen desde PATCH /api/perfiles/{id}/acceso.

Todavía no se ha asignado un administrador real porque esa cuenta está pendiente de registro.

## Desarrollo y despliegue

El proxy Vite existente usa http://localhost:5026. En Development, la API permite ese flujo local sin redirigir la petición a otro origen. CORS permite localhost:5173 y 127.0.0.1:5173. Fuera de Development se mantiene la redirección HTTPS. Para un frontend desplegado en otro origen, configurar Cors__AllowedOrigins__0 con su URL.

La API obtiene las claves de validación desde JWKS. La clave publishable se configura en el SDK del frontend.

Las pruebas JWT usan una clave ES256 local y la validación Bearer real del backend. Las pruebas PostgreSQL usan esquemas temporales y cuentas de Auth simuladas para probar la inicialización del administrador. El inicio de sesión real con Supabase y la cuenta definitiva quedan pendientes.
