using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IProductosPersistence
{
    Task<Producto?> BuscarPorSkuAsync(string sku, CancellationToken cancellationToken = default);
    void Agregar(Producto producto);
}
