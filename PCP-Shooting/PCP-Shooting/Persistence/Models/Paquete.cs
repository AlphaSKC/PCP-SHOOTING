namespace PCP_Shooting.Persistence.Models;

public sealed class Paquete
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public required string IncluyeMarkdown { get; set; }
    public string? RequisitosMarkdown { get; set; }
    public decimal? PrecioPorPersona { get; set; }
    public decimal? PrecioSocioCartaq { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
    public Guid Version { get; set; }
    public ICollection<ConsumiblePaquete> Consumibles { get; } = new List<ConsumiblePaquete>();
}

public sealed class ConsumiblePaquete
{
    public Guid Id { get; set; }
    public Guid PaqueteId { get; set; }
    public Paquete Paquete { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int CantidadPorPersona { get; set; }
}
