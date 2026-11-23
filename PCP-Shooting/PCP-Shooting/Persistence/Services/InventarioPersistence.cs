using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class InventarioPersistence(PcpShootingDbContext db) : IInventarioPersistence
{
    private IQueryable<MovimientoConsulta> Movimientos(FiltroMovimientosInventario filtro)
    {
        var consulta = db.MovimientosInventario.AsNoTracking();
        if (filtro.ProductoId.HasValue) consulta = consulta.Where(m => m.ProductoId == filtro.ProductoId);
        if (filtro.Tipo.HasValue) consulta = consulta.Where(m => m.Tipo == filtro.Tipo);
        if (filtro.DesdeUtc.HasValue) consulta = consulta.Where(m => m.RegistradoEnUtc >= filtro.DesdeUtc.Value.UtcDateTime);
        if (filtro.HastaUtc.HasValue) consulta = consulta.Where(m => m.RegistradoEnUtc < filtro.HastaUtc.Value.UtcDateTime);
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(busqueda)) consulta = consulta.Where(m => m.Producto.Sku.ToUpper().Contains(busqueda) || m.Producto.Nombre.ToUpper().Contains(busqueda));
        var filas = from m in consulta
            let actor = db.EventosAuditoria.Where(e => e.Entidad == nameof(MovimientoInventario) && e.EntidadId == m.Id && e.Accion == "Added" && e.ActorId != null)
                .OrderBy(e => e.FechaUtc).ThenBy(e => e.Id).Select(e => e.ActorId).FirstOrDefault()
            select new { Movimiento = m, ActorId = actor };
        if (filtro.ActorId.HasValue) filas = filas.Where(m => m.ActorId == filtro.ActorId);
        return filas.OrderByDescending(f => f.Movimiento.RegistradoEnUtc).ThenBy(f => f.Movimiento.Id)
            .Select(f => new MovimientoConsulta(f.Movimiento.Id, f.Movimiento.RegistradoEnUtc, f.Movimiento.Tipo.ToString(),
                f.Movimiento.VariacionCantidad, f.Movimiento.Motivo, f.Movimiento.ProductoId, f.Movimiento.Producto.Sku,
                f.Movimiento.Producto.Nombre, f.ActorId, db.PerfilesUsuario.Where(p => p.Id == f.ActorId).Select(p => p.Nombre).FirstOrDefault()));
    }

    public Task<PaginaConsulta<MovimientoConsulta>> ConsultarMovimientosAsync(FiltroMovimientosInventario filtro, CancellationToken ct) =>
        PaginacionConsulta.CrearAsync(Movimientos(filtro), filtro, ct);
    public Task<List<MovimientoConsulta>> ConsultarHistorialConAutorAsync(Guid productoId, CancellationToken ct) =>
        Movimientos(new FiltroMovimientosInventario { ProductoId = productoId }).ToListAsync(ct);

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
    public Task<PaginaConsulta<ExistenciaProducto>> ListarAsync(FiltroInventario filtro, CancellationToken ct) =>
        PaginacionConsulta.CrearAsync(ConsultasInventario.Crear(db, filtro), filtro, ct);
}
