using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Domain.Models;

public sealed class FiltroPerfiles
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;

    [StringLength(150)]
    public string? Busqueda { get; init; }

    [EnumDataType(typeof(RolUsuario))]
    public RolUsuario? Rol { get; init; }

    public bool? Activo { get; init; }
}

public sealed record PaginaPerfiles(
    IReadOnlyList<PerfilConsulta> Elementos, int Total, int Pagina, int TamanoPagina);