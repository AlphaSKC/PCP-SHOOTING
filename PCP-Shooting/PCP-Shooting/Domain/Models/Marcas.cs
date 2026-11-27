using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class GuardarMarca : IValidatableObject
{
    [Required, StringLength(80)] public string Nombre { get; init; } = "";
    [StringLength(80), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string? Slug { get; init; }
    public bool? VisibleEnCarrusel { get; init; }
    [StringLength(60)] public string? Descripcion { get; init; }
    [StringLength(2048)] public string? Url { get; init; }
    public bool Activo { get; init; } = true;
    [Range(0, int.MaxValue)] public int Orden { get; init; }
    public Guid? Version { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrWhiteSpace(Url) && (!Uri.TryCreate(Url.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))) yield return new("El sitio de la marca debe ser HTTPS y sin credenciales.", [nameof(Url)]);
    }
}
public sealed record MarcaPublica(Guid Id, string Nombre, string? Descripcion, string? Url, string? LogoUrl, int Orden, string Slug = "");
public sealed record MarcaGestion(Guid Id, string Nombre, string? Descripcion, string? Url, string? LogoUrl, int Orden, bool Activo, Guid Version,
    string Slug = "", bool VisibleEnCarrusel = true);
public sealed record ImagenContenidoResultado(string Url, Guid Version);
public sealed record LogoMarcaResultado(string LogoUrl, Guid Version);
public sealed record PortadaBlogResultado(string ImagenUrl, Guid Version);
