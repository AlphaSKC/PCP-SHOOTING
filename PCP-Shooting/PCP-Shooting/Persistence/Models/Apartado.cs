namespace PCP_Shooting.Persistence.Models;

public sealed class Apartado
{
    // El ID de operación enviado por el cliente identifica el apartado y sus reintentos.
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid PaqueteId { get; set; }
    public Guid PaqueteVersion { get; set; }
    public Guid ConfiguracionVersion { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public required string ZonaHoraria { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinActividadUtc { get; set; }
    public DateTime FinOcupacionUtc { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime VenceEnUtc { get; set; }
    public ICollection<LineaApartado> Lineas { get; } = new List<LineaApartado>();
}

public sealed class LineaApartado
{
    public Guid Id { get; set; }
    public Guid ApartadoId { get; set; }
    public Apartado Apartado { get; set; } = null!;
    public Guid LineaId { get; set; }
    public LineaTiro Linea { get; set; } = null!;
}
