using System.Text.Json.Serialization;

namespace PCP_Shooting.Persistence.Models;

[JsonConverter(typeof(JsonStringEnumConverter<EstadoOrdenAlimentos>))]
public enum EstadoOrdenAlimentos { Recibido=0, EnPreparacion=1, Listo=2, Entregado=3 }
[JsonConverter(typeof(JsonStringEnumConverter<MetodoCobroAlimentos>))]
public enum MetodoCobroAlimentos { Tarjeta=0, Transferencia=1 }

public sealed class OrdenAlimentos
{
    public Guid Id { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid CreadaPorId { get; set; }
    public bool EsMostrador { get; set; }
    public required string NombreRecogida { get; set; }
    public string? Notas { get; set; }
    public required string SolicitudHash { get; set; }
    public EstadoOrdenAlimentos Estado { get; set; }
    public decimal Total { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadaEnUtc { get; set; }
    public DateTime ActualizadaEnUtc { get; set; }
    public DateTime? EnPreparacionEnUtc { get; set; }
    public DateTime? ListaEnUtc { get; set; }
    public DateTime? EntregadaEnUtc { get; set; }
    public DateTime? PagoConfirmadoEnUtc { get; set; }
    public decimal ImporteConfirmado { get; set; }
    public MetodoCobroAlimentos? MetodoCobro { get; set; }
    public string? ReferenciaCobro { get; set; }
    public ICollection<DetalleOrdenAlimentos> Detalles { get; set; } = new List<DetalleOrdenAlimentos>();
}

public sealed class DetalleOrdenAlimentos
{
    public Guid Id { get; set; }
    public Guid OrdenId { get; set; }
    public Guid ProductoId { get; set; }
    public required string Sku { get; set; }
    public required string Nombre { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public sealed class OperacionOrdenAlimentos
{
    public Guid Id { get; set; }
    public Guid OrdenId { get; set; }
    public Guid ActorId { get; set; }
    public required string Tipo { get; set; }
    public required string SolicitudHash { get; set; }
    public DateTime CreadaEnUtc { get; set; }
}
