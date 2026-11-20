namespace PCP_Shooting.Persistence.Models;

public sealed class ConfiguracionOperativa
{
    public static readonly Guid IdUnico = Guid.Parse("e4ec8df0-0000-4000-8000-000000000001");
    public Guid Id { get; set; }
    public int DuracionActividadMinutos { get; set; } = 10;
    public int MargenMinutos { get; set; } = 5;
    public int ApartadoPagoMinutos { get; set; } = 10;
    public int AnticipacionMinimaMinutos { get; set; } = 60;
    public int AnticipacionMaximaDias { get; set; } = 2;
    public int LlegadaAnticipadaMinutos { get; set; } = 3;
    public int ToleranciaLlegadaMinutos { get; set; } = 3;
    public Guid Version { get; set; }
}

public sealed class HorarioOperacion
{
    public Guid Id { get; set; }
    // Convención de System.DayOfWeek: domingo=0, lunes=1, ... sábado=6.
    public int DiaSemana { get; set; }
    public bool Cerrado { get; set; }
    public TimeOnly Apertura { get; set; }
    public TimeOnly Cierre { get; set; }
    public TimeOnly PrimerTurno { get; set; }
    public TimeOnly UltimoTurno { get; set; }
}

public sealed class LineaTiro
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public required string Nombre { get; set; }
    public bool Activa { get; set; } = true;
    public int Capacidad { get; set; } = 1;
    public Guid Version { get; set; }
}

public sealed class BloqueoOperacion
{
    public Guid Id { get; set; }
    // null bloquea todo el club; un ID bloquea una sola línea.
    public Guid? LineaId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public required string Motivo { get; set; }
    public bool Activo { get; set; } = true;
    public Guid CreadoPorId { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
    public Guid Version { get; set; }
}
