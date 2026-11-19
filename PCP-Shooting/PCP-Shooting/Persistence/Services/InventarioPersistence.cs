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
        var productos = db.Productos.AsNoTracking().Where(p => p.Activo && p.StockMinimo.HasValue);
        if (filtro is FiltroAlertasInventario { Categoria: not null } alertas)
            productos = productos.Where(p => p.Categoria == alertas.Categoria);
        var consulta = productos.Select(p => new
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
    public Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct)
    {
        var productos = db.Productos.AsNoTracking();
        if (filtro.Categoria.HasValue)
            productos = productos.Where(p => p.Categoria == filtro.Categoria.Value);
        if (filtro.Activo.HasValue)
            productos = productos.Where(p => p.Activo == filtro.Activo.Value);
        if (filtro.EsVendible.HasValue)
            productos = productos.Where(p => p.EsVendible == filtro.EsVendible.Value);
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(busqueda))
            productos = productos.Where(p => p.Sku.ToUpper().Contains(busqueda) || p.Nombre.ToUpper().Contains(busqueda));

        var consulta = productos.Select(p => new
        {
            ProductoId = p.Id, p.Sku, p.Nombre, p.Categoria, p.Activo, p.EsVendible,
            p.ImagenUrl, p.StockMinimo,
            Existencia = db.MovimientosInventario.Where(m => m.ProductoId == p.Id)
                .Sum(m => (long?)m.VariacionCantidad) ?? 0
        });
        if (filtro.ConExistencias == true)
            consulta = consulta.Where(p => p.Existencia > 0);
        if (filtro.ConExistencias == false)
            consulta = consulta.Where(p => p.Existencia <= 0);
        if (filtro.BajoMinimo == true)
            consulta = consulta.Where(p => p.StockMinimo.HasValue && p.Existencia <= p.StockMinimo.Value);
        if (filtro.BajoMinimo == false)
            consulta = consulta.Where(p => !p.StockMinimo.HasValue || p.Existencia > p.StockMinimo.Value);

        return PaginacionConsulta.CrearAsync(consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.ProductoId)
            .Select(p => new ExistenciaProducto(p.ProductoId, p.Sku, p.Nombre, p.Categoria,
                p.Activo, p.EsVendible, p.ImagenUrl, p.Existencia, p.StockMinimo,
                p.StockMinimo.HasValue && p.Existencia <= p.StockMinimo.Value)), filtro, ct);
    }

}