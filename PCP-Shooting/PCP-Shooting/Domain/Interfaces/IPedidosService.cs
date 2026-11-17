using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IPedidosService
{
    Task<IReadOnlyList<PedidoResumen>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<PedidoConsulta?> ConsultarAsync(Guid usuarioId, Guid pedidoId, CancellationToken cancellationToken = default);
}
