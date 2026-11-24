using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize(Policy = "OperacionPedidos")]
public sealed class PagosController(IEscriturasService escrituras) : ControllerBase
{
    [HttpPost("{id:guid}/confirmacion")]
    public async Task<ActionResult<EscrituraResultado>> Confirmar(Guid id, CancellationToken cancellationToken) =>
        Ok(await escrituras.ConfirmarPagoAsync(id, cancellationToken));
}