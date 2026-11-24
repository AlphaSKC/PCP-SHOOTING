using System.Text.Json.Serialization;

namespace PCP_Shooting.Persistence.Models;

[JsonConverter(typeof(JsonStringEnumConverter<EstadoSolicitudContacto>))]
public enum EstadoSolicitudContacto { Pendiente = 0, Atendida = 1, Archivada = 2 }

public sealed class SolicitudContacto
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public string? Telefono { get; set; }
    public required string Motivo { get; set; }
    public required string Mensaje { get; set; }
    public required string SolicitudHash { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime PrivacidadAceptadaEnUtc { get; set; }
    public EstadoSolicitudContacto Estado { get; set; }
    public string? NotasInternas { get; set; }
    public Guid Version { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
