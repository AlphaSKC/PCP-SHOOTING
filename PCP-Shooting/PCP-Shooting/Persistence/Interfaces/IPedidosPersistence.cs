using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Persistence.Interfaces;

public interface IPedidosPersistence
{
    Task<Pedido?> BuscarAsync(Guid pedidoId, CancellationToken cancellationToken = default);
    Task<List<Pedido>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    void Agregar(Pedido pedido);
}
