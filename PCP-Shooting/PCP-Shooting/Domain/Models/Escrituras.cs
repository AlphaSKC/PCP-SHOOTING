using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public sealed class GuardarProducto
{
    [Required, StringLength(80)] public string Sku { get; init; } = "";
    [Required, StringLength(200)] public string Nombre { get; init; } = "";
    [StringLength(4000)] public string? Descripcion { get; init; }
    [EnumDataType(typeof(CategoriaProducto))] public CategoriaProducto Categoria { get; init; }
    [Url, StringLength(2048)] public string? ImagenUrl { get; init; }
    [StringLength(10000)] public string? NotasMarkdown { get; init; }
    public bool EsVendible { get; init; }
    public bool? UsaTallas { get; init; }
    public decimal? Precio { get; init; }
    public JsonElement? EspecificacionesTecnicas { get; init; }
}

public sealed class CrearPedido
{
    public Guid? UsuarioId { get; init; }
    public Guid IdOperacion { get; init; }
    [Required, MinLength(1), MaxLength(100)]
    public List<PartidaPedido> Partidas { get; init; } = new();
}

public sealed class PartidaPedido
{
    public Guid ProductoId { get; init; }
    [EnumDataType(typeof(TallaPlayera))] public TallaPlayera? Talla { get; init; }
    [Range(1, 100000)] public int Cantidad { get; init; }
}

public sealed class RegistrarPago
{
    public Guid IdOperacion { get; init; }
    [Required, StringLength(200)] public string ReferenciaExterna { get; init; } = "";
}

public sealed class RegistrarMovimiento
{
    public Guid IdOperacion { get; init; }
    [EnumDataType(typeof(TallaPlayera))] public TallaPlayera? Talla { get; init; }
    [EnumDataType(typeof(TipoMovimientoInventario))] public TipoMovimientoInventario Tipo { get; init; }
    public int VariacionCantidad { get; init; }
    [Required, StringLength(500)] public string Motivo { get; init; } = "";
}

public sealed class RenovarMembresia
{
    public Guid IdOperacion { get; init; }
    public Guid UsuarioId { get; init; }
    public Guid PagoId { get; init; }
    public DateOnly InicioVigencia { get; init; }
    public DateOnly FinVigenciaExclusivo { get; init; }
}

public sealed record EscrituraResultado(Guid Id, string Estado);
