namespace PCP_Shooting.Persistence.Models;

public sealed class ReservaWeb
{
    public Guid Id { get; set; }
    public Guid ApartadoId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CreadoPorId { get; set; }
    public Guid PaqueteId { get; set; }
    public Guid PaqueteVersion { get; set; }
    public required string CodigoPaquete { get; set; }
    public required string NombrePaquete { get; set; }
    public string? DescripcionPaquete { get; set; }
    public required string IncluyeMarkdown { get; set; }
    public string? RequisitosMarkdown { get; set; }
    public required string SolicitudHash { get; set; }
    public decimal PrecioPorPersona { get; set; }
    public decimal? PrecioSocioInformativo { get; set; }
    public decimal Total { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public required string ZonaHoraria { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinActividadUtc { get; set; }
    public DateTime FinOcupacionUtc { get; set; }
    public DateTime VencePagoEnUtc { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public ICollection<ParticipanteReserva> Participantes { get; } = new List<ParticipanteReserva>();
    public ICollection<ConsumoReserva> Consumibles { get; } = new List<ConsumoReserva>();
}

public sealed class ParticipanteReserva
{
    public Guid Id { get; set; }
    public Guid ReservaId { get; set; }
    public ReservaWeb Reserva { get; set; } = null!;
    public Guid LineaId { get; set; }
    public required string Nombre { get; set; }
    public DateOnly Nacimiento { get; set; }
    public string? TutorNombre { get; set; }
    public DateOnly? TutorNacimiento { get; set; }
}

public sealed class ConsumoReserva
{
    public Guid Id { get; set; }
    public Guid ReservaId { get; set; }
    public ReservaWeb Reserva { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public required string Sku { get; set; }
    public required string Nombre { get; set; }
    public int CantidadPorPersona { get; set; }
    public int CantidadTotal { get; set; }
}
