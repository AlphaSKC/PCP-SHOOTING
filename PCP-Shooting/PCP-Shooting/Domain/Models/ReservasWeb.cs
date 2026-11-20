using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public sealed class CrearReservaWeb
{
    public Guid IdOperacion { get; init; }
    public Guid ApartadoId { get; init; }
    [Required, MinLength(1), MaxLength(15)] public List<ParticipanteReservaDatos> Participantes { get; init; } = new();
}
public sealed class ParticipanteReservaDatos
{
    public Guid LineaId { get; init; }
    [Required, StringLength(150)] public required string Nombre { get; init; }
    [Required] public DateOnly? Nacimiento { get; init; }
    [StringLength(150)] public string? TutorNombre { get; init; }
    public DateOnly? TutorNacimiento { get; init; }
}
[JsonConverter(typeof(JsonStringEnumConverter<EstadoReservaWeb>))]
public enum EstadoReservaWeb { PendientePago = 0, Vencida = 1 }
public sealed record ParticipanteReservaConsulta(Guid LineaId, string Nombre, DateOnly Nacimiento, string? TutorNombre, DateOnly? TutorNacimiento);
public sealed record ConsumoReservaConsulta(Guid ProductoId, string Sku, string Nombre, int CantidadPorPersona, int CantidadTotal);
public sealed record ReservaWebResumen(Guid Id, Guid ApartadoId, string NombrePaquete, DateTime InicioUtc, DateTime VencePagoEnUtc, decimal Total, EstadoReservaWeb Estado);
public sealed record ReservaWebConsulta(Guid Id, Guid ApartadoId, Guid PaqueteId, string CodigoPaquete, string NombrePaquete,
    string? DescripcionPaquete, string IncluyeMarkdown, string? RequisitosMarkdown, DateOnly Fecha, TimeOnly Hora,
    string ZonaHoraria, DateTime InicioUtc, DateTime FinActividadUtc, DateTime FinOcupacionUtc, DateTime VencePagoEnUtc,
    DateTime CreadoEnUtc, decimal PrecioPorPersona, decimal? PrecioSocioInformativo, decimal Total, EstadoReservaWeb Estado,
    IReadOnlyList<ParticipanteReservaConsulta> Participantes, IReadOnlyList<ConsumoReservaConsulta> Consumibles);
