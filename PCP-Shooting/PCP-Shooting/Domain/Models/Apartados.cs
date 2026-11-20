using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public sealed class CrearApartado
{
    public Guid IdOperacion { get; init; }
    public Guid PaqueteId { get; init; }
    [Required] public DateOnly? Fecha { get; init; }
    [Required] public TimeOnly? Hora { get; init; }
    [Required, MinLength(1), MaxLength(15)] public List<Guid> Lineas { get; init; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter<EstadoApartado>))]
public enum EstadoApartado { Vigente = 0, Vencido = 1 }

public sealed class FiltroApartados : FiltroPagina
{
    [EnumDataType(typeof(EstadoApartado))] public EstadoApartado? Estado { get; init; }
}

public sealed record HistorialApartadosConsulta(IReadOnlyList<ApartadoConsulta> Elementos, int Total,
    int Pagina, int TamanoPagina, DateTime ConsultadoEnUtc);

public sealed record ApartadoConsulta(Guid Id, Guid PaqueteId, DateOnly Fecha, TimeOnly Hora, string ZonaHoraria,
    DateTime InicioUtc, DateTime FinActividadUtc, DateTime FinOcupacionUtc,
    DateTime CreadoEnUtc, DateTime VenceEnUtc, EstadoApartado Estado, IReadOnlyList<Guid> Lineas);
