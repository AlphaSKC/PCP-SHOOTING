namespace PCP_Shooting.Domain.Interfaces;

public sealed record MovimientoConsulta(Guid Id, DateTime RegistradoEnUtc, string Tipo, int VariacionCantidad, string? Motivo);

public interface IInventarioService
{
    Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoConsulta>> ConsultarHistorialAsync(Guid productoId, CancellationToken cancellationToken = default);
}
