namespace PCP_Shooting.Persistence.Models;

public sealed class ConfiguracionAvisos
{
    public static readonly Guid IdUnico = Guid.Parse("e4ec8df0-0000-4000-8000-000000000010");
    public Guid Id { get; set; }
    public string? NotifyEmail { get; set; }
    public string WaOrderReady { get; set; } = "";
    public string WaReminder { get; set; } = "";
    public Guid Version { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }
}
