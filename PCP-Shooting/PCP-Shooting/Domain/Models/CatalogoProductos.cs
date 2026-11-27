using PCP_Shooting.Persistence.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public class FiltroProductos : FiltroPagina, IValidatableObject
{
    [StringLength(150)]
    public string? Busqueda { get; init; }

    public bool? EsVendible { get; init; }

    [EnumDataType(typeof(CategoriaProducto))]
    public CategoriaProducto? Categoria { get; set; }
    [StringLength(60), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string? CategoriaTienda { get; init; }
    [StringLength(80), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string? Marca { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal? PrecioMinimo { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal? PrecioMaximo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PrecioMinimo.HasValue && PrecioMaximo.HasValue && PrecioMinimo > PrecioMaximo)
            yield return new ValidationResult("El precio máximo debe ser mayor o igual al mínimo.",
                new[] { nameof(PrecioMaximo) });
    }
}

public sealed class FiltroProductosAdministrativo : FiltroProductos
{
    public bool? Activo { get; init; }
}

public sealed class CambiarEstadoProducto
{
    [JsonRequired]
    public bool Activo { get; init; }
}

public sealed class ConfigurarStockMinimo
{
    [JsonRequired, Range(0, int.MaxValue)]
    public int? StockMinimo { get; init; }
}

public sealed record ProductoCatalogo(Guid Id, string Sku, string Nombre, string? Descripcion,
    bool EsVendible, decimal? Precio, JsonElement? EspecificacionesTecnicas,
    CategoriaProducto Categoria = CategoriaProducto.SinClasificar,
    string? ImagenUrl = null, string? NotasMarkdown = null, long? Existencia = null, bool? Agotado = null,
    bool UsaTallas = false, IReadOnlyList<ExistenciaTalla>? Tallas = null,
    CategoriaTiendaReferencia? CategoriaTienda = null, MarcaTiendaReferencia? Marca = null);

public sealed record ProductoGestion(Guid Id, string Sku, string Nombre, string? Descripcion,
    bool EsVendible, decimal? Precio, bool Activo, int? StockMinimo,
    CategoriaProducto Categoria = CategoriaProducto.SinClasificar,
    string? ImagenUrl = null, string? NotasMarkdown = null, bool UsaTallas = false, IReadOnlyList<ExistenciaTalla>? Tallas = null,
    Guid? CategoriaTiendaId = null, Guid? MarcaId = null);

public sealed record ExistenciaTalla(TallaPlayera Talla, long Existencia, bool Agotado);

public sealed record AlertaInventario(Guid ProductoId, string Sku, string Nombre,
    long Existencia, int StockMinimo);
