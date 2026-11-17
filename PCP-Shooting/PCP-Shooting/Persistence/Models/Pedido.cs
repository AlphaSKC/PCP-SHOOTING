namespace PCP_Shooting.Persistence.Models;

public sealed class Pedido
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime? RecogidoEnUtc { get; set; }
    public EstadoPedido Estado { get; set; }
    public ICollection<DetallePedido> Detalles { get; } = new List<DetallePedido>();
    public ICollection<Pago> Pagos { get; } = new List<Pago>();
}

public enum EstadoPedido
{
    PendientePago = 0,
    Pagado = 1,
    Recogido = 2,
    Cancelado = 3
}
