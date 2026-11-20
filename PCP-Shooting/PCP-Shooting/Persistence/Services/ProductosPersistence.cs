using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class ProductosPersistence(PcpShootingDbContext db) : IProductosPersistence
{
    public Task<Producto?> BuscarPorSkuAsync(string sku, CancellationToken cancellationToken = default) =>
        db.Productos.SingleOrDefaultAsync(producto => producto.Sku == sku, cancellationToken);

    public Task<PaginaConsulta<ProductoCatalogo>> ListarCatalogoAsync(FiltroProductos filtro, CancellationToken ct) =>
        PaginacionConsulta.CrearAsync(Filtrar(filtro).Where(p => p.Activo && p.Categoria != CategoriaProducto.Consumible)
            .OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.Id)
            .Select(p => new ProductoCatalogo(p.Id, p.Sku, p.Nombre, p.Descripcion,
                p.EsVendible, p.Precio, p.EspecificacionesTecnicas, p.Categoria, p.ImagenUrl, p.NotasMarkdown)), filtro, ct);

    public Task<PaginaConsulta<ProductoGestion>> ListarGestionAsync(FiltroProductosAdministrativo filtro, CancellationToken ct)
    {
        var consulta = Filtrar(filtro);
        if (filtro.Activo.HasValue)
            consulta = consulta.Where(p => p.Activo == filtro.Activo.Value);

        return PaginacionConsulta.CrearAsync(consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.Id)
            .Select(p => new ProductoGestion(p.Id, p.Sku, p.Nombre, p.Descripcion,
                p.EsVendible, p.Precio, p.Activo, p.StockMinimo, p.Categoria, p.ImagenUrl, p.NotasMarkdown)), filtro, ct);
    }

    private IQueryable<Producto> Filtrar(FiltroProductos filtro)
    {
        var consulta = db.Productos.AsNoTracking();
        var busqueda = filtro.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(busqueda))
            consulta = consulta.Where(p => p.Sku.ToUpper().Contains(busqueda) ||
                p.Nombre.ToUpper().Contains(busqueda) ||
                (p.Descripcion != null && p.Descripcion.ToUpper().Contains(busqueda)));
        if (filtro.Categoria.HasValue)
            consulta = consulta.Where(p => p.Categoria == filtro.Categoria.Value);
        if (filtro.EsVendible.HasValue)
            consulta = consulta.Where(p => p.EsVendible == filtro.EsVendible.Value);
        if (filtro.PrecioMinimo.HasValue)
            consulta = consulta.Where(p => p.Precio >= filtro.PrecioMinimo.Value);
        if (filtro.PrecioMaximo.HasValue)
            consulta = consulta.Where(p => p.Precio <= filtro.PrecioMaximo.Value);
        return consulta;
    }

    public void Agregar(Producto producto) => db.Productos.Add(producto);
}