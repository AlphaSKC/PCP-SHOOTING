using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public sealed class FiltroReportePagos : FiltroFechas
{
    public Guid? UsuarioId { get; init; }
    public Guid? PedidoId { get; init; }
    [EnumDataType(typeof(EstadoPago))] public EstadoPago? Estado { get; init; }
    [StringLength(200)] public string? Referencia { get; init; }
}

public sealed class FiltroReporteMembresias : FiltroPagina, IValidatableObject
{
    public Guid? UsuarioId { get; init; }
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }
    public DateOnly? VigenteEn { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Desde.HasValue && Hasta.HasValue && Hasta <= Desde)
            yield return new ValidationResult("Hasta debe ser posterior a Desde.", new[] { nameof(Hasta) });
    }
}

public sealed record MembresiaReporte(Guid Id, Guid UsuarioId, Guid PagoId,
    DateOnly InicioVigencia, DateOnly FinVigenciaExclusivo, EstadoPago EstadoPago, decimal Importe);

public sealed record Reporte<T>(PaginaConsulta<T> Datos, decimal? ImporteConfirmado = null);
