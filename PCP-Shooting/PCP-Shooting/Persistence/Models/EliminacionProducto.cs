namespace PCP_Shooting.Persistence.Models;

// Sin FK al producto: este registro sobrevive al borrado y permite reanudar Storage.
public sealed class EliminacionProducto
{
    public Guid Id { get; set; }
    public CategoriaProducto Categoria { get; set; }
    public Guid ActorId { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime? CompletadaEnUtc { get; set; }
    public ICollection<ArchivoProductoEliminado> Archivos { get; set; } = new List<ArchivoProductoEliminado>();
}

public sealed class ArchivoProductoEliminado
{
    public Guid Id { get; set; }
    public Guid EliminacionId { get; set; }
    public required string Ruta { get; set; }
    public DateTime? EliminadoEnUtc { get; set; }
}
