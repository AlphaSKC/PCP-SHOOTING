namespace PCP_Shooting.Domain.Services;

public sealed class MercadoPagoOptions
{
    public const string Section = "MercadoPago";
    public string? AccessToken { get; set; }
    public string? WebhookSecret { get; set; }
    public string? NotificationUrl { get; set; }
    public string? SuccessUrl { get; set; }
    public string? FailureUrl { get; set; }
    public string? PendingUrl { get; set; }
    public bool UsarEntornoPruebas { get; set; } = true;
}
