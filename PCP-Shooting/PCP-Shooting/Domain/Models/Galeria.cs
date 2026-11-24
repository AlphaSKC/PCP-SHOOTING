using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class GuardarAlbumGaleria
{
    [Required, StringLength(120)] public required string Nombre { get; init; }
    [StringLength(500)] public string? Descripcion { get; init; }
    [Range(0, int.MaxValue)] public int Orden { get; init; }
    public bool Publicado { get; init; }
    public Guid? Version { get; init; }
}
public sealed class GuardarFotoGaleria
{
    public Guid? IdOperacion { get; init; }
    [Required, StringLength(150)] public required string Titulo { get; init; }
    [Required, StringLength(250)] public required string TextoAlternativo { get; init; }
    [StringLength(1000)] public string? Descripcion { get; init; }
    [Range(0, int.MaxValue)] public int Orden { get; init; }
    public bool Publicada { get; init; }
    public Guid? Version { get; init; }
}
public sealed class FiltroFotosGaleria : FiltroPagina { public Guid? AlbumId { get; init; } }
public sealed record AlbumGaleriaPublico(Guid Id, string Nombre, string? Descripcion, int Orden, string? PortadaUrl, int CantidadFotos);
public sealed record AlbumGaleriaGestion(Guid Id, string Nombre, string? Descripcion, int Orden, bool Publicado, Guid Version);
public sealed record FotoGaleriaPublica(Guid Id, Guid AlbumId, string Titulo, string TextoAlternativo, string? Descripcion, int Orden, string ImagenUrl);
public sealed record FotoGaleriaGestion(Guid Id, Guid AlbumId, string Titulo, string TextoAlternativo, string? Descripcion, int Orden, bool Publicada, string? ImagenUrl, Guid Version);
