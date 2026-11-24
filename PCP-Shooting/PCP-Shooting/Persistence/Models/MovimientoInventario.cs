namespace PCP_Shooting.Persistence.Models;

public sealed class MovimientoInventario
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public TallaPlayera? Talla { get; set; }
    public Producto Producto { get; set; } = null!;
    public Guid? DetallePedidoId { get; set; }
    public DetallePedido? DetallePedido { get; set; }
    public DateTime RegistradoEnUtc { get; set; }
    public TipoMovimientoInventario Tipo { get; set; }
    public int VariacionCantidad { get; set; }
    public string? Motivo { get; set; }
}

public enum TipoMovimientoInventario
{
    Entrada = 0,
    Salida = 1,
    Ajuste = 2
}
