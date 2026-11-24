using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCP_Shooting.Domain.Interfaces;
using PCP_Shooting.Domain.Models;

namespace PCP_Shooting.Controllers;

[ApiController]
[Route("api/administracion/paquetes")]
[Authorize(Policy = "OperacionReservas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaquetesAdministracionController(IPaquetesService paquetes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaConsulta<PaqueteGestion>>> Listar([FromQuery] FiltroPaquetesGestion filtro, CancellationToken ct) =>
        Ok(await paquetes.ListarGestionAsync(filtro, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaqueteGestion>> Consultar(Guid id, CancellationToken ct)
    {
        var paquete = await paquetes.ConsultarGestionAsync(id, ct);
        return paquete is null ? NotFound() : Ok(paquete);
    }

    [HttpPost, Authorize(Policy = "Administrador")]
    public async Task<ActionResult<PaqueteGestion>> Crear(GuardarPaquete datos, CancellationToken ct)
    {
        var paquete = await paquetes.GuardarAsync(null, datos, ct);
        return CreatedAtAction(nameof(Consultar), new { id = paquete.Id }, paquete);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "Administrador")]
    public async Task<ActionResult<PaqueteGestion>> Editar(Guid id, GuardarPaquete datos, CancellationToken ct) =>
        Ok(await paquetes.GuardarAsync(id, datos, ct));
}
