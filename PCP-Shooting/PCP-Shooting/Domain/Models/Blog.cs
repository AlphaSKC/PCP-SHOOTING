using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public class FiltroBlog : FiltroPagina
{
    [StringLength(150)] public string? Busqueda { get; init; }
    [EnumDataType(typeof(CategoriaBlog))] public CategoriaBlog? Categoria { get; init; }
    public bool? Destacado { get; init; }
}

public sealed class FiltroBlogGestion : FiltroBlog
{
    [EnumDataType(typeof(EstadoArticuloBlog))] public EstadoArticuloBlog? Estado { get; init; }
}

public sealed class GuardarArticuloBlog
{
    [Required, StringLength(160), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public required string Slug { get; init; }
    [Required, StringLength(200)] public required string Titulo { get; init; }
    [Required, StringLength(600)] public required string Resumen { get; init; }
    [Required, StringLength(100000)] public required string ContenidoMarkdown { get; init; }
    [EnumDataType(typeof(CategoriaBlog))] public CategoriaBlog Categoria { get; init; }
    [StringLength(2048)] public string? ImagenUrl { get; init; }
    public bool Destacado { get; init; }
    [EnumDataType(typeof(EstadoArticuloBlog))] public EstadoArticuloBlog Estado { get; init; }
    public DateTimeOffset? PublicarEnUtc { get; init; }
    public Guid? Version { get; init; }
}

public sealed record ArticuloBlogResumen(Guid Id, string Slug, string Titulo, string Resumen,
    CategoriaBlog Categoria, string? ImagenUrl, bool Destacado, DateTime? PublicarEnUtc);
public sealed record ArticuloBlogDetalle(Guid Id, string Slug, string Titulo, string Resumen,
    string ContenidoMarkdown, CategoriaBlog Categoria, string? ImagenUrl, bool Destacado, DateTime? PublicarEnUtc);
public sealed record ArticuloBlogGestion(Guid Id, string Slug, string Titulo, string Resumen,
    string ContenidoMarkdown, CategoriaBlog Categoria, string? ImagenUrl, bool Destacado,
    EstadoArticuloBlog Estado, DateTime? PublicarEnUtc, DateTime CreadoEnUtc, DateTime ActualizadoEnUtc,
    Guid AutorId, Guid Version);
