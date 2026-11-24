using System.Text.Json.Serialization;

namespace PCP_Shooting.Persistence.Models;

[JsonConverter(typeof(JsonStringEnumConverter<CategoriaBlog>))]
public enum CategoriaBlog { Guias = 0, Tecnica = 1, Eventos = 2, Club = 3 }

[JsonConverter(typeof(JsonStringEnumConverter<EstadoArticuloBlog>))]
public enum EstadoArticuloBlog { Borrador = 0, Programado = 1, Publicado = 2, Archivado = 3 }

public sealed class ArticuloBlog
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Titulo { get; set; }
    public required string Resumen { get; set; }
    public required string ContenidoMarkdown { get; set; }
    public CategoriaBlog Categoria { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Destacado { get; set; }
    public EstadoArticuloBlog Estado { get; set; }
    public DateTime? PublicarEnUtc { get; set; }
    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
    public Guid AutorId { get; set; }
    public Guid Version { get; set; }
}
