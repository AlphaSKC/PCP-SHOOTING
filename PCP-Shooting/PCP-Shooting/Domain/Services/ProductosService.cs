using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Persistence.Interfaces;

namespace PCP_Shooting.Domain.Services;

public sealed class ProductosService(IProductosPersistence productos) : IProductosService
{
    public async Task<FichaProducto?> ConsultarFichaAsync(string sku, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        var producto = await productos.BuscarPorSkuAsync(sku.Trim(), cancellationToken);

        return producto is null
            ? null
            : new FichaProducto(producto.Id, producto.Sku, producto.Nombre, producto.Descripcion,
                producto.EspecificacionesTecnicas?.Clone());
    }
}
