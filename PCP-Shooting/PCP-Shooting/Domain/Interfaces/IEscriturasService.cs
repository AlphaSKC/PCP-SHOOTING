using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IEscriturasService
{
    Task<EscrituraResultado> GuardarProductoAsync(Guid? id, GuardarProducto datos, CancellationToken ct);
    Task<EscrituraResultado> CrearPedidoAsync(Guid usuarioId, CrearPedido datos, CancellationToken ct);
    Task<EscrituraResultado> RegistrarPagoAsync(Guid pedidoId, RegistrarPago datos, CancellationToken ct);
    Task<EscrituraResultado> ConfirmarPagoAsync(Guid pagoId, CancellationToken ct);
    Task<EscrituraResultado> CancelarPedidoAsync(Guid actorId, Guid pedidoId, CancellationToken ct);
    Task<EscrituraResultado> RecogerPedidoAsync(Guid pedidoId, CancellationToken ct);
    Task<EscrituraResultado> RegistrarMovimientoAsync(Guid productoId, RegistrarMovimiento datos, CancellationToken ct);
    Task<EscrituraResultado> RenovarMembresiaAsync(RenovarMembresia datos, CancellationToken ct);
    Task<EscrituraResultado> CambiarEstadoProductoAsync(Guid id, CambiarEstadoProducto datos, CancellationToken ct);
    Task<EscrituraResultado> ConfigurarStockMinimoAsync(Guid id, ConfigurarStockMinimo datos, CancellationToken ct);
}