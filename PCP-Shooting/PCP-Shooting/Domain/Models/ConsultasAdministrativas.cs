using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public class FiltroPagina
{
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;
}

public class FiltroFechas : FiltroPagina, IValidatableObject
{
    public DateTimeOffset? DesdeUtc { get; init; }
    public DateTimeOffset? HastaUtc { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DesdeUtc.HasValue && HastaUtc.HasValue && HastaUtc <= DesdeUtc)
            yield return new ValidationResult("HastaUtc debe ser posterior a DesdeUtc.",
                new[] { nameof(HastaUtc) });
    }
}

public sealed class FiltroPedidos : FiltroFechas
{
    [StringLength(150)] public string? Busqueda { get; init; }
    public Guid? UsuarioId { get; init; }
    [EnumDataType(typeof(EstadoPedido))] public EstadoPedido? Estado { get; init; }
}

public sealed class FiltroPagos : FiltroPagina
{
    [StringLength(200)] public string? Referencia { get; init; }
    public Guid? PedidoId { get; init; }
    public Guid? UsuarioId { get; init; }
    [EnumDataType(typeof(EstadoPago))] public EstadoPago? Estado { get; init; }
}

public sealed class FiltroAuditoria : FiltroFechas
{
    public Guid? ActorId { get; init; }
    public Guid? EntidadId { get; init; }
    [StringLength(100)] public string? Entidad { get; init; }
}

public sealed record PaginaConsulta<T>(IReadOnlyList<T> Elementos, int Total, int Pagina, int TamanoPagina);
public sealed record PedidoAdministrativo(Guid Id, Guid UsuarioId, DateTime CreadoEnUtc, EstadoPedido Estado, decimal Total);
public sealed record PagoAdministrativo(Guid Id, Guid PedidoId, Guid UsuarioId, decimal Importe,
    string? ReferenciaExterna, EstadoPago Estado, DateTime? ConfirmadoEnUtc);
public sealed record MembresiaHistorial(Guid Id, Guid PagoId, DateOnly InicioVigencia, DateOnly FinVigenciaExclusivo);
public sealed record AuditoriaConsulta(Guid Id, Guid? ActorId, string Entidad, Guid EntidadId,
    string Accion, string Campos, DateTime FechaUtc, string? SolicitudId);
public sealed record PedidoDetalleAdministrativo(PedidoAdministrativo Pedido,
    IReadOnlyList<DetallePedidoConsulta> Detalles, IReadOnlyList<PagoAdministrativo> Pagos);
