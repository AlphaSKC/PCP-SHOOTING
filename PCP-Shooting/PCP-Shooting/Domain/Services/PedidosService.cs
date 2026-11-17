using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;
using PCP_Shooting.Persistence.Interfaces;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Services;

public sealed class PedidosService(IPedidosPersistence pedidos) : IPedidosService
{
    public async Task<IReadOnlyList<PedidoResumen>> ListarPorUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var resultados = await pedidos.ListarPorUsuarioAsync(usuarioId, cancellationToken);
        return resultados.Select(CrearResumen).ToArray();
    }

    public async Task<PedidoConsulta?> ConsultarAsync(
        Guid usuarioId, Guid pedidoId, CancellationToken cancellationToken = default)
    {
        var pedido = await pedidos.BuscarAsync(pedidoId, cancellationToken);
        if (pedido is null || pedido.UsuarioId != usuarioId)
        {
            return null;
        }

        var detalles = pedido.Detalles.Select(detalle => new DetallePedidoConsulta(
            detalle.ProductoId, detalle.Cantidad, detalle.PrecioUnitario)).ToArray();
        var total = detalles.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario);
        var importeConfirmado = pedido.Pagos
            .Where(pago => pago.Estado == EstadoPago.Confirmado)
            .Sum(pago => pago.Importe);

        return new PedidoConsulta(CrearResumen(pedido), detalles, total, importeConfirmado);
    }

    private static PedidoResumen CrearResumen(Pedido pedido) =>
        new(pedido.Id, pedido.CreadoEnUtc, pedido.Estado.ToString(), pedido.RecogidoEnUtc);
}
