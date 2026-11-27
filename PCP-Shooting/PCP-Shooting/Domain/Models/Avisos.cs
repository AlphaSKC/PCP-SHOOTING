using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public sealed class GuardarAvisos : IValidatableObject
{
    [JsonRequired, StringLength(254)] public string? NotifyEmail { get; init; }
    [JsonRequired, Required(AllowEmptyStrings = true), StringLength(4000)] public string WaOrderReady { get; init; } = "";
    [JsonRequired, Required(AllowEmptyStrings = true), StringLength(4000)] public string WaReminder { get; init; } = "";
    [JsonRequired] public Guid Version { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Version == Guid.Empty) yield return new("La versión actual es obligatoria.", [nameof(Version)]);
        if (!string.IsNullOrWhiteSpace(NotifyEmail) && !new EmailAddressAttribute().IsValid(NotifyEmail.Trim()))
            yield return new("El correo de avisos no es válido.", [nameof(NotifyEmail)]);
    }
}

public sealed record AvisosConsulta(string? NotifyEmail, string WaOrderReady, string WaReminder, Guid Version, DateTime ActualizadoEnUtc);
public sealed record PlantillasWhatsAppConsulta(string WaOrderReady, string WaReminder);
