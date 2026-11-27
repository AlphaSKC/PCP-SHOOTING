namespace PCP_Shooting.Persistence.Models;

public sealed class CategoriaTienda
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; }
    public Guid Version { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
