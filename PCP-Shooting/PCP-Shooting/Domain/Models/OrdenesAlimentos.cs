using System.ComponentModel.DataAnnotations;
using PCP_Shooting.Persistence.Models;

namespace PCP_Shooting.Domain.Models;

public sealed class OrdenesAlimentosOptions { public bool OrdenesHabilitadas { get; set; } }
public sealed class CrearOrdenAlimentos
{
    public Guid IdOperacion { get; init; }
    public Guid? ClienteId { get; init; }
    [StringLength(150)] public string? NombreRecogida { get; init; }
    [StringLength(500)] public string? Notas { get; init; }
    [Required,MinLength(1),MaxLength(50)] public IReadOnlyList<PartidaOrdenAlimentos> Partidas { get; init; } = Array.Empty<PartidaOrdenAlimentos>();
}
public sealed record PartidaOrdenAlimentos(Guid ProductoId,[Range(1,100)] int Cantidad);
public class MutacionOrdenAlimentos
{
    public Guid IdOperacion { get; init; }
    public Guid Version { get; init; }
}
public sealed class CambiarEstadoOrdenAlimentos : MutacionOrdenAlimentos
{
    [EnumDataType(typeof(EstadoOrdenAlimentos))] public EstadoOrdenAlimentos Estado { get; init; }
}
public sealed class RegistrarCobroAlimentos : MutacionOrdenAlimentos
{
    [Required,EnumDataType(typeof(MetodoCobroAlimentos))] public MetodoCobroAlimentos? Metodo { get; init; }
    [Required,StringLength(150)] public string Referencia { get; init; } = "";
    public decimal Importe { get; init; }
}
public sealed class FiltroOrdenesAlimentos : FiltroFechas
{
    [EnumDataType(typeof(EstadoOrdenAlimentos))] public EstadoOrdenAlimentos? Estado { get; init; }
    public bool? Pagada { get; init; }
}
public sealed record PartidaOrdenAlimentosConsulta(Guid Id,Guid ProductoId,string Sku,string Nombre,int Cantidad,decimal PrecioUnitario);
public sealed record OrdenAlimentosConsulta(Guid Id,Guid? ClienteId,bool EsMostrador,string NombreRecogida,string? Notas,
    string Estado,decimal Total,string EstadoPago,decimal ImporteConfirmado,MetodoCobroAlimentos? MetodoCobro,string? ReferenciaCobro,
    DateTime CreadaEnUtc,DateTime ActualizadaEnUtc,DateTime? EnPreparacionEnUtc,DateTime? ListaEnUtc,
    DateTime? PagoConfirmadoEnUtc,DateTime? EntregadaEnUtc,Guid Version,IReadOnlyList<PartidaOrdenAlimentosConsulta> Partidas);
