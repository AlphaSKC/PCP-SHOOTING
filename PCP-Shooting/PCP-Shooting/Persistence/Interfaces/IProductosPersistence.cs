using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IProductosPersistence
{
    Task<Producto?> BuscarPorSkuAsync(string sku, CancellationToken cancellationToken = default);
    void Agregar(Producto producto);
    Task<PaginaConsulta<ProductoCatalogo>> ListarCatalogoAsync(FiltroProductos filtro, CancellationToken ct);
    Task<PaginaConsulta<ProductoGestion>> ListarGestionAsync(FiltroProductosAdministrativo filtro, CancellationToken ct);
}
