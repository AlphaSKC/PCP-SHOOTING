using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class ProductosPersistence(PcpShootingDbContext db) : IProductosPersistence
{
    public Task<Producto?> BuscarPorSkuAsync(string sku, CancellationToken cancellationToken = default) =>
        db.Productos.SingleOrDefaultAsync(producto => producto.Sku == sku, cancellationToken);

    public void Agregar(Producto producto) => db.Productos.Add(producto);
}
