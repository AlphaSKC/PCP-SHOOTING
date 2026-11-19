using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Models;
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
    public Task<PaginaConsulta<AlertaInventario>> ConsultarAlertasAsync(FiltroPagina filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
        return inventario.ConsultarAlertasAsync(filtro, ct);
    }

    public Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
        return inventario.ListarAsync(filtro, ct);
    }

}
