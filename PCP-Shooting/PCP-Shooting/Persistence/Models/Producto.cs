using System.Text.Json;

namespace PCP_Shooting.Persistence.Models;

public sealed class Producto
{
    public Guid Id { get; set; }
    public required string Sku { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public CategoriaProducto Categoria { get; set; }
    public string? ImagenUrl { get; set; }
    public ICollection<ImagenProducto> Imagenes { get; } = new List<ImagenProducto>();
    public string? NotasMarkdown { get; set; }
    public bool Activo { get; set; } = true;
    public int? StockMinimo { get; set; }
    public bool EsVendible { get; set; }
    public decimal? Precio { get; set; }
    public JsonElement? EspecificacionesTecnicas { get; set; }
}

public enum CategoriaProducto
{
    SinClasificar = 0,
    Alimento = 1,
    Playera = 2,
    ReplicaDecorativa = 3,
    Consumible = 4
}