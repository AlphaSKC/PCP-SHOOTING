using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class GuardarCategoriaTienda
{
    [Required, StringLength(60)] public string Nombre { get; init; } = "";
    [Required, StringLength(60), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string Slug { get; init; } = "";
    [StringLength(300)] public string? Descripcion { get; init; }
    [Range(0, int.MaxValue)] public int Orden { get; init; }
    public bool Activa { get; init; }
    public Guid? Version { get; init; }
}
public sealed record CategoriaTiendaPublica(Guid Id, string Nombre, string Slug, string? Descripcion, string? ImagenUrl, int Orden, int CantidadProductos);
public sealed record CategoriaTiendaGestion(Guid Id, string Nombre, string Slug, string? Descripcion, string? ImagenUrl, int Orden,
    bool Activa, Guid Version, int CantidadProductos);
public sealed record CategoriaTiendaReferencia(Guid Id, string Nombre, string Slug);
public sealed record MarcaTiendaReferencia(Guid Id, string Nombre, string Slug, string? LogoUrl, int Orden);
public sealed record ImagenCategoriaTiendaResultado(string ImagenUrl, Guid Version);
