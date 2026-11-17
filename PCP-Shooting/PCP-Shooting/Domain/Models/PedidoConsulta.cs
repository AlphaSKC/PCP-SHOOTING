namespace PCP_Shooting.Domain.Models;

public sealed record PedidoResumen(
    Guid Id, DateTime CreadoEnUtc, string Estado, DateTime? RecogidoEnUtc);

public sealed record DetallePedidoConsulta(Guid ProductoId, int Cantidad, decimal PrecioUnitario);

public sealed record PedidoConsulta(
    PedidoResumen Resumen,
    IReadOnlyList<DetallePedidoConsulta> Detalles,
    decimal Total,
    decimal ImporteConfirmado);
