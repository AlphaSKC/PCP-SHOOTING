using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/paquetes")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaquetesController(IPaquetesService paquetes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<PaquetePublico>>> Listar([FromQuery] FiltroPaquetes filtro, CancellationToken ct) =>
        Ok(await paquetes.ListarPublicosAsync(filtro, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaquetePublico>> Consultar(Guid id, CancellationToken ct)
    {
        var paquete = await paquetes.ConsultarPublicoAsync(id, ct);
        return paquete is null ? NotFound() : Ok(paquete);
    }
}
