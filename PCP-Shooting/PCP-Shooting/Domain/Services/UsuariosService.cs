using System.Security.Claims;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class UsuariosService : IUsuariosService
{
    public UsuarioActual? ObtenerActual(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identificador = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(identificador, out var id) || id == Guid.Empty)
        {
            return null;
        }

        var rol = principal.IsInRole(nameof(RolUsuario.Administrador))
            ? RolUsuario.Administrador
            : principal.IsInRole(nameof(RolUsuario.Recepcion)) ? RolUsuario.Recepcion : RolUsuario.Cliente;

        return new UsuarioActual(id, rol);
    }
}
