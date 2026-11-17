using Microsoft.EntityFrameworkCore;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Services;

public sealed class PedidosPersistence(PcpShootingDbContext db) : IPedidosPersistence
{
    public Task<Pedido?> BuscarAsync(Guid pedidoId, CancellationToken cancellationToken = default) =>
        db.Pedidos.Include(pedido => pedido.Detalles).Include(pedido => pedido.Pagos)
            .SingleOrDefaultAsync(pedido => pedido.Id == pedidoId, cancellationToken);

    public Task<List<Pedido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        db.Pedidos.AsNoTracking().Where(pedido => pedido.UsuarioId == usuarioId)
            .OrderByDescending(pedido => pedido.CreadoEnUtc).ToListAsync(cancellationToken);

    public void Agregar(Pedido pedido) => db.Pedidos.Add(pedido);
}
