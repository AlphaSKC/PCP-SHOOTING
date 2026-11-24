using Microsoft.EntityFrameworkCore;
using System.Data;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class ProductosPersistence(PcpShootingDbContext db) : IProductosPersistence
{
    public Task<Producto?> BuscarPorSkuAsync(string sku, CancellationToken cancellationToken = default) =>
        db.Productos.SingleOrDefaultAsync(producto => producto.Sku == sku, cancellationToken);

    public Task<IReadOnlyList<ExistenciaTalla>> ConsultarTallasAsync(Guid productoId, CancellationToken ct) => TallasProducto(productoId, ct);
    private async Task<IReadOnlyList<ExistenciaTalla>> TallasProducto(Guid id, CancellationToken ct) => (await ConsultasTallas.CargarAsync(db, [id], ct))[id];

    public async Task<PaginaConsulta<ProductoCatalogo>> ListarCatalogoAsync(FiltroProductos filtro, CancellationToken ct)
    {
        await using var lectura = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var pagina = await PaginacionConsulta.CrearAsync(ProyectarCatalogo(Filtrar(filtro).Where(p => p.Activo && p.Categoria != CategoriaProducto.Consumible)
            .OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.Id)), filtro, ct);
        var tallas = await ConsultasTallas.CargarAsync(db, pagina.Elementos.Where(p => p.UsaTallas && p.EsVendible).Select(p => p.Id).ToArray(), ct);
        return pagina with { Elementos = pagina.Elementos.Select(p => p with { Tallas = tallas.GetValueOrDefault(p.Id) }).ToArray() };
    }

    public Task<ProductoCatalogo?> ConsultarAlimentoAsync(string sku, CancellationToken ct) =>
        ProyectarCatalogo(db.Productos.AsNoTracking().Where(p => p.Sku == sku && p.Activo && p.EsVendible && p.Categoria == CategoriaProducto.Alimento))
            .SingleOrDefaultAsync(ct);

    private IQueryable<ProductoCatalogo> ProyectarCatalogo(IQueryable<Producto> consulta) =>
        consulta.Select(p => new ProductoCatalogo(p.Id, p.Sku, p.Nombre, p.Descripcion,
            p.EsVendible, p.Precio, p.EspecificacionesTecnicas, p.Categoria, p.ImagenUrl, p.NotasMarkdown,
            p.EsVendible ? db.MovimientosInventario.Where(m => m.ProductoId == p.Id).Sum(m => (long?)m.VariacionCantidad) ?? 0 : null,
            p.EsVendible ? (db.MovimientosInventario.Where(m => m.ProductoId == p.Id).Sum(m => (long?)m.VariacionCantidad) ?? 0) <= 0 : null,
            p.UsaTallas, null));

    public async Task<PaginaConsulta<ProductoGestion>> ListarGestionAsync(FiltroProductosAdministrativo filtro, CancellationToken ct)
    {
        await using var lectura = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var consulta = Filtrar(filtro);
        if (filtro.Activo.HasValue)
            consulta = consulta.Where(p => p.Activo == filtro.Activo.Value);

        var pagina = await PaginacionConsulta.CrearAsync(consulta.OrderBy(p => p.Nombre).ThenBy(p => p.Sku).ThenBy(p => p.Id)
            .Select(p => new ProductoGestion(p.Id, p.Sku, p.Nombre, p.Descripcion,
                p.EsVendible, p.Precio, p.Activo, p.StockMinimo, p.Categoria, p.ImagenUrl, p.NotasMarkdown, p.UsaTallas, null)), filtro, ct);
        var tallas = await ConsultasTallas.CargarAsync(db, pagina.Elementos.Where(p => p.UsaTallas).Select(p => p.Id).ToArray(), ct);
        return pagina with { Elementos = pagina.Elementos.Select(p => p with { Tallas = tallas.GetValueOrDefault(p.Id) }).ToArray() };
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
