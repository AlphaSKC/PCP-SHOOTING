using System.Text.Json.Serialization;
using System.Security.Claims;

namespace PCP_Shooting.Domain.Interfaces;

[JsonConverter(typeof(JsonStringEnumConverter<RolUsuario>))]
public enum RolUsuario
{
    Cliente = 0,
    Administrador = 1,
    Recepcion = 2
}

public sealed record UsuarioActual(Guid Id, RolUsuario Rol);

public interface IUsuariosService
{
    UsuarioActual? ObtenerActual(ClaimsPrincipal principal);
}
