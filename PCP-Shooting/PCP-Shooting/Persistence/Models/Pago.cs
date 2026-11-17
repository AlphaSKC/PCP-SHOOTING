namespace PCP_Shooting.Persistence.Models;

public sealed class Pago
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public decimal Importe { get; set; }
    public string? ReferenciaExterna { get; set; }
    public EstadoPago Estado { get; set; }
    public DateTime? ConfirmadoEnUtc { get; set; }
}

public enum EstadoPago
{
    Pendiente = 0,
    Confirmado = 1,
    Rechazado = 2
}
