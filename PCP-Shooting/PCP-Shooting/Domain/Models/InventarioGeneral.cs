using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public sealed class FiltroMovimientosInventario : FiltroFechas
{
    public Guid? ProductoId { get; init; }
    public Guid? ActorId { get; init; }
    [EnumDataType(typeof(TallaPlayera))] public TallaPlayera? Talla { get; init; }
    [EnumDataType(typeof(TipoMovimientoInventario))] public TipoMovimientoInventario? Tipo { get; init; }
    [StringLength(150)] public string? Busqueda { get; init; }
}

public sealed class FiltroInventario : FiltroAlertasInventario
{
    [StringLength(150)]
    public string? Busqueda { get; init; }
    public bool? Activo { get; init; }
    public bool? EsVendible { get; init; }
    public bool? ConExistencias { get; init; }
    public bool? BajoMinimo { get; init; }
}

public sealed record ExistenciaProducto(
    Guid ProductoId, string Sku, string Nombre, CategoriaProducto Categoria,
    bool Activo, bool EsVendible, string? ImagenUrl,
    long Existencia, int? StockMinimo, bool BajoMinimo, bool UsaTallas = false);
