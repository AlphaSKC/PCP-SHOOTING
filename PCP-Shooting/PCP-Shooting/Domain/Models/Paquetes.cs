using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public class FiltroPaquetes : FiltroPagina
{
    [StringLength(150)] public string? Busqueda { get; init; }
}

public sealed class FiltroPaquetesGestion : FiltroPaquetes
{
    public bool? Activo { get; init; }
}

public sealed class ConsumiblePaqueteDatos
{
    public Guid ProductoId { get; init; }
    [Range(1, 100000)] public int CantidadPorPersona { get; init; }
}

public sealed class GuardarPaquete
{
    [Required, StringLength(80), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")]
    public required string Codigo { get; init; }
    [Required, StringLength(200)] public required string Nombre { get; init; }
    [StringLength(4000)] public string? Descripcion { get; init; }
    [Required, StringLength(10000)] public required string IncluyeMarkdown { get; init; }
    [StringLength(10000)] public string? RequisitosMarkdown { get; init; }
    public decimal? PrecioPorPersona { get; init; }
    public decimal? PrecioSocioCartaq { get; init; }
    public bool Activo { get; init; }
    public Guid? Version { get; init; }
    [Required, MaxLength(100)] public List<ConsumiblePaqueteDatos> Consumibles { get; init; } = new();
}

public sealed record PaquetePublico(Guid Id, string Codigo, string Nombre, string? Descripcion, string IncluyeMarkdown,
    string? RequisitosMarkdown, decimal? PrecioPorPersona, decimal? PrecioSocioCartaq);
public sealed record ConsumiblePaqueteConsulta(Guid ProductoId, string Sku, string Nombre, int CantidadPorPersona, bool Activo);
public sealed record PaqueteGestion(Guid Id, string Codigo, string Nombre, string? Descripcion, string IncluyeMarkdown,
    string? RequisitosMarkdown, decimal? PrecioPorPersona, decimal? PrecioSocioCartaq, bool Activo,
    bool VisibleEnCatalogo, DateTime CreadoEnUtc, DateTime ActualizadoEnUtc, Guid Version,
    IReadOnlyList<ConsumiblePaqueteConsulta> Consumibles);
