using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public class FiltroEventos : FiltroFechas
{
    [StringLength(150)] public string? Busqueda { get; init; }
    public bool? Proximos { get; init; }
}

public sealed class FiltroEventosGestion : FiltroEventos
{
    [EnumDataType(typeof(EstadoEvento))] public EstadoEvento? Estado { get; init; }
}

public sealed class GuardarEvento : IValidatableObject
{
    [Required, StringLength(160), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public required string Slug { get; init; }
    [Required, StringLength(200)] public required string Titulo { get; init; }
    [Required, StringLength(600)] public required string Resumen { get; init; }
    [Required, StringLength(30000)] public required string DescripcionMarkdown { get; init; }
    [StringLength(250)] public string? Ubicacion { get; init; }
    [StringLength(2048)] public string? ImagenUrl { get; init; }
    [JsonRequired] public DateTimeOffset InicioUtc { get; init; }
    [JsonRequired] public DateTimeOffset FinUtc { get; init; }
    [JsonRequired, EnumDataType(typeof(EstadoEvento))] public EstadoEvento Estado { get; init; }
    public Guid? Version { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FinUtc <= InicioUtc)
            yield return new ValidationResult("FinUtc debe ser posterior a InicioUtc.", [nameof(FinUtc)]);
        if (!string.IsNullOrWhiteSpace(ImagenUrl) && (!Uri.TryCreate(ImagenUrl.Trim(), UriKind.Absolute, out var url) ||
            url.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(url.UserInfo)))
            yield return new ValidationResult("La imagen requiere una URL HTTPS válida sin credenciales.", [nameof(ImagenUrl)]);
    }
}

public sealed record EventoResumen(Guid Id, string Slug, string Titulo, string Resumen, string? Ubicacion,
    string? ImagenUrl, DateTime InicioUtc, DateTime FinUtc);
public sealed record EventoDetalle(Guid Id, string Slug, string Titulo, string Resumen, string DescripcionMarkdown,
    string? Ubicacion, string? ImagenUrl, DateTime InicioUtc, DateTime FinUtc);
public sealed record EventoGestion(Guid Id, string Slug, string Titulo, string Resumen, string DescripcionMarkdown,
    string? Ubicacion, string? ImagenUrl, DateTime InicioUtc, DateTime FinUtc, EstadoEvento Estado,
    Guid Version, DateTime CreadoEnUtc, DateTime ActualizadoEnUtc);
