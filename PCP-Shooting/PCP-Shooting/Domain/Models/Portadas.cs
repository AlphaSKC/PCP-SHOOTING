using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace PCP_Shooting.Domain.Models;

public static class PaginasPortada
{
    public static bool Valida(string pagina) => pagina is "inicio" or "nosotros" or "reservar" or "catalogo" or "tienda" or "eventos" or "alimentos" or "membresias" or "contacto";
}
public sealed class GuardarPortada
{
    [JsonRequired] public bool Activa { get; init; }
    [StringLength(2048)] public string? ImagenUrl { get; init; }
    [Required, StringLength(9), RegularExpression("^(100|[1-9]?[0-9])% (100|[1-9]?[0-9])%$")]
    public string Posicion { get; init; } = "50% 50%";
    [Required, RegularExpression("^(suave|normal|fuerte)$")] public string Velo { get; init; } = "normal";
    public Guid? Version { get; init; }
}
public sealed record PortadaPublica(string Pagina, bool Activa, string? ImagenUrl, string Posicion, string Velo);
public sealed record PortadaGestion(string Pagina, bool Activa, string? ImagenUrl, string Posicion, string Velo, Guid Version, DateTime ActualizadoEnUtc);
public sealed record ImagenPortadaResultado(string ImagenUrl, Guid Version);
