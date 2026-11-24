using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PCP_Shooting.Domain.Models;

public sealed class ContenidoSitioDatos : IValidatableObject
{
    [JsonRequired, Required, StringLength(300)] public string NosotrosTitulo { get; set; } = "El primer club de tiro exclusivo para armas de aire comprimido en México";
    [JsonRequired, Required, StringLength(2000)] public string NosotrosTexto { get; set; } = "En la Ruta de los Cenotes, Quintana Roo, reunimos a entusiastas y principiantes del tiro deportivo y recreativo con rifles PCP, en un entorno seguro y conforme a la normativa aplicable.";
    [JsonRequired, Required, StringLength(150)] public string SeguridadTitulo { get; set; } = "Un campo seguro para todos";
    [JsonRequired, Required, StringLength(2000)] public string SeguridadTexto { get; set; } = "Consulta el reglamento del campo y los requisitos de participación antes de realizar tu actividad.";
    [JsonRequired, StringLength(1000)] public string? Testimonio { get; set; }
    [JsonRequired, StringLength(150)] public string? AutorTestimonio { get; set; }
    [JsonRequired, Required, StringLength(500)] public string DescripcionPie { get; set; } = "El primer club de tiro exclusivo para armas de aire comprimido en México. Rifles PCP para tiro deportivo y recreativo, con energía máxima de 140 joules.";
    [JsonRequired, Required, StringLength(300)] public string Direccion { get; set; } = "Km 23 Ruta de los Cenotes, Quintana Roo";
    [JsonRequired, Required, StringLength(300)] public string HorarioPublico { get; set; } = "Martes a domingo, 8:00 a 18:00 · lunes cerrado";
    [JsonRequired, Required, StringLength(40), RegularExpression(@"^\+?[0-9 ()-]{7,40}$")] public string Telefono { get; set; } = "998 159 6699";
    [JsonRequired, Required, RegularExpression(@"^\+[1-9][0-9]{6,14}$")] public string TelefonoMarcacion { get; set; } = "+529981596699";
    [JsonRequired, Required, RegularExpression(@"^[1-9][0-9]{6,14}$")] public string WhatsAppNumero { get; set; } = "529981596699";
    [JsonRequired, Required, EmailAddress, StringLength(254)] public string Correo { get; set; } = "contacto@pcpshooting.com";
    [JsonRequired, Required, StringLength(300)] public string UbicacionMapa { get; set; } = "PcP Shooting Ruta de los Cenotes";
    [JsonRequired, StringLength(2048)] public string? InstagramUrl { get; set; } = "https://www.instagram.com/pcpshooting/";
    [JsonRequired, StringLength(2048)] public string? FacebookUrl { get; set; } = "https://www.facebook.com/profile.php?id=61594533773594";
    [JsonRequired, StringLength(2048)] public string? TikTokUrl { get; set; } = "https://www.tiktok.com/@pcp.shooting";
    [JsonRequired, StringLength(2048)] public string? YouTubeUrl { get; set; } = "https://www.youtube.com/@pcpshooting";

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Testimonio) != string.IsNullOrWhiteSpace(AutorTestimonio))
            yield return new("Completa testimonio y autor, o deja ambos vacíos.", [nameof(Testimonio), nameof(AutorTestimonio)]);
        foreach (var (nombre, valor) in new[] { (nameof(InstagramUrl), InstagramUrl), (nameof(FacebookUrl), FacebookUrl), (nameof(TikTokUrl), TikTokUrl), (nameof(YouTubeUrl), YouTubeUrl) })
            if (!string.IsNullOrWhiteSpace(valor) && (!Uri.TryCreate(valor.Trim(), UriKind.Absolute, out var url) || url.Scheme != "https" || string.IsNullOrEmpty(url.Host) || !string.IsNullOrEmpty(url.UserInfo)))
                yield return new("Las redes sociales requieren un enlace HTTPS válido, sin credenciales.", [nombre]);
    }
}
public sealed class GuardarContenidoSitio
{
    [JsonRequired, Required] public ContenidoSitioDatos Contenido { get; init; } = null!;
    [JsonRequired] public Guid Version { get; init; }
}
public sealed record ContenidoSitioConsulta(ContenidoSitioDatos Contenido, Guid Version, DateTime ActualizadoEnUtc);
