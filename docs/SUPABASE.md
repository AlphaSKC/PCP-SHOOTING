# Supabase · Configuración y seguridad

Supabase se usa **solo** para autenticación (y, más adelante, Storage para fotos). Los datos de negocio viven en tablas que la **API C#** lee y escribe; el navegador nunca consulta tablas directamente.

## Estado verificado (18 nov 2026)

| Revisión | Resultado |
|---|---|
| Firma de los tokens (JWKS) | ✅ ES256 asimétrica: la API valida tokens con las claves públicas |
| Acceso anónimo a tablas por la Data API | ✅ Bloqueado (`permission denied` en las 9 tablas) |
| Acceso de usuarios con sesión (`authenticated`) | ⚠️ Sin verificar: ejecutar [script de seguridad](../supabase/seguridad/01_rls_bloquear_acceso_publico.sql) |
| Confirmación de correo al registrarse | ✅ Activa |
| Proveedor Google | ⏸️ Desactivado (el botón está oculto en el sitio) |

## 1. Ejecutar el script de RLS (backend)

[script de seguridad](../supabase/seguridad/01_rls_bloquear_acceso_publico.sql) → Supabase → **SQL Editor**. Lee el encabezado antes: confirma que la API se conecta como `postgres`. Al final trae dos consultas de verificación que deben devolver 0 filas.

## 2. Panel de Supabase · lista de verificación

**Authentication → URL Configuration**
- Site URL: el dominio de producción (Vercel) cuando exista; mientras, `http://localhost:5173`.
- Redirect URLs: `http://localhost:5173/**` y `https://<dominio-vercel>/**`. Sin esto no funcionan los enlaces de confirmación y recuperación.

**Authentication → Sign In / Providers → Email**
- Confirm email: **activado**.
- Secure email change: **activado** (hay que confirmar el correo viejo y el nuevo).
- Secure password change: **activado**.

**Authentication → Policies (contraseñas)**
- Longitud mínima: **8**. Requisitos: **letras y números** (igual que valida el sitio).
- Leaked password protection (HaveIBeenPwned): activar si el plan lo permite.

**Authentication → Rate Limits**
- Dejar los valores por defecto o más estrictos. Opcional: activar CAPTCHA (hCaptcha/Turnstile) si hay registros abusivos.

**Authentication → Emails (SMTP)**
- ⚠️ El SMTP incluido en Supabase envía **muy pocos correos por hora** y es solo para pruebas. Antes de publicar, configurar un SMTP propio (Resend, SendGrid, Amazon SES…) con un remitente del dominio del club.
- Traducir las plantillas (confirmación, recuperación, cambio de correo) al español.

**Project Settings → Data API**
- La app no usa la Data API (PostgREST). Lo más seguro es **quitar `public` de "Exposed schemas"** o desactivar la Data API. El script de RLS cubre el caso aunque siga expuesta.

**Project Settings → JWT Keys**
- Mantener las JWT Signing Keys asimétricas (ya están). No volver al "JWT secret" antiguo.

## 3. Roles

- El rol vive en la tabla `PerfilesUsuario` de la API (no en Supabase). El registro público crea siempre **Cliente**.
- El **primer administrador** se asigna a mano en la base (cambiar `Rol` del usuario en `PerfilesUsuario`). Los demás, desde el panel por un administrador.
- Pendiente con backend: agregar el rol **Recepción** (reservas, llegadas, pedidos y alimentos).
- Nunca guardar el rol en `user_metadata`: el usuario puede editarlo.
