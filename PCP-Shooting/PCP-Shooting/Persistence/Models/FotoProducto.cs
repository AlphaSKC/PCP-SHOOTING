namespace PCP_Shooting.Persistence.Models;

// La galería es independiente del historial de cargas, que se conserva para reintentos y F22.
public sealed class FotoProducto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public required string Url { get; set; }
    public int Orden { get; set; }
}
