namespace PCP_Shooting.Domain.Models;

public sealed record CheckoutMercadoPago(Guid PagoId, string PreferenciaId, string CheckoutUrl, decimal Importe, string Estado);
