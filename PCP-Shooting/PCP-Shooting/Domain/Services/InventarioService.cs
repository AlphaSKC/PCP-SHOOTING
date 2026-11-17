using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class InventarioService(IInventarioPersistence inventario) : IInventarioService
{
    public Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default) =>
        inventario.ConsultarExistenciaAsync(productoId, cancellationToken);

    public async Task<IReadOnlyList<MovimientoConsulta>> ConsultarHistorialAsync(
        Guid productoId, CancellationToken cancellationToken = default)
    {
        var movimientos = await inventario.ListarMovimientosAsync(productoId, cancellationToken);
        return movimientos.Select(movimiento => new MovimientoConsulta(
            movimiento.Id, movimiento.RegistradoEnUtc, movimiento.Tipo.ToString(),
            movimiento.VariacionCantidad, movimiento.Motivo)).ToArray();
    }
}
