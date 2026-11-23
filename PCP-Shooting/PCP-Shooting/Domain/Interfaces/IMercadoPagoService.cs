using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Domain.Interfaces;

public interface IMercadoPagoService
{
    Task<CheckoutMercadoPago> CrearCheckoutPedidoAsync(Guid usuarioId, Guid pedidoId, CancellationToken ct);
    Task<CheckoutMercadoPago> CrearCheckoutReservaAsync(Guid usuarioId, Guid reservaId, CancellationToken ct);
    Task<CheckoutMercadoPago> CrearCheckoutReservaMostradorAsync(Guid reservaId, CancellationToken ct);
    Task<bool> ProcesarNotificacionAsync(string? dataId, string? firma, string? requestId, CancellationToken ct);
    Task<ConciliacionMercadoPago> ConciliarAsync(string tipo, Guid id, int pagina, CancellationToken ct);
}
