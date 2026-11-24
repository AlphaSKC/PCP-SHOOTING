using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;

namespace PCP_Shooting.Controllers;

[ApiController, AllowAnonymous, Route("api/webhooks/mercadopago")]
public sealed class MercadoPagoWebhookController(IMercadoPagoService mercadoPago) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir(CancellationToken ct)
    {
        var procesado = await mercadoPago.ProcesarNotificacionAsync(
            Request.Query["data.id"].FirstOrDefault(),
            Request.Headers["x-signature"].FirstOrDefault(),
            Request.Headers["x-request-id"].FirstOrDefault(), ct);
        return procesado ? Ok() : Unauthorized();
    }
}
