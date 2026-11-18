using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
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
    public Task<PaginaConsulta<AlertaInventario>> ConsultarAlertasAsync(FiltroPagina filtro, CancellationToken ct)
    {
        var consulta = db.Productos.AsNoTracking()
            .Where(p => p.Activo && p.StockMinimo.HasValue)
            .Select(p => new
            {
                ProductoId = p.Id,
                p.Sku,
                p.Nombre,
                Existencia = db.MovimientosInventario.Where(m => m.ProductoId == p.Id)
                    .Sum(m => (long?)m.VariacionCantidad) ?? 0,
                StockMinimo = p.StockMinimo!.Value
            })
            .Where(p => p.Existencia <= p.StockMinimo)
            .OrderBy(p => p.Existencia).ThenBy(p => p.Sku).ThenBy(p => p.ProductoId);
        return PaginacionConsulta.CrearAsync(consulta.Select(p =>
            new AlertaInventario(p.ProductoId, p.Sku, p.Nombre, p.Existencia, p.StockMinimo)), filtro, ct);
    }

}
