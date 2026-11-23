using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Authorize(Policy = "Administrador"), Route("api/administracion/mercadopago")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ConciliacionMercadoPagoController(IMercadoPagoService pagos) : ControllerBase
{
    [HttpPost("{tipo}/{id:guid}/conciliar")]
    public Task<ConciliacionMercadoPago> Conciliar(string tipo, Guid id, [FromQuery] int pagina = 1, CancellationToken ct = default) =>
        pagos.ConciliarAsync(tipo, id, pagina, ct);
}
