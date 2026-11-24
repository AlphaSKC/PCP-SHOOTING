using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public sealed class CrearSolicitudContacto : IValidatableObject
{
    [JsonRequired] public Guid IdOperacion { get; init; }
    [Required, StringLength(150, MinimumLength = 2)] public required string Nombre { get; init; }
    [Required, EmailAddress, StringLength(254)] public required string Email { get; init; }
    [StringLength(20), RegularExpression(@"^(\+?52)?[0-9]{10}$")] public string? Telefono { get; init; }
    [Required, RegularExpression("^(reservacion|grupos|tienda|cotizacion|otro)$")] public required string Motivo { get; init; }
    [Required, StringLength(2000, MinimumLength = 10)] public required string Mensaje { get; init; }
    [JsonRequired] public bool Privacidad { get; init; }
    [StringLength(200)] public string? SitioWeb { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (IdOperacion == Guid.Empty) yield return new("IdOperacion requiere un UUID distinto de cero.", [nameof(IdOperacion)]);
        if (!Privacidad) yield return new("Debes aceptar las políticas de privacidad.", [nameof(Privacidad)]);
        if (!string.IsNullOrWhiteSpace(SitioWeb)) yield return new("No se pudo aceptar la solicitud.", [nameof(SitioWeb)]);
        if ((Nombre?.Trim().Length ?? 0) < 2 || (Mensaje?.Trim().Length ?? 0) < 10) yield return new("Revisa el nombre y el mensaje.");
    }
}
public sealed class FiltroSolicitudesContacto : FiltroFechas
{
    [EnumDataType(typeof(EstadoSolicitudContacto))] public EstadoSolicitudContacto? Estado { get; init; }
    [StringLength(150)] public string? Busqueda { get; init; }
    [RegularExpression("^(reservacion|grupos|tienda|cotizacion|otro)$")] public string? Motivo { get; init; }
}
public sealed class ActualizarSolicitudContacto
{
    [JsonRequired, EnumDataType(typeof(EstadoSolicitudContacto))] public EstadoSolicitudContacto Estado { get; init; }
    [JsonRequired, StringLength(4000)] public string? NotasInternas { get; init; }
    [JsonRequired] public Guid Version { get; init; }
}
public sealed record ContactoRecibido(Guid Id, string Estado = "Recibida");
public sealed record ContactoResumen(Guid Id, string Nombre, string Email, string Motivo, EstadoSolicitudContacto Estado, DateTime CreadoEnUtc);
public sealed record ContactoDetalle(Guid Id, string Nombre, string Email, string? Telefono, string Motivo, string Mensaje,
    DateTime CreadoEnUtc, DateTime PrivacidadAceptadaEnUtc, EstadoSolicitudContacto Estado, string? NotasInternas, Guid Version, DateTime ActualizadoEnUtc);
