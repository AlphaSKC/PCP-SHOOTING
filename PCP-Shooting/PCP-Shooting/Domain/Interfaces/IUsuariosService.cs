using System.Security.Claims;

namespace PCP_Shooting.Domain.Interfaces;

public enum RolUsuario
{
    Cliente,
    Administrador
}

public sealed record UsuarioActual(Guid Id, RolUsuario Rol);

public interface IUsuariosService
{
    UsuarioActual? ObtenerActual(ClaimsPrincipal principal);
}
