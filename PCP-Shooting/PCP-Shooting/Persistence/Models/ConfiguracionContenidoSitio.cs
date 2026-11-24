namespace PCP_Shooting.Persistence.Models;

public sealed class ConfiguracionContenidoSitio
{
    public static readonly Guid IdUnico = Guid.Parse("e4ec8df0-0000-4000-8000-000000000020");
    public Guid Id { get; set; }
    public required string DatosJson { get; set; }
    public Guid Version { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
