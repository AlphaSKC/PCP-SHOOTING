using System.Text.Json;

namespace PCP_Shooting.Persistence.Models;

public sealed class Producto
{
    public Guid Id { get; set; }
    public required string Sku { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool EsVendible { get; set; }
    public decimal? Precio { get; set; }
    public JsonElement? EspecificacionesTecnicas { get; set; }
}
