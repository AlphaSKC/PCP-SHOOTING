namespace PCP_Shooting.Persistence.Models;

public sealed class EventoAuditoria
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public required string Entidad { get; set; }
    public Guid EntidadId { get; set; }
    public required string Accion { get; set; }
    public required string Campos { get; set; }
    public DateTime FechaUtc { get; set; }
    public string? SolicitudId { get; set; }
}