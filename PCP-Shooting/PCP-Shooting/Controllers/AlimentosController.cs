using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController, Route("api/alimentos"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AlimentosController(IAlimentosService alimentos) : ControllerBase
{
    [HttpGet]
    public Task<PaginaConsulta<AlimentoPublico>> Listar([FromQuery] FiltroAlimentos filtro, CancellationToken ct) => alimentos.ListarAsync(filtro, ct);

    [HttpGet("{sku}")]
    public async Task<ActionResult<AlimentoPublico>> Consultar(string sku, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku)) return BadRequest();
        var alimento = await alimentos.ConsultarAsync(sku, ct);
        return alimento is null ? NotFound() : Ok(alimento);
    }
}
