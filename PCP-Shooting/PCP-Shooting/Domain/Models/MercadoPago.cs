namespace PCP_Shooting.Domain.Models;

public sealed record CheckoutMercadoPago(Guid PagoId, string PreferenciaId, string CheckoutUrl, decimal Importe, string Estado);
public sealed record ConciliacionMercadoPago(int Consultados, int Pagina, bool HayMas);
public sealed record CicloConciliacionResultado(int Revisadas,int Fallidas,int Paginas,int Consultados);
