using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class InventarioPersistence(PcpShootingDbContext db) : IInventarioPersistence
{
    public Task<long> ConsultarExistenciaAsync(Guid productoId, CancellationToken cancellationToken = default) =>
        db.MovimientosInventario.Where(movimiento => movimiento.ProductoId == productoId)
            .SumAsync(movimiento => (long)movimiento.VariacionCantidad, cancellationToken);

    public Task<List<MovimientoInventario>> ListarMovimientosAsync(Guid productoId, CancellationToken cancellationToken = default) =>
        db.MovimientosInventario.AsNoTracking().Where(movimiento => movimiento.ProductoId == productoId)
            .OrderByDescending(movimiento => movimiento.RegistradoEnUtc).ThenBy(movimiento => movimiento.Id)
            .ToListAsync(cancellationToken);

    public void AgregarMovimiento(MovimientoInventario movimiento) => db.MovimientosInventario.Add(movimiento);
}
