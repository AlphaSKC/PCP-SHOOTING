using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter<EstadoAvisoPublico>))]
public enum EstadoAvisoPublico { Inactivo, Programado, Vigente, Vencido }

public sealed class FiltroAvisosPublicos : FiltroPagina
{
    [EnumDataType(typeof(EstadoAvisoPublico))] public EstadoAvisoPublico? Estado { get; init; }
    [StringLength(120)] public string? Busqueda { get; init; }
}

public sealed class GuardarAvisoPublico : IValidatableObject
{
    [StringLength(120)] public string? Titulo { get; init; }
    [JsonRequired, Required, StringLength(1000)] public string Texto { get; init; } = "";
    [JsonRequired] public bool Activo { get; init; }
    [JsonRequired, Range(0, int.MaxValue)] public int Orden { get; init; }
    [JsonRequired] public DateTimeOffset? InicioUtc { get; init; }
    [JsonRequired] public DateTimeOffset? FinUtc { get; init; }
    public Guid? Version { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InicioUtc.HasValue && FinUtc.HasValue && FinUtc <= InicioUtc)
            yield return new ValidationResult("FinUtc debe ser posterior a InicioUtc.", [nameof(FinUtc)]);
    }
}

public sealed record AvisoPublicoDto(Guid Id, string? Titulo, string Texto, int Orden, DateTime? InicioUtc, DateTime? FinUtc);
public sealed record AvisoPublicoGestion(Guid Id, string? Titulo, string Texto, bool Activo, int Orden,
    DateTime? InicioUtc, DateTime? FinUtc, EstadoAvisoPublico Estado, Guid Version, DateTime CreadoEnUtc, DateTime ActualizadoEnUtc);
