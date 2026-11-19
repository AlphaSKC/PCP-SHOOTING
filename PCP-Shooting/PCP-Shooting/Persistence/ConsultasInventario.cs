using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Persistence;

internal static class ConsultasInventario
{
    internal static IQueryable<ExistenciaProducto> Crear(PcpShootingDbContext db, FiltroInventario filtro)
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

        return consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.ProductoId)
            .Select(p => new ExistenciaProducto(p.ProductoId, p.Sku, p.Nombre, p.Categoria,
                p.Activo, p.EsVendible, p.ImagenUrl, p.Existencia, p.StockMinimo,
                p.StockMinimo.HasValue && p.Existencia <= p.StockMinimo.Value));
    }

}