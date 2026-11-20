using System.ComponentModel.DataAnnotations;

namespace PCP_Shooting.Domain.Models;

public sealed class HorarioOperacionDatos
{
    [Range(0, 6)] public int DiaSemana { get; init; }
    public bool Cerrado { get; init; }
    public TimeOnly Apertura { get; init; }
    public TimeOnly Cierre { get; init; }
    public TimeOnly PrimerTurno { get; init; }
    public TimeOnly UltimoTurno { get; init; }
}

public sealed class GuardarConfiguracionOperativa
{
    public Guid Version { get; init; }
    [Range(1, 240)] public int DuracionActividadMinutos { get; init; }
    [Range(0, 60)] public int MargenMinutos { get; init; }
    [Range(1, 60)] public int ApartadoPagoMinutos { get; init; }
    [Range(0, 10080)] public int AnticipacionMinimaMinutos { get; init; }
    [Range(1, 365)] public int AnticipacionMaximaDias { get; init; }
    [Range(0, 60)] public int LlegadaAnticipadaMinutos { get; init; }
    [Range(0, 60)] public int ToleranciaLlegadaMinutos { get; init; }
    [Required, MinLength(7), MaxLength(7)] public List<HorarioOperacionDatos> Horarios { get; init; } = new();
}

public sealed class GuardarLineaTiro
{
    [Required, StringLength(80)] public required string Nombre { get; init; }
    public bool Activa { get; init; }
    public Guid Version { get; init; }
}

public sealed class GuardarBloqueoOperacion
{
    public Guid? LineaId { get; init; }
    public DateTimeOffset InicioUtc { get; init; }
    public DateTimeOffset FinUtc { get; init; }
    [Required, StringLength(300)] public required string Motivo { get; init; }
    public bool Activo { get; init; } = true;
    public Guid? Version { get; init; }
}

public sealed class FiltroBloqueosOperacion : FiltroFechas
{
    public Guid? LineaId { get; init; }
    public bool? Activo { get; init; } = true;
}

public sealed record ConfiguracionOperativaConsulta(string ZonaHoraria, int DuracionActividadMinutos, int MargenMinutos,
    int IntervaloTurnosMinutos, int ApartadoPagoMinutos, int AnticipacionMinimaMinutos, int AnticipacionMaximaDias,
    int LlegadaAnticipadaMinutos, int ToleranciaLlegadaMinutos, Guid Version, IReadOnlyList<HorarioOperacionDatos> Horarios);
public sealed record LineaTiroConsulta(Guid Id, int Numero, string Nombre, bool Activa, int Capacidad, Guid Version);
public sealed record BloqueoOperacionConsulta(Guid Id, Guid? LineaId, DateTime InicioUtc, DateTime FinUtc,
    string Motivo, bool Activo, Guid CreadoPorId, DateTime CreadoEnUtc, DateTime ActualizadoEnUtc, Guid Version);
