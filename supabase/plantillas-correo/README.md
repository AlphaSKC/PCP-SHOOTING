# Plantillas de correo de Supabase Auth

Correos con la imagen de PCP Shooting (encabezado negro, botón rojo, Montserrat con respaldo Arial). Se pegan a mano en el panel; este repo guarda la versión vigente.

| Archivo | Plantilla en Supabase | Asunto |
| --- | --- | --- |
| `confirmar-cuenta.html` | Confirm signup | Confirma tu cuenta · PCP Shooting |
| `recuperar-contrasena.html` | Reset Password | Restablece tu contraseña · PCP Shooting |
| `cambiar-correo.html` | Change Email Address | Confirma tu nuevo correo · PCP Shooting |

## Cómo aplicarlas

1. Sustituir `URL_DEL_LOGO` por una URL **pública** del logo blanco (`PCP-ShootingWeb/src/assets/logo-pcp-blanco.png`). Opciones:
   - Mientras no esté publicado el sitio: Supabase → Storage → crear bucket **público** `marca` → subir el PNG → copiar su URL pública.
   - Ya publicado en Vercel: `https://<dominio>/logo-pcp-blanco.png` (copiar el PNG a `PCP-ShootingWeb/public/`).
   - Si el logo no carga (algunos correos bloquean imágenes), se muestra el texto "PCP SHOOTING".
2. Supabase → **Authentication → Emails → Templates** → elegir la plantilla → cambiar el **Subject** y pegar el HTML en **Message body** → **Save**.
3. Probar registrando una cuenta con un correo propio (Gmail y Outlook se ven distinto).

## Reglas

- No quitar `{{ .ConfirmationURL }}`: es el enlace que confirma. Las redirecciones (`/login?confirmado=1`, `/restablecer`) las define el frontend.
- Solo estilos en línea y tablas: los clientes de correo ignoran CSS externo y `<style>` en muchos casos.
- Si el panel pide SMTP propio para editar o enviar plantillas, ver la sección SMTP de [docs/SUPABASE.md](../../docs/SUPABASE.md).
