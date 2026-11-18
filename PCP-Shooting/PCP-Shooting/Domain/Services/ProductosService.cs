using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class ProductosService(IProductosPersistence productos) : IProductosService
{
    public async Task<FichaProducto?> ConsultarFichaAsync(string sku, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        var producto = await productos.BuscarPorSkuAsync(sku.Trim(), cancellationToken);

        return producto is null || !producto.Activo
            ? null
            : new FichaProducto(producto.Id, producto.Sku, producto.Nombre, producto.Descripcion,
                producto.EspecificacionesTecnicas?.Clone(), producto.Categoria, producto.ImagenUrl, producto.NotasMarkdown);
    }

    public Task<PaginaConsulta<ProductoCatalogo>> ListarCatalogoAsync(FiltroProductos filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
        return productos.ListarCatalogoAsync(filtro, ct);
    }

    public Task<PaginaConsulta<ProductoGestion>> ListarGestionAsync(FiltroProductosAdministrativo filtro, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), validateAllProperties: true);
        return productos.ListarGestionAsync(filtro, ct);
    }
}