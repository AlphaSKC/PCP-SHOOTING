using PCP_Shooting.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Interfaces;

public sealed record EditarPerfil(
    [Required, StringLength(150, MinimumLength = 1)] string Nombre,
    [StringLength(30), Phone] string? Telefono);

public sealed record CambiarAcceso([EnumDataType(typeof(RolUsuario))] RolUsuario Rol, bool Activo);

public sealed record PerfilConsulta(Guid Id, string Nombre, string? Telefono, RolUsuario Rol, bool Activo);

public interface IPerfilesService
{
    Task<PaginaPerfiles> ListarAsync(FiltroPerfiles filtro, CancellationToken cancellationToken);
    Task<PerfilConsulta?> ConsultarAsync(Guid id, CancellationToken cancellationToken);
    Task<PerfilConsulta> GuardarPropioAsync(Guid id, EditarPerfil datos, CancellationToken cancellationToken);
    Task<PerfilConsulta?> CambiarAccesoAsync(Guid id, CambiarAcceso datos, CancellationToken cancellationToken);
}