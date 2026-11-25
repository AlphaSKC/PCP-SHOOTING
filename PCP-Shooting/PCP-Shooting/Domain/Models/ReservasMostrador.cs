using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class CrearReservaMostrador
{
    public Guid IdOperacion { get; init; }
    public Guid ClienteId { get; init; }
    public Guid PaqueteId { get; init; }
    [Required] public DateOnly? Fecha { get; init; }
    [Required] public TimeOnly? Hora { get; init; }
    [Required, MinLength(1), MaxLength(15)] public List<ParticipanteReservaDatos> Participantes { get; init; } = new();
}

public sealed record ReservaAdministrativaResumen(Guid Id, Guid ClienteId, string NombreCliente, string? TelefonoCliente,
    string NombrePaquete, DateOnly Fecha, TimeOnly Hora, DateTime InicioUtc, decimal Total, EstadoReservaWeb Estado,
    int CantidadParticipantes);

public sealed record ReservaAdministrativaConsulta(Guid Id, Guid ClienteId, string NombreCliente, string? TelefonoCliente,
    Guid PaqueteId, string CodigoPaquete, string NombrePaquete, DateOnly Fecha, TimeOnly Hora, string ZonaHoraria,
    DateTime InicioUtc, DateTime FinActividadUtc, DateTime FinOcupacionUtc, DateTime VencePagoEnUtc, DateTime CreadoEnUtc,
    decimal PrecioPorPersona, decimal Total, EstadoReservaWeb Estado, IReadOnlyList<ParticipanteAdministrativaConsulta> Participantes);

public sealed record ParticipanteAdministrativaConsulta(Guid LineaId, int NumeroLinea, string Nombre, DateOnly Nacimiento,
    string? TutorNombre, DateOnly? TutorNacimiento);

public sealed class FiltroReservasAdministrativas : FiltroPagina
{
    public DateOnly? Fecha { get; init; }
    [EnumDataType(typeof(EstadoReservaWeb))] public EstadoReservaWeb? Estado { get; init; }
}

public sealed class FiltroClientesMostrador : FiltroPagina, IValidatableObject
{
    [Required, StringLength(150, MinimumLength = 2)] public string Busqueda { get; init; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Busqueda) || Busqueda.Trim().Length < 2)
            yield return new ValidationResult("Indica al menos dos caracteres de nombre o teléfono.", [nameof(Busqueda)]);
    }
}

public sealed record ClienteMostradorConsulta(Guid Id, string Nombre, string? Telefono);
