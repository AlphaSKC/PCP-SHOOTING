using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public sealed class ConsultaDisponibilidad
{
    [Required] public Guid? PaqueteId { get; init; }
    [Required] public DateOnly? Fecha { get; init; }
    public TimeOnly? Hora { get; init; }
    [Range(1, 15)] public int Personas { get; init; } = 1;
}

[JsonConverter(typeof(JsonStringEnumConverter<EstadoLineaDisponibilidad>))]
public enum EstadoLineaDisponibilidad { Libre = 0, Bloqueada = 1, Inactiva = 2, FueraDeAnticipacion = 3, Apartada = 4 }

public sealed record LineaDisponibilidad(Guid Id, int Numero, string Nombre, bool Disponible, EstadoLineaDisponibilidad Estado);
public sealed record TurnoDisponibilidad(TimeOnly Hora, DateTime InicioUtc, DateTime FinActividadUtc,
    DateTime FinOcupacionUtc, bool Disponible, int LineasLibres, IReadOnlyList<LineaDisponibilidad> Lineas);
public sealed record DisponibilidadConsulta(Guid PaqueteId, DateOnly Fecha, string ZonaHoraria,
    DateTime ConsultadoEnUtc, bool DiaAbierto, int Personas, IReadOnlyList<TurnoDisponibilidad> Horarios);
