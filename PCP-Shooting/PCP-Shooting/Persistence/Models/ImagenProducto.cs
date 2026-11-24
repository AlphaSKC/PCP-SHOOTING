namespace PCP_Shooting.Persistence.Models;

public sealed class ImagenProducto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public Guid CreadorId { get; set; }
    public required string Ruta { get; set; }
    public required string Sha256 { get; set; }
    public required string TipoContenido { get; set; }
    public long Tamano { get; set; }
    public bool Disponible { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime? SubidaEnUtc { get; set; }
}