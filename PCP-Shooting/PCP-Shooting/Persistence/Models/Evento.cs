using System.Text.Json.Serialization;

namespace PCP_Shooting.Persistence.Models;

[JsonConverter(typeof(JsonStringEnumConverter<EstadoEvento>))]
public enum EstadoEvento { Borrador = 0, Publicado = 1, Archivado = 2 }

public sealed class Evento
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Resumen { get; set; } = "";
    public string DescripcionMarkdown { get; set; } = "";
    public string? Ubicacion { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public EstadoEvento Estado { get; set; }
    public Guid Version { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
